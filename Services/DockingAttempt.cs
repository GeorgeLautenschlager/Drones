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
        public class DockingAttempt
        {
            private Drone Drone;
            private string State = "Initial";
            private Queue<Vector3D> DockingPath;
            private IMyShipConnector DockingPort;
            private bool RequestSent = false;
            private bool ClearanceGranted = false;
            private long DockWithGrid;
            private Move Move;
            private string DockingRequestChannel;
            private Vector3D[] ApproachPath = new Vector3D[2] { new Vector3D(), new Vector3D() };

            public DockingAttempt(Drone drone, IMyShipConnector dockingPort, long dockWithGrid, string dockingRequestChannel)
            {
                Drone = drone;
                DockingPort = dockingPort;
                DockWithGrid = dockWithGrid;
                DockingRequestChannel = dockingRequestChannel;
            }

            public bool Perform()
            {
                switch (State)
                {
                    case "Initial":
                        State = "Requesting Clearance";
                        break;
                    case "Requesting Clearance":
                        if (ClearanceGranted)
                            State = "Processing Clearance";

                        if (!RequestSent)
                        {
                            SendRequest();
                            RequestSent = true;
                        }

                        break;
                    case "Waiting for Clearance":
                        break;

                    case "Approaching Dock":
                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { ApproachPath[0] }), Drone.Remote, true);

                        if (Move.Perform())
                        {
                            Move = null;
                            State = "Docking";
                        }

                        break;

                    case "Docking":
                        DockingPort.Enabled = true;

                        Vector3D nearDock = ApproachPath[1] + Vector3D.Normalize(ApproachPath[0] - ApproachPath[1]) * 5;
                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { nearDock }), DockingPort, true);

                        if (Move.Perform())
                        {
                            Move = null;
                            Drone.AllStop();
                            State = "Final Approach";
                        }
                        break;

                    case "Final Approach":
                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { ApproachPath[1] }), DockingPort, true, 0.25);

                        if (Move.Perform() || DockingPort.Status == MyShipConnectorStatus.Connected)
                        {
                            Move = null;
                            Drone.AllStop();
                            State = "Final Approach";
                        }
                        DockingPort.Connect();
                        break;
                    case "Connecting":
                        if ( DockingPort.Status == MyShipConnectorStatus.Connected)
                        {
                            Drone.AllStop();
                            State = "Final";
                        }
                        DockingPort.Connect();
                        break;
                    case "Final":
                        return true;
                        break;
                        
                }

                return false;
            }
            
            private void SendRequest()
            {
                Drone.Program.IGC.SendUnicastMessage(DockWithGrid, DockingRequestChannel, "Requesting Docking Clearance");
            }

            public void ProcessClearance(MyIGCMessage clearance)
            {
                //=================================
                //TODO:  use the connectors bounding box to compute an offset?
                //=================================

                MyTuple<Vector3D, Vector3D, Vector3D> messageTuple = (MyTuple<Vector3D, Vector3D, Vector3D>)clearance.Data;
                ApproachPath[0] = messageTuple.Item1;
                ApproachPath[1] = messageTuple.Item2;
                State = "Approaching Dock";
            }

            private void Connect()
            {

            }
        }
    }
}
