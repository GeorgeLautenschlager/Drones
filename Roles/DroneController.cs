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
            private long MinerAddress;
            public string Channel { get; private set; }

            public DroneController(MyIni config)
            {
                string rawValue = config.Get(Name(), "miner_address").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Int64.TryParse(rawValue, out MinerAddress);
                }
                else
                {
                    throw new Exception("Drone has no address for its Miner!");
                }
            }
            public override void InitWithDrone(Drone drone)
            {
                Drone = drone;
                Drone.ListenToChannel(DockingRequestChannel);
                Drone.NetworkService.RegisterBroadcastCallback(DockingRequestChannel, "docking_request_pending");
                Drone.Program.IGC.UnicastListener.SetMessageCallback("unicast");
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
                    List<Vector3D> dockingPath = new List<Vector3D> { approachPoint, dockingPort.GetPosition()};
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
                    case "unicast":
                        // We only support one Unicast for now
                        Drone.LogToLcd($"\nProcessing Docking Request: {DateTime.Now}");
                        MyIGCMessage message = Drone.NetworkService.GetUnicastListener().AcceptMessage();

                        if (message.Data == null)
                            Drone.LogToLcd($"\nNo Message");

                        IMyShipConnector dockingPort = Drone.Grid().GetBlockWithName("Docking Port 1") as IMyShipConnector;

                        if (dockingPort == null)
                        {
                            Drone.LogToLcd("\nDocking Port 1 not found.");
                        }
                        else
                        {
                            MyTuple<Vector3D, Vector3D, Vector3D> payload = new MyTuple<Vector3D, Vector3D, Vector3D>();
                            payload.Item1 = dockingPort.GetPosition() + dockingPort.WorldMatrix.Forward * 40;
                            payload.Item2 = dockingPort.GetPosition() + 1.5 * dockingPort.WorldMatrix.Forward;

                            Drone.LogToLcd($"\nClearance granted: {message.Source}");
                            Drone.LogToLcd($"\nApproach: {payload.Item1.ToString()}");
                            Drone.LogToLcd($"\nDocking Port: { payload.Item2.ToString()}");

                            Drone.Program.IGC.SendUnicastMessage(message.Source, DockingRequestChannel, payload);
                        }

                        break;
                    case "docking_request_pending":
                        ProcessDockingRequest();
                        break;
                    case "recall":
                        Drone.LogToLcd("Recalling drones");
                        Drone.Program.IGC.SendUnicastMessage(MinerAddress, "recall", "recall");
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
