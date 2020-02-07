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
              * 0:  requesting departure clearance
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

            private Vector3D[] ApproachPath = new Vector3D[2] { new Vector3D(), new Vector3D() };
            private Vector3D dockingConnectorOrientation;
            private IMyShipConnector DockingConnector;

            public Miner(Drone drone)
            {
                this.Drone = drone;
                this.State = 4;
                this.Drone.ListenToChannel(DockingRequestChannel);
                this.Drone.NetworkService.RegisterCallback(DockingRequestChannel, "docking_request_granted");
            }

            public override void Perform()
            {
                Drone.Program.Echo($"Miner: performing role, state: {this.State}");

                switch (this.State)
                {
                    case 4:
                        // Get a path to following as well as the location
                        Drone.Program.Echo("Sending docking request");
                        Drone.NetworkService.BroadcastMessage(DockingRequestChannel, "Requesting Docking Clearance");
                        this.State = 5;
                        break;
                    case 5:
                        //Waiting for docking clearance from controller
                        break;
                    case 6:
                        //Use docking path from the last state
                        //Drone.Program.Echo($"Moving to Approach position {dockingConnectorOrientation}, {ApproachPath[0]}, {ApproachPath[1]} {DateTime.Now.ToString()}");
                        //Drone.ManeuverService.GoToPosition(ApproachPath[0], DockingConnector, false, false);
                        Drone.FlyToCoordinates(ApproachPath[0]);
                        //this.State = 7;
                        break;
                    case 7:
                        //Aligning
                        //Drone.ManeuverService.AlignTo(dockingConnectorOrientation, DockingConnector);
                        //this.State = 8;
                        break;
                    case 8:
                        //Docking
                        //bool contact = Drone.ManeuverService.FlyToPosition(ApproachPath[1], DockingConnector);
                        //if (contact)
                        //{
                        //    DockingConnector.Enabled = true;
                        //    DockingConnector.Connect();
                        //}
                        break;
                }
            }

            public void Dock()
            {
                Drone.ManeuverService.AlignTo(ApproachPath[1], DockingConnector);

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

                // Find connector for docking
                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                Drone.Program.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);

                if (connectors == null || connectors.Count == 0)
                    throw new Exception("No docking connector found!");

                DockingConnector = connectors.First();

                // TODO: it would be nicer to have names rather than magic numbers.
                this.State = 6;
            }

            public override string Name()
            {
                return "Miner";
            }
        }
    }
}
