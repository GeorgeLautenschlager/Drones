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
            
            private List<IMyShipDrill> Drills = new List<IMyShipDrill>();

            private Vector3D DeparturePoint;
            // TODO: this will need to be a lot more complex, but for now the actual mining will be manual,
            // the drone just needs to fly to the mining site.
            private Vector3D MiningSite;
            private Vector3D TunnelEnd;
            public long ForemanAddress;
            private bool ManualMining;
            private bool ManualSiteApproach;
            private bool ComputeDeparturePoint;
            private Move Move;
            private DockingAttempt DockingAttempt;
            private Tunnel Tunnel;

            public Miner(MyIni config)
            {
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                this.Drone = drone;

                Drone.Grid().GetBlocksOfType(Drills);
                if (Drills == null || Drills.Count == 0)
                    throw new Exception("Miner is missing drills!");

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
                        Drone.DockingConnector.Disconnect();
                        Drone.DockingConnector.Enabled = false;
                        Drone.OpenFuelTanks();

                        InitializeMiningRun();

                        if(ComputeDeparturePoint)
                        {
                            DeparturePoint = Drone.DockingConnector.GetPosition() + 15 * Drone.DockingConnector.WorldMatrix.Backward;
                        }
                        State = 1;
                        break;
                    case 1:
                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { DeparturePoint }), Drone.Remote, false);

                        // Departing
                        if (Move.Perform())
                        {
                            Move = null;
                            State = 2;
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
                            State = 3;
                        }
                        break;
                    case 3:
                        if (ManualMining)
                        {
                            Drone.Program.IGC.SendUnicastMessage(ForemanAddress, "Notifications", "I have arrived at the mining site");
                            Drone.Sleep();
                            return;
                        }
                        else
                        {
                            Drone.Log("Automated");
                            ActivateDrills();
                            if (Tunnel.Mine())
                            {
                                State = 4;
                                DeactivateDrills();
                            }
                        }
                        break;
                    case 4:
                        Drone.Wake();

                        if (DockingAttempt == null)
                            DockingAttempt = new DockingAttempt(Drone, Drone.DockingConnector, ForemanAddress, DockingRequestChannel);

                        if (DockingAttempt.Perform())
                            State = 5;
                        break;
                    case 5:
                        DockingAttempt = null;
                        Drone.LogToLcd("Miner Shutting Down");
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
                        ProcessUnicast();
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

            private void ProcessUnicast()
            {
                MyIGCMessage message = Drone.NetworkService.GetUnicastListener().AcceptMessage();
                if (message.Data == null)
                    Drone.LogToLcd($"\nNo Message");

                // TODO: This could be a switch
                if (message.Tag == DockingRequestChannel && DockingAttempt != null)
                {
                    DockingAttempt.ProcessClearance(message);
                }
                else if (message.Tag == "recall")
                {
                    this.State = 4;
                }
                else
                {
                    Drone.LogToLcd($"{message.Tag} is not a recognized message format.");
                }
            }

            private void InitializeMiningRun()
            {
                MyIni config = Drone.GetConfig();

                string rawValue = config.Get(Name(), "mining_site").ToString();
                if (!Vector3D.TryParse(rawValue, out MiningSite))
                    throw new Exception($"Unable to parse: {rawValue} as mining site");

                rawValue = config.Get(Name(), "tunnel_end").ToString();
                if (!Vector3D.TryParse(rawValue, out TunnelEnd))
                    throw new Exception($"Unable to parse: {rawValue} as tunnel end");

                int index;
                if (!config.Get(Name(), "index").TryGetInt32(out index))
                    throw new Exception("index is missing");

                Tunnel = new Tunnel(Drone, MiningSite, TunnelEnd, index, ForemanAddress);
            }

            public override string Name()
            {
                return "miner";
            }
        }
    }
}
