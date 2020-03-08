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
        public class Tunnel
        {
            private Drone Drone;
            public Vector3D StartingPoint;
            private Vector3D EndPoint;
            private int State;
            private int TunnelIndex;

            public Tunnel(Drone drone, Vector3D startingPoint, Vector3D endPoint)
            {
                this.Drone = drone;
                this.State = 0;
                this.StartingPoint = startingPoint;
                this.EndPoint = endPoint;
                this.TunnelIndex = 1;
            }

            public bool Mine()
            {
                switch (this.State)
                {   
                    //Mining Tunnel
                    case 0:
                        Drone.ManeuverService.AlignBlockTo(EndPoint, Drone.Remote);
                        if (Drone.FlyTo(EndPoint, Drone.Remote, false, 0.5))
                        {
                            State = 1;
                        }
                        else if(Drone.CurrentCargo() > MyFixedPoint.MultiplySafe(Drone.MaxCargo, 0.9f))
                        {
                            Drone.AllStop();
                            State = 2;
                        }
                        break;
                    //Tunnel complete
                    case 1:
                        //TODO: notify foreman that this tunnel was completed
                        State = 3;
                        break;
                    //Unable to continue
                    case 2:
                        //TODO: Keep this tunnel for later
                        State = 3;
                        break;
                    //Backing out
                    case 3:
                        if (Drone.FlyTo(StartingPoint, Drone.DockingConnector, false, 2))
                        {
                            return true;
                        }
                        break;
                }  
                
                return false;
            }
        }
    }
}
