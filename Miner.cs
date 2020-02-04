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

            private Vector3D[] dockingPath = new Vector3D[2] { new Vector3D(), new Vector3D() };
            private Vector3D dockingConnectorOrientation;

            public Miner(Drone drone)
            {
                this.Drone = drone;
                this.State = 4;
            }

            public override void Perform()
            {
                switch (this.State)
                {
                    case 4:
                        Drone.NetworkService.BroadcastMessage(DockingRequestChannel, "Requesting Docking Clearance");
                        break;
                    case 5:
                        //Waiting for docking clearance from controller
                        break;
                    case 6:
                        //Use docking path from the last state
                        Drone.FlyToCoordinates(dockingPath[0]);
                        break;
                    case 7:
                        //perform docking
                        Drone.Dock(dockingPath[1], ConnectorOrientation);
                        break;
                }
            }

            public void AcceptDockingClearance()
            {
                //TODO: what if there are multiple docking requests?
                MyIGCMessage message = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel).AcceptMessage();

                //TODO: parse out orientation as the third string
                string[] vectorStrings = message.Data.ToString().Split(',');
                Vector3D.TryParse(vectorStrings[0], out dockingPath[0]);
                Vector3D.TryParse(vectorStrings[1], out dockingPath[1]);
                Vector3D.TryParse(vectorStrings[2], out dockingConnectorOrientation);
            }

            public override string Name()
            {
                return "Miner";
            }
        }
    }
}
