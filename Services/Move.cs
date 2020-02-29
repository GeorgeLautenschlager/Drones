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
            private bool Aligned;

            // 0:initial, 1: aligning, 2: Moving, 3: Both
            int State;

            public Move(Drone drone, Queue<Vector3D> waypoints, IMyTerminalBlock referenceBlock, bool align)
            {
                this.Drone = drone;
                this.ReferenceBlock = referenceBlock;
                this.Aligned = align;
                this.Waypoints = waypoints;
                this.State = 0;
                this.Align = true;
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
                        if (Drone.ManeuverService.AlignBlockTo(CurrentWaypoint, ReferenceBlock))
                            this.State = 2;
                        break;

                    case 2:
                        if (Drone.FlyTo(CurrentWaypoint, ReferenceBlock))
                        {
                            this.State = 3;
                        }

                        break;

                    case 3:
                        Drone.AllStop();
                        this.State = 0;

                        break;

                }

                //switch (this.State)
                //{
                //    case 0:
                //        Drone.AllStop();
                //        this.State = 1;
                //        break;

                //    case 1:
                //        //set new waypoint and make sure the drone has stopped
                //        if (Waypoints.Count == 0)
                //            return true;
                        
                //        CurrentWaypoint = Waypoints.Dequeue();
                //        Aligned = false;
                //        this.State = 2;
                //        break;

                //    case 2:
                //        //translate and optionally align
                //        if (AlignmentMode == "Simultaneous")
                //        {
                //            Drone.ManeuverService.AlignBlockTo(CurrentWaypoint, ReferenceBlock);
                //        }
                //        else if(AlignmentMode == "First")
                //        {
                //            Drone.Log($"Aligned: {Aligned}");
                //            if (!Aligned && Drone.ManeuverService.AlignBlockTo(CurrentWaypoint, ReferenceBlock))
                //            {
                //                Aligned = true;
                //                Drone.AllStop();
                //            }
                //            else if(!Aligned)
                //            {
                //                Drone.Log("Aligning");
                //                break;
                //            }
                //        }

                //        if (Drone.FlyTo(CurrentWaypoint, ReferenceBlock))
                //        {
                //            Drone.AllStop();
                //            if (Waypoints.Count > 0)
                //            {
                //                this.State = 1;
                //            }
                //            else
                //            {
                //                this.State = 3;
                //            }
                //        }
                //        else
                //        {
                //            Drone.Log($"Flying to waypoint: {CurrentWaypoint.ToString()}");
                //        }

                //        break;
                    
                //    case 3:
                //        Drone.AllStop();
                //        return true;
                //}

                return false;
            }

            //public void Transition()
            //{
            //    if (Waypoints.Count == 0)
            //    {
            //        this.State = 3;
            //        return;
            //    }


            //    if (AlignmentMode == "Between Waypoints" && this.State == 0 || this.State == 2)
            //    {

            //        this.State = 1;
            //    }
            //    else if (this.State == 1 || this.State == 0)
            //    {
            //        if (Waypoints.Count > 0)
            //        {
            //            this.State = 2;
            //        }
            //        else
            //        {
            //            //The Queue is empty, move to final state
            //            this.State = 3;
            //        }
            //    }

            //}
        }
    }
}
