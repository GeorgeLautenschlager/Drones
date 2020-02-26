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

            public Tester(MyIni config)
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
                this.State = 0;
            }

            public void ParseConfig(MyIni config)
            {

            }

            public override void Perform()
            {
                switch (this.State)
                {
                    case 0:
                        Drone.Startup();
                        DockingConnector.Disconnect();
                        DockingConnector.Enabled = false;
                        Drone.OpenFuelTanks();

                        MyWaypointInfo waypoint;
                        MyWaypointInfo.TryParse("GPS:Test Marker #1:141170.2:-72210.54:-61138.71:", out waypoint);
                        Target = waypoint.Coords;

                        Drone.Log("Starting Move");

                        this.State = 1;
                        break;
                    case 1:
                        if ((Drone.Remote.CenterOfMass - Target).Length() > 200 || (Drone.FlyTo(Target)))
                            this.State = 2;

                        Drone.Log("Moving");
                        break;
                    case 2:
                        Drone.AllStop();
                        Drone.Sleep();

                        Drone.Log("Shutting down");
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
