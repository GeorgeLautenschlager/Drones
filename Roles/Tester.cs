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
        public class Tester : Role
        {
            /* 
                * This is a test role
            */

            IMyCockpit Cockpit;
            List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
            Vector3D targetPosition;

            public Tester(Drone drone)
            {
                this.Drone = drone;
                this.State = 0;
                Drone.Program.GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Blocks);
                Cockpit = Blocks.First() as IMyCockpit;
                targetPosition = Cockpit.GetPosition() + 100 * Cockpit.WorldMatrix.Forward;
            }

            public override void Perform()
            {
                Drone.Program.Echo("Performing role");
                switch (this.State)
                {
                    case 0:
                        Drone.Program.Echo("Moving");
                        if (this.Drone.ManeuverService.GoToWithThrusters(targetPosition, Cockpit))
                        {
                            this.State = 0;
                            this.Drone.ManeuverService.Reset();
                        }
                        break;
                    case 1:
                        Drone.Program.Echo("Translation Complete");
                        break;

                }
            }

            public override string Name()
            {
                return "Tester";
            }
        }
    }
}
