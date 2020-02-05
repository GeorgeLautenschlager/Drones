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

            /* 
            * Drone controllers are more complex. They're more reactive because they have to make decisions based
            * on the needs of their drones.
            */
            public DroneController(Drone drone)
            {
                this.Drone = drone;
                this.Drone.ListenToChannel(DockingRequestChannel);
                this.Drone.NetworkService.RegisterCallback(DockingRequestChannel, "docking_request_pending");
            }

            public override void Perform()
            {
                Drone.Program.Echo("Drone Controller: Perform");

                IMyBroadcastListener listener = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel);

                if (listener.HasPendingMessage)
                {
                    MyIGCMessage message = listener.AcceptMessage();
                    Drone.Program.Echo(message.Data.ToString());

                }
                else
                {
                    Drone.Program.Echo($"No Messages on channel {listener.Tag}");
                }

                //For now Drone controllers just sit and wait for a message from a drone.
            }

            public void ProcessDockingRequest()
            {
                IMyTextPanel callbackLogger = this.Drone.Program.GridTerminalSystem.GetBlockWithName("callback_logger") as IMyTextPanel;

                if (callbackLogger == null)
                    throw new Exception("No screen found");

                callbackLogger.WriteText($"Logging: {DateTime.Now}");

                //TODO: what if there are multiple docking requests?
                MyIGCMessage message = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel).AcceptMessage();

                if (message.Data == null)
                    throw new Exception("No Message");

                callbackLogger.WriteText(message.Data.ToString());

                IMyShipConnector dockingPort = this.Drone.Program.GridTerminalSystem.GetBlockWithName("Docking Port 1") as IMyShipConnector;

                if (dockingPort == null)
                {
                    throw new Exception("Docking Port 1 not found.");
                }
                else
                {
                    List<Vector3D> dockingPath = new List<Vector3D> { (dockingPort.GetPosition() + 10 * dockingPort.WorldMatrix.Forward), dockingPort.GetPosition() };

                    this.Drone.NetworkService.BroadcastMessage(
                        DockingRequestChannel, 
                        $"{dockingPort.WorldMatrix.Backward},{dockingPath[0].ToString()},{dockingPath[1].ToString()}"
                    );
                }
            }

            public override string Name()
            {
                return "DroneController";
            }
        }
    }
}
