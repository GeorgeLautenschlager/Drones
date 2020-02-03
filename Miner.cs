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
                        Behaviour.RequestDockingClearance(Drone);
                        break;
                    case 5:
                        //Waiting for docking clearance from controller
                        break;
                    case 6:
                        //Use docking path from the last state
                        Behaviour.FlyToCoordinates(DockingPath);
                        break;
                    case 7:
                        //perform docking
                        Behaviour.Dock(ConnectorPosition, ConnectorOrientation);
                        break;
                }
            }

            public override string Name()
            {
                return "Miner";
            }
        }
    }
}
