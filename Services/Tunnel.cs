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
            private int State;

            public Vector3D StartingPoint;
            public Vector3D EndPoint;
            public int TunnelIndex;
            private long ParentAddress;

            public Tunnel(Drone drone, Vector3D startingPoint, Vector3D endPoint, int tunnelIndex, long parentAddress)
            {
                Drone = drone;
                State = 0;
                StartingPoint = startingPoint;
                EndPoint = endPoint;
                TunnelIndex = tunnelIndex;
                ParentAddress = parentAddress;
            }

            public Tunnel(MyIni asteroidCatalogue)
            {
                //go into the asteroid catalogue, look at available deposits and assign StartingPoint, EndPoint and TunnelIndex
                List<string> sections = new List<string>();
                asteroidCatalogue.GetSections(sections);
                IEnumerable<string> deposits = sections.Where(record => record.StartsWith("deposit"));

                //Assume one entry for now
                string rawValue, deposit;
                double depth;
                int index;

                if (deposits != null && deposits.Count() != 0)
                    deposit = deposits.First();
                else
                    throw new Exception("No deposit data found!");

                if (!asteroidCatalogue.Get(deposit, "deposit_depth").TryGetDouble(out depth))
                    throw new Exception("deposit_depth is missing");
                if (!asteroidCatalogue.Get(deposit, "index").TryGetInt32(out index))
                    throw new Exception("index is missing");

                //Convert config data into usable vectors
                Vector3D TopLeftCorner;
                if (asteroidCatalogue.Get(deposits.First(), "top_left_corner").TryGetString(out rawValue))
                    Vector3D.TryParse(rawValue, out TopLeftCorner);
                else
                    throw new Exception("top_left_corner is missing");

                Vector3D TopRightCorner;
                if (asteroidCatalogue.Get(deposits.First(), "top_right_corner").TryGetString(out rawValue))
                    Vector3D.TryParse(rawValue, out TopRightCorner);
                else
                    throw new Exception("top_right_corner is missing");

                Vector3D BottomLeftCorner;
                if (asteroidCatalogue.Get(deposits.First(), "bottom_left_corner").TryGetString(out rawValue))
                    Vector3D.TryParse(rawValue, out BottomLeftCorner);
                else
                    throw new Exception("bottom_left_corner is missing");

                SetPointsFromDeposit(index, TopLeftCorner, TopRightCorner, BottomLeftCorner, depth);
            }

            private void SetPointsFromDeposit(int index, Vector3D TopLeftCorner, Vector3D TopRightCorner, Vector3D BottomLeftCorner, double depth)
            {
                double droneWidth = 3.0;
                Vector3D DepositNormal = (new PlaneD(TopLeftCorner, TopRightCorner, BottomLeftCorner)).Normal;
                Vector3D xAxis, yAxis;
                int rows, row, column, columns;

                xAxis = TopRightCorner - TopLeftCorner;
                yAxis = BottomLeftCorner - TopLeftCorner;
                rows = (int) Math.Ceiling(xAxis.Length() / droneWidth);
                columns = (int) Math.Ceiling(xAxis.Length() / droneWidth);
                row = index % rows;
                column = (int) Math.Floor(index / (double) rows);

                // Starting from the "origin" move along the x axis 3 times the drone width and the y axis 3 times the drone width
                StartingPoint = TopLeftCorner + (row * droneWidth * Vector3D.Normalize(xAxis)) + (column * droneWidth * Vector3D.Normalize(yAxis));
                EndPoint = StartingPoint + depth * Vector3D.Normalize(DepositNormal);
                TunnelIndex = index;
            }

            public bool Mine()
            {
                switch (this.State)
                {   
                    //Mining Tunnel
                    case 0:
                        Drone.ManeuverService.AlignBlockTo(EndPoint, Drone.Remote);
                        if (Drone.FlyTo(EndPoint, Drone.Remote, false, 0.3))
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
                        Drone.Program.IGC.SendUnicastMessage(ParentAddress, "tunnel_complete", TunnelIndex);
                        State = 3;
                        break;
                    //Unable to continue
                    case 2:
                        State = 3;
                        break;
                    //Backing out
                    case 3:
                        //Calculate backout point
                        Vector3D BackOutPoint = (EndPoint - StartingPoint) * 2;

                        if (Drone.FlyTo(StartingPoint, Drone.DockingConnector, false, 1))
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
