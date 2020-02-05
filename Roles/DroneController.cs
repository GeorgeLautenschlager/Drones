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
            }

            public override void Perform()
            {
                //For now Drone controllers just sit and wait for a message from a drone.
            }

            public void ProcessDockingRequest()
            {
                //TODO: what if there are multiple docking requests?
                MyIGCMessage message = this.Drone.NetworkService.GetBroadcastListenerForChannel(DockingRequestChannel).AcceptMessage();

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
