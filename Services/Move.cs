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
        public class Move
        {
            private Drone Drone;
            private Vector3D CurrentWaypoint;
            private Queue<Vector3D> Waypoints;
            private IMyTerminalBlock ReferenceBlock;
            private bool Align;
            private double SpeedLimit;

            // 0:initial, 1: aligning, 2: Moving, 3: Both
            int State;

            public Move(Drone drone, Queue<Vector3D> waypoints, IMyTerminalBlock referenceBlock, bool align, double speedLimit = -1)
            {
                Drone = drone;
                ReferenceBlock = referenceBlock;
                Waypoints = waypoints;
                State = 0;
                Align = align;
                SpeedLimit = speedLimit;
            }

            public bool Perform()
            {
                switch(this.State)
                {
                    case 0:
                        //Next Waypoint
                        if (Waypoints.Count == 0)
                            return true;

                        CurrentWaypoint = Waypoints.Dequeue();
                        if (Align)
                        {
                            this.State = 1;
                        }
                        else
                        {
                            this.State = 2;
                        }

                        break;

                    case 1:
                        Drone.Log("Rotating");
                        if (Drone.ManeuverService.AlignBlockTo(CurrentWaypoint, ReferenceBlock))
                            this.State = 2;
                        break;

                    case 2:
                        Drone.Log("Translating");
                        if (Drone.FlyTo(CurrentWaypoint, ReferenceBlock, Align, SpeedLimit))
                        {
                            this.State = 3;
                        }

                        break;

                    case 3:
                        Drone.AllStop();
                        this.State = 0;

                        break;

                }



                return false;
            }

            
        }
    }
}
