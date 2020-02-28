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
            private string AlignmentMode;

            // 0:initial, 1: aligning, 2: Moving, 3: Both
            int State;

            public Move(Drone drone, Queue<Vector3D> waypoints, string alignmentMode, IMyTerminalBlock referenceBlock)
            {
                this.Drone = drone;
                this.ReferenceBlock = referenceBlock;
                this.AlignmentMode = alignmentMode;
                this.Waypoints = waypoints;
                this.State = 0;
            }

            public bool Perform()
            {
                switch (this.State)
                {
                    case 0:
                        Transition();
                        break;

                    case 1:
                        if (Drone.ManeuverService.AlignBlockTo(CurrentWaypoint, ReferenceBlock))
                            Transition();

                        break;
                    case 2:
                        if (Drone.FlyTo(CurrentWaypoint, ReferenceBlock))
                            Transition();

                        break;
                    case 3:
                        Drone.AllStop();
                        return true;
                }

                return false;
            }

            public void Transition()
            {
                if (Waypoints.Count == 0)
                {
                    this.State = 3;
                    return;
                }


                if (AlignmentMode == "Between Waypoints" && this.State == 0 || this.State == 2)
                {
                    this.State = 1;
                }
                else if (this.State == 1 || this.State == 0)
                {
                    if (Waypoints.Count > 0)
                    {
                        this.CurrentWaypoint = Waypoints.Dequeue();
                        this.State = 2;
                    }
                    else
                    {
                        //The Queue is empty, move to final state
                        this.State = 3;
                    }
                }

            }
        }
    }
}
