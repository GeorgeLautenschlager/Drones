using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Miner : Role
        {
            /* 
            * Miners are simple automatons and follow this state progression:
            * 0:  departing


            * 1:  departing
            * 2:  flying to mining site
            * 3:  mining
            * 4:  requesting docking clearance
            * 5:  waiting for docking clearance
            * 6:  returning to drone controller
            * 7:  aligning to docking port
            * 8:  Docking
            * 9:  Shutting down
            */
            
            private IMyShipConnector DockingConnector;
            private List<IMyShipDrill> Drills = new List<IMyShipDrill>();

            private Vector3D DeparturePoint;
            // TODO: this will need to be a lot more complex, but for now the actual mining will be manual,
            // the drone just needs to fly to the mining site.
            private Vector3D MiningSite;
            private Vector3D TunnelEnd;
            private Vector3D[] ApproachPath = new Vector3D[2] { new Vector3D(), new Vector3D() };
            private Vector3D dockingConnectorOrientation;
            public long ForemanAddress;
            private bool DockingClearanceReceived = false;
            private bool ManualMining;
            private bool ManualSiteApproach;
            private bool ComputeDeparturePoint;
            private DockingAttempt Docking;
            private Move Move;

            public Miner(MyIni config)
            {
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                this.Drone = drone;

                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                //TODO: make sure you don't grab a connector on the other grid
                Drone.Program.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);
                if (connectors == null || connectors.Count == 0)
                    throw new Exception("No docking connector found!");

                Drone.Grid().GetBlocksOfType(Drills);
                if (Drills == null || Drills.Count == 0)
                    throw new Exception("Miner is missing drills!");

                DockingConnector = connectors.First(); 
                Drone.ListenToChannel(DockingRequestChannel);
                Drone.NetworkService.RegisterBroadcastCallback(DockingRequestChannel, "docking_request_granted");
                Drone.Program.IGC.UnicastListener.SetMessageCallback("unicast");
            }

            public override void Perform()
            {
                Drone.Program.Echo($"Miner: {this.State}");

                switch (this.State)
                {
                    case 0:
                        // Startup and Depart
                        Drone.Wake();
                        Drone.Startup();
                        DockingConnector.Disconnect();
                        DockingConnector.Enabled = false;
                        Drone.OpenFuelTanks();

                        if(ComputeDeparturePoint)
                        {
                            DeparturePoint = DockingConnector.GetPosition() + 15 * DockingConnector.WorldMatrix.Backward;
                        }
                        this.State = 1;
                        break;
                    case 1:
                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { DeparturePoint }), Drone.Remote, false);

                        // Departing
                        if (Move.Perform())
                        {
                            Move = null;
                            this.State = 2;
                        }
                        break;
                    case 2:
                        if (ManualSiteApproach)
                        {
                            Drone.Sleep();
                            return;
                        }

                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { MiningSite }), Drone.Remote, true);

                        if (Move.Perform())
                        {
                            Move = null;
                            this.State = 3;
                        }
                        break;
                    case 3:
                        Drone.Log("Mining");
                        if (ManualMining)
                        {
                            Drone.LogToLcd("Sleeping");
                            Drone.Program.IGC.SendUnicastMessage(ForemanAddress, "Notifications", "I have arrived at the mining site");
                            Drone.Sleep();
                            return;
                        }
                        else
                        {
                            //TODO: introduce Tunnel, CargoService and speed limit in Move
                            Drone.Log("Automated");
                            if (Move == null)
                            {
                                Drone.Log("Beginning Excavation");
                                ActivateDrills();
                                Move = new Move(Drone, new Queue<Vector3D>(new[] { TunnelEnd }), Drone.Remote, true);
                            }

                            if (Move.Perform())
                            {
                                Drone.Log("Excavating tunnel");
                                Move = null;
                                DeactivateDrills();
                                this.State = 4;
                            }
                        }
                        break;
                    case 4:
                        Drone.Wake();
                        Drone.Program.IGC.SendUnicastMessage(ForemanAddress, DockingRequestChannel, "Requesting Docking Clearance");
                        this.State = 5;
                        break;
                    case 5:
                        // Waiting for docking clearance from controller
                        Drone.Log("Waiting for docking clearance.");
                        if (DockingClearanceReceived)
                        {
                            this.State = 6;
                            Drone.LogToLcd("Clearance Received");
                            Drone.LogToLcd($"Approach: {ApproachPath[0].ToString()}");
                            Drone.LogToLcd($"Docking Port: {ApproachPath[1].ToString()}\n");
                        }
                        break;
                    case 6:
                        Drone.Log($"\nProceeding to docking approach");
                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { ApproachPath[0] }), Drone.Remote, true);

                        if (Move.Perform())
                        {
                            Move = null;
                            this.State = 7;
                        }
                        break;
                    case 7:
                        DockingConnector.Enabled = true;
                        Drone.Log($"\nOn Final Approach");
                        //=================================
                        //TODO:  use the connectors bounding box to compute an offset?
                        //=================================

                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { ApproachPath[1] }), DockingConnector, true);

                        if (Move.Perform() || DockingConnector.Status == MyShipConnectorStatus.Connected)
                        {
                            Move = null;
                            this.State = 8;
                        }
                        DockingConnector.Connect();
                        break;
                    case 8:
                        if (DockingConnector.Status == MyShipConnectorStatus.Connected)
                        {
                            this.State = 9;
                        }

                        DockingConnector.Connect();
                        break;
                    case 9:
                        Drone.Shutdown();
                        Drone.Sleep();
                        break;
                }
            }

            public void ActivateDrills()
            {
                foreach (IMyShipDrill drill in Drills)
                {
                    drill.Enabled = true;
                }
            }

            public void DeactivateDrills()
            {
                foreach (IMyShipDrill drill in Drills)
                {
                    drill.Enabled = false;
                }
            }

            public void ParseConfig(MyIni config)
            {

                string rawValue = config.Get(Name(), "departure_point").ToString();
                if (!Vector3D.TryParse(rawValue, out DeparturePoint))
                    throw new Exception($"Unable to parse: {rawValue} as departure point");

                rawValue = config.Get(Name(), "mining_site").ToString();
                if (!Vector3D.TryParse(rawValue, out MiningSite))
                    throw new Exception($"Unable to parse: {rawValue} as mining site");

                rawValue = config.Get(Name(), "tunnel_end").ToString();
                if (!Vector3D.TryParse(rawValue, out TunnelEnd))
                    throw new Exception($"Unable to parse: {rawValue} as tunnel end");

                rawValue = config.Get(Name(), "foreman_address").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Int64.TryParse(rawValue, out ForemanAddress);
                }
                else
                {
                    throw new Exception("Drone has no address for its foreman!");
                }

                rawValue = config.Get(Name(), "initial_state").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Int32.TryParse(rawValue, out this.State);
                }
                else
                {
                    throw new Exception("initial_state is missing");
                }

                ManualMining = config.Get(Name(), "manual_mining").ToBoolean();
                ManualSiteApproach = config.Get(Name(), "manual_site_approach").ToBoolean();
                ComputeDeparturePoint = config.Get(Name(), "compute_departure_point").ToBoolean();
            }

            public override void HandleCallback(string callback)
            {
                switch (callback)
                {
                    case "unicast":
                        MyIGCMessage message = Drone.NetworkService.GetUnicastListener().AcceptMessage();
                        if (message.Data == null)
                            Drone.LogToLcd($"\nNo Message");

                        if (message.Tag == DockingRequestChannel)
                        {
                            MyTuple<Vector3D, Vector3D, Vector3D> messageTuple = (MyTuple<Vector3D, Vector3D, Vector3D>)message.Data;
                            ApproachPath[0] = messageTuple.Item1;
                            ApproachPath[1] = messageTuple.Item2;
                            DockingClearanceReceived = true;
                        }
                        else if (message.Tag == "recall")
                        {
                            this.State = 4;
                        }
                        else
                        {
                            Drone.LogToLcd($"{message.Tag} is not a recognized message format.");
                        }

                        
                        break;
                    case "docking_request_granted":
                        this.Docking.ProcessClearance();
                        break;
                    case "launch":
                        this.State = 0;
                        Drone.Wake();
                        break;
                    case "activate":
                        // TODO: remove this diagnostic code
                        ActivateDrills();
                        break;
                    case "":
                        // Just Ignore empty arguments
                        break;
                    default:
                        Drone.LogToLcd($"\nDrone received unrecognized callback: {callback}");
                        break;
                }
            }

            //TODO: this is too general to live in the Miner role. It should be moved to Drone, or maybe Role
            public void AcceptDockingClearance()
            {
                IMyBroadcastListener listener = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel);

                if (listener == null)
                    Drone.LogToLcd("\nNo listener found");

                MyIGCMessage message = listener.AcceptMessage();

                if (message.Data == null)
                    Drone.LogToLcd("No message");

                Drone.LogToLcd($"\nDocking Clearance: {message.Data.ToString()}");

                string[] vectorStrings = message.Data.ToString().Split(',');
                Vector3D.TryParse(vectorStrings[0], out dockingConnectorOrientation);
                Vector3D.TryParse(vectorStrings[1], out ApproachPath[0]);
                Vector3D.TryParse(vectorStrings[2], out ApproachPath[1]);

                DockingClearanceReceived = true;
            }

            public override string Name()
            {
                return "miner";
            }
        }
    }
}
