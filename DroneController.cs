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
            public DroneController(Drone drone, string channel)
            {
                this.Drone = drone;
                this.Channel = channel;
                this.Drone.ListenToChannel(channel);
            }

            public override void Perform()
            {
                // Check if your drones need anything
                BroadcastMessage message = this.Drone.NetworkService.GetNextBroadcaseMesage(this.Channel);

                switch (message.Action)
                {
                    case "requesting docking clearance":
                        IMyShipConnector dockingPort = Drone.Program.GridTerminalSystem.GetBlockWithName("Docking Port 1") as IMyShipConnector;

                        if (dockingPort == null)
                        {
                            throw new Exception("Docking Port 1 not found.");
                        } else
                        {
                            Vector3D connectorForward = dockingPort.WorldMatrix.Forward;
                            List<Vector3D> dockingPath = new List<Vector3D> { (dockingPort.GetPosition() + 10 * connectorForward), dockingPort.GetPosition() };

                            SerializableVector3D

                            Drone.NetworkService.BroadcastMessage("docking clearance ganted", Serializer.Serialize(dockingPath));
                        }
                        break;
                }
            }

            public override string Name()
            {
                return "Miner";
            }
        }
    }
}
