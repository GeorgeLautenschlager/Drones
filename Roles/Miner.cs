﻿using Sandbox.Game.EntityComponents;
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
            public long DroneControllerEntityId;
            private bool DockingClearanceReceived = false;
            private bool ManualMining;
            private bool ManualSiteApproach;
            private bool ComputeDeparturePoint;

            public Miner(MyIni config)
            {
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                this.Drone = drone;

                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                Drone.Program.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);
                if (connectors == null || connectors.Count == 0)
                    throw new Exception("No docking connector found!");

                this.DockingConnector = connectors.First();
                this.Drone.ListenToChannel(DockingRequestChannel);
                this.Drone.NetworkService.RegisterBroadcastCallback(DockingRequestChannel, "docking_request_granted");
            }

            public override void Perform()
            {
                //Drone.Program.Echo($"Miner: {this.State}");

                switch (this.State)
                {
                    case 0:
                        // Startup and Depart
                        Drone.Startup();
                        DockingConnector.Disconnect();
                        DockingConnector.Enabled = false;
                        Drone.OpenFuelTanks();

                        if(ComputeDeparturePoint)
                        {
                            DeparturePoint = DockingConnector.GetPosition() + 10 * DockingConnector.WorldMatrix.Backward;
                        }

                        Remote().SetCollisionAvoidance(true);
                        Drone.Move(DeparturePoint, "Departure Point", dockingMode: true);
                        this.State = 1;
                        break;
                    case 1:
                        // Departing
                        if (Drone.Moving(DeparturePoint, docking: false))
                        {   
                            // Departure complete, begin moving to the mining site
                            if(ManualSiteApproach)
                            {
                                Drone.Sleep();
                                return;
                            }

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
                        if (ManualMining)
                        {
                            Drone.Sleep();
                            return;
                        }
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
                        if (DockingClearanceReceived)
                        {
                            Drone.LogToLcd($"\nSetting docking approach: {ApproachPath[0]}");
                            Drone.Log($"Setting docking approach: {ApproachPath[0]}");
                            Drone.Move(ApproachPath[0], "Docking Approach", dockingMode: true);
                            this.State = 6;
                        }
                        else
                        {
                            Drone.Log("Waiting for docking clearance.");
                        }
                        break;
                    case 6:
                        // Moving to Docking Approach point
                        if (Drone.Moving(ApproachPath[0], docking: false))
                        {
                            this.State = 7;
                            Remote().SetCollisionAvoidance(false);
                            Drone.Move(ApproachPath[1], "Docking Port", dockingMode: true, direction: Base6Directions.Direction.Backward);
                        }
                        break;
                    case 7:
                        // Final Approach
                        DockingConnector.Enabled = true;

                        if (Drone.Moving(ApproachPath[1], docking: true))
                        {
                            // activate connector and lock
                            DockingConnector.Connect();

                            if (DockingConnector.Status == MyShipConnectorStatus.Connected)
                            {
                                // Force deactivation of autopilot. We've made contact, our position doesn't matter anymore
                                Remote().SetAutoPilotEnabled(false);
                                this.State = 8;
                            }
                        }
                        break;
                    case 8:
                        Drone.Shutdown();
                        Drone.Sleep();
                        break;
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

                // TODO: switch to Unicasts
                //rawValue = config.Get(Name(), "home_address").ToString();
                //if (rawValue != null && rawValue != "")
                //{
                //    Int64.TryParse(rawValue, out DroneControllerEntityId);
                //}
                //else
                //{
                //    throw new Exception("Drone has no home address!");
                //}

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
                    case "docking_request_granted":
                        AcceptDockingClearance();
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

                DockingClearanceReceived = true;
            }

            public override string Name()
            {
                return "miner";
            }
        }
    }
}
