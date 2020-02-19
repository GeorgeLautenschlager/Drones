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

            private Vector3D DeparturePoint;
            // TODO: this will need to be a lot more complex, but for now the actual mining will be manual,
            // the drone just needs to fly to the mining site.
            private Vector3D MiningSite;
            private Vector3D[] ApproachPath = new Vector3D[2] { new Vector3D(), new Vector3D() };
            private Vector3D dockingConnectorOrientation;

            public Miner(MyIniValue roleConfig)
            {
                ParseConfig(roleConfig);

                // Find connector for docking
                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                Drone.Program.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);
                if (connectors == null || connectors.Count == 0)
                    throw new Exception("No docking connector found!");

                this.DockingConnector = connectors.First();

                this.Drone.ListenToChannel(DockingRequestChannel);
                this.Drone.NetworkService.RegisterBroadcastCallback(DockingRequestChannel, "callback_docking_request_granted");

                this.State = 0;
            }

            public override void Perform()
            {
                Drone.Program.Echo($"Miner: {this.State}");

                switch (this.State)
                {
                    case 0:
                        // Startup and Depart
                        Drone.Startup();
                        DockingConnector.Disconnect();
                        DockingConnector.Enabled = false;
                        Drone.OpenFuelTanks();

                        Drone.Move(DeparturePoint, "Departure Point", dockingMode: true);
                        break;
                    case 1:
                        // Departing
                        if (Drone.Moving(DeparturePoint, docking: false))
                        {   
                            // Departure complete, begin moving to the mining site
                            this.State = 2;
                            Drone.Move(MiningSite, "Mining Site", dockingMode: false);
                        }
                        break;
                    case 2:
                        // Flying to Mining Site
                        if (Drone.Moving(MiningSite, docking: false))
                        {   
                            // Arrived at mining site. Go Idle and wait for manual mining via remoote control
                            this.State = 3;
                        }
                        break;
                    case 3:
                        // Manual mining for now
                        Drone.Sleep();
                        // When manual mining is complete, call this PB remotely with the argument "4" to override state and send it home.
                        break;
                    case 4:
                        // Since Mining is a manual process ATM, the drone is asleep in the previous state.
                        Drone.Wake();
                        // Request Docking Clearance and wait
                        Drone.NetworkService.BroadcastMessage(DockingRequestChannel, "Requesting Docking Clearance");
                        this.State = 5;
                        break;
                    case 5:
                        // Waiting for docking clearance from controller
                        if (dockingConnectorOrientation != null)
                        {
                            Drone.Move(ApproachPath[0], "Docking Approach", dockingMode: true);
                            this.State = 6;
                        }
                        break;
                    case 6:
                        // Moving to Docking Approach point
                        if (Drone.Moving(ApproachPath[0], docking: false))
                        {
                            this.State = 7;
                            Drone.Move(ApproachPath[1], "Docking Port", dockingMode: true, direction: Base6Directions.Direction.Backward);
                        }
                        break;
                    case 7:
                        // Final Approach
                        if (Drone.Moving(ApproachPath[1], docking: true))
                        {
                            // activate connector and lock
                            DockingConnector.Enabled = true;
                            DockingConnector.Connect();

                            if (DockingConnector.Status == MyShipConnectorStatus.Connected)
                            {
                                // Force deactivation of autopilot. We've made contact, our position doesn't matter anymore
                                Remote().SetAutoPilotEnabled(false);
                            }

                            this.State = 8;
                        }
                        break;
                    case 8:
                        Drone.Shutdown();
                        Drone.Sleep();
                        break;
                }
            }

            public void ParseConfig(MyIniValue roleConfig)
            {
                String [] rawStrings = roleConfig.ToString().Split(',');

                if (!Vector3D.TryParse(rawStrings[0], out DeparturePoint))
                    throw new Exception($"Unable to parse: {rawStrings[0]} as departure point");

                if (!Vector3D.TryParse(rawStrings[1], out MiningSite))
                    throw new Exception($"Unable to parse: {rawStrings[1]} as mining site");

                if (rawStrings[2] != null && rawStrings[2] != "")
                    this.State = Int32.Parse(rawStrings[2]);
            }

            //TODO: this is too general to live in the Miner role. It should be moved to Drone, or maybe Role
            public void AcceptDockingClearance()
            {
                Drone.Program.Echo("Accepting Docking Request");
                IMyBroadcastListener listener = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel);

                if (listener == null)
                    throw new Exception("No listener found");

                MyIGCMessage message = listener.AcceptMessage();
                Drone.Program.Echo("Preparing Docking coordinates");

                if (message.Data == null)
                    throw new Exception("No message");

                Drone.Program.Echo($"{message.Data.ToString()}");

                string[] vectorStrings = message.Data.ToString().Split(',');
                Drone.Program.Echo("Vectors Parsed");
                Vector3D.TryParse(vectorStrings[0], out dockingConnectorOrientation);
                Vector3D.TryParse(vectorStrings[1], out ApproachPath[0]);
                Vector3D.TryParse(vectorStrings[2], out ApproachPath[1]);

                // Centre of Mass Offset math
                double offsetLength = (Remote().GetPosition() - Remote().CenterOfMass).Length();
                Vector3D offsetDirection = Vector3D.Normalize(Remote().GetPosition() - ApproachPath[0]);
                Vector3D connectorOffset = offsetLength * offsetDirection;
                ApproachPath[0] = ApproachPath[0] + connectorOffset;

                // Connector Offset math
                offsetLength = (Remote().GetPosition() - DockingConnector.GetPosition()).Length();
                offsetDirection = Vector3D.Normalize(dockingConnectorOrientation);
                connectorOffset = offsetLength * offsetDirection;
                ApproachPath[1] = ApproachPath[1] + connectorOffset;

                this.State = 6;
            }

            public override string Name()
            {
                return "Miner";
            }
        }
    }
}
