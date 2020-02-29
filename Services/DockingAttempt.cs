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
            private Move move;
            private string DockingRequestChannel;
            // TODO: when using Unicasts, tell the DockingAttempt, whcih grid to dock with
            // private long DockWithGrid;

            public DockingAttempt(Drone drone, IMyShipConnector dockingPort, string dockingRequestChannel)
            {
                this.Drone = drone;
                this.DockingPort = dockingPort;
                this.DockingRequestChannel = dockingRequestChannel;
            }

            public bool Perform()
            {
                switch (this.State)
                {
                    case "Initial":
                        this.State = "Requesting Clearance";
                        break;
                    case "Requesting Clearance":
                        if (ClearanceGranted)
                            this.State = "Processing Clearance";

                        if (!RequestSent)
                        {
                            SendRequest();
                            RequestSent = true;
                        }

                        break;
                    case "Processing Clearance":
                        ProcessClearance();
                        this.State = "Approaching Dock";
                        break;

                    case "Approaching Dock":
                        if (move == null)
                        {
                            move = new Move(Drone, DockingPath, Drone.Remote, true);
                            move.Perform();
                        }
                        else if (move.Perform())
                        {
                            move = null;
                            this.State = "Final Approach";
                        }

                        break;

                    case "Final Approach":
                        if (move == null)
                        {
                            move = new Move(Drone, DockingPath, DockingPort, true);
                            move.Perform();
                        }
                        else if (move.Perform())
                        {
                            move = null;
                            this.State = "Connecting";
                        }

                        break;

                    case "Connecting":
                        Connect();
                        return true;
                }

                return false;
            }
            
            private void SendRequest()
            {
                Drone.NetworkService.BroadcastMessage(DockingRequestChannel, "Requesting Docking Clearance");
            }

            public void ProcessClearance()
            {
                IMyBroadcastListener listener = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel);

                if (listener == null)
                    Drone.LogToLcd("\nNo listener found");

                MyIGCMessage message = listener.AcceptMessage();

                if (message.Data == null)
                    Drone.LogToLcd("No message");

                Drone.LogToLcd($"\nDocking Clearance: {message.Data.ToString()}");

                DockingPath = new Queue<Vector3D>();

                string[] vectorStrings = message.Data.ToString().Split(',');
                Vector3D outVector;
                Vector3D.TryParse(vectorStrings[0], out outVector);
                DockingPath.Enqueue(outVector);
                Vector3D.TryParse(vectorStrings[1], out outVector);
                DockingPath.Enqueue(outVector);
            }

            private void Connect()
            {

            }
        }
    }
}
