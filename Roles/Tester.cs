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
using System.Diagnostics;

namespace IngameScript
{
    partial class Program
    {
        public class Tester : Role
        {
            /* 
                * This is a test role
            */
            private IMyShipConnector DockingConnector;
            private Vector3D Target;
            private Move Move;
            private IMyCockpit Cockpit;

            public Tester(MyIni config)
            {
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                Drone = drone;

                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                Drone.Program.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);
                if (connectors == null || connectors.Count == 0)
                    throw new Exception("No docking connector found!");

                DockingConnector = connectors.First();

                State = 0;
            }

            public void ParseConfig(MyIni config)
            {
                
            }

            public override void Perform()
            {
                Drone.LogToLcd("Deposit Centre: " + Drone.Remote.GetPosition());
                Drone.LogToLcd("Deposit Normal: " + Drone.Remote.WorldMatrix.Backward);
            }

            public override string Name()
            {
                return "tester";
            }
        }
    }
}
