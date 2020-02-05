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
             * 0: requesting departure clearance
             * 1: departing
             * 2: flying to mining site
             * 3: mining
             * 4: requesting docking clearance
             * 5: waiting for docking clearance
             * 6: returning to drone controller
             * 7: docking
             * 8: shutting down
             */

            private Vector3D[] approachPath = new Vector3D[2] { new Vector3D(), new Vector3D() };
            private Vector3D dockingConnectorOrientation;

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
                        //this.State = 5;
                        break;
                    case 5:
                        //Waiting for docking clearance from controller
                        break;
                    case 6:
                        //Use docking path from the last state
                        Drone.FlyToCoordinates(approachPath[0]);
                        break;
                    case 7:
                        //perform docking
                        Drone.Program.Echo("Ready to dock!");
                        //Drone.Dock(dockingPath[1], ConnectorOrientation);
                        break;
                }
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
                Vector3D.TryParse(vectorStrings[1], out approachPath[0]);
                Vector3D.TryParse(vectorStrings[2], out approachPath[1]);

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
