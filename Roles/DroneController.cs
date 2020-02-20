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
        public class DroneController : Role
        {
            public string Channel { get; private set; }

            public DroneController(MyIni config)
            {
                
            }
            public override void InitWithDrone(Drone drone)
            {
                this.Drone = drone;
                this.Drone.ListenToChannel(DockingRequestChannel);
                this.Drone.NetworkService.RegisterBroadcastCallback(DockingRequestChannel, "docking_request_pending");
            }


            public override void Perform()
            {
                //For now Drone controllers just sit and wait for a message from a drone.
                Drone.Wake();
            }

            public void ProcessDockingRequest()
            {
                Drone.LogToLcd($"\nLogging: {DateTime.Now}");

                MyIGCMessage message = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel).AcceptMessage();

                if (message.Data == null)
                    Drone.LogToLcd($"\nNo Message");

                IMyShipConnector dockingPort = Drone.Grid().GetBlockWithName("Docking Port 1") as IMyShipConnector;

                if (dockingPort == null)
                {
                    Drone.LogToLcd("\nDocking Port 1 not found.");
                }
                else
                {
                    Vector3D approachPoint = Vector3D.Add(dockingPort.GetPosition(), Vector3D.Multiply(dockingPort.WorldMatrix.Forward, 50));
                    List<Vector3D> dockingPath = new List<Vector3D> { approachPoint, dockingPort.GetPosition() + 0.875 * dockingPort.WorldMatrix.Forward};
                    Drone.LogToLcd($"Sending message: {dockingPort.WorldMatrix.Forward},{dockingPath[0].ToString()},{dockingPath[1].ToString()}");
                    this.Drone.NetworkService.BroadcastMessage(
                        DockingRequestChannel, 
                        $"{dockingPort.WorldMatrix.Forward},{dockingPath[0].ToString()},{dockingPath[1].ToString()}"
                    );
                }
            }

            public override void HandleCallback(string callback)
            {
                switch (callback)
                {
                    case "docking_request_pending":
                        ProcessDockingRequest();
                        break;
                    case "":
                        // Just Ignore empty arguments
                        break;
                    default:
                        Drone.LogToLcd($"\nDrone received unrecognized callback: {callback}");
                        break;
                }
            }

            public override string Name()
            {
                return "drone_controller";
            }
        }
    }
}
