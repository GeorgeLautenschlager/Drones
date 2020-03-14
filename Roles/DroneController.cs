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
            //TODO: generalize and automate the tacking of owned drones and the assignment of drones to a controller
            private long MinerAddress;
            private long SurveyorAddress;

            IMySoundBlock DroneKlaxon;
            Vector3D DepositCentre;
            Vector3D DepositNormal;
            double DepositDepth;

            public string Channel { get; private set; }

            public DroneController(MyIni config)
            {
                // TODO: move to proper ParseInit method and use TryGet instead
                string rawValue = config.Get(Name(), "miner_address").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Int64.TryParse(rawValue, out MinerAddress);
                }
                else
                {
                    throw new Exception("Drone has no address for its Miner!");
                }

                rawValue = config.Get(Name(), "surveyor_address").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Int64.TryParse(rawValue, out SurveyorAddress);
                }
                else
                {
                    throw new Exception("Drone has no address for its Miner!");
                }

                rawValue = config.Get(Name(), "deposit_centre").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Vector3D.TryParse(rawValue, out DepositCentre);
                }
                else
                {
                    throw new Exception("Deposit Centre not set");
                }

                rawValue = config.Get(Name(), "deposit_normal").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Vector3D.TryParse(rawValue, out DepositNormal);
                }
                else
                {
                    throw new Exception("Deposit Normal not set");
                }

                rawValue = config.Get(Name(), "deposit_depth").ToString();
                if (rawValue != null && rawValue != "")
                {
                    Double.TryParse(rawValue, out DepositDepth);
                }
                else
                {
                    throw new Exception("Deposit Centre not set");
                }
            }
            public override void InitWithDrone(Drone drone)
            {
                Drone = drone;
                Drone.ListenToChannel(DockingRequestChannel);
                Drone.NetworkService.RegisterBroadcastCallback(DockingRequestChannel, "docking_request_pending");
                Drone.Program.IGC.UnicastListener.SetMessageCallback("unicast");

                List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
                Drone.Grid().GetBlocksOfType<IMySoundBlock>(soundBlocks, rc => rc.CustomName == "Drone Klaxon" && rc.IsSameConstructAs(Drone.Program.Me));
                DroneKlaxon = soundBlocks.First();
                DroneKlaxon.LoopPeriod = 2f;
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
                        MyIGCMessage message = Drone.NetworkService.GetUnicastListener().AcceptMessage();

                        if (message.Data == null)
                            Drone.LogToLcd($"\nNo Message");

                        if (message.Tag == DockingRequestChannel)
                        {
                            Drone.LogToLcd("\nReceived Docking Request");
                            IMyShipConnector dockingPort = GetFreeDockingPort();

                            if (dockingPort == null)
                            {
                                Drone.LogToLcd("\nNo Free Docking Port");
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
                        }
                        else if (message.Tag == "Notifications")
                        {
                            Drone.LogToLcd($"Received notification:{message.Data.ToString()}");
                            DroneKlaxon.LoopPeriod = 2f;
                            DroneKlaxon.Play();
                        }
                        else if (message.Tag == "survey_reports")
                        {
                            MyTuple<long, string, double, Vector3D, Vector3D, Vector3D> report = (MyTuple<long, string, double, Vector3D, Vector3D, Vector3D>)message.Data;
                            Drone.LogToLcd($"Received survey_report: {report.ToString()}");

                            //TODO: This needs to be in a persistence layer, not the CustomData
                            MyIni ini = new MyIni();
                            MyIniParseResult config;
                            if (!ini.TryParse(Drone.Program.Me.CustomData, out config))
                                throw new Exception($"Error parsing config: {config.ToString()}");

                            //TODO: what about multiple deposits?
                            ini.Set(report.Item1.ToString(), "deposit_type", report.Item2.ToString());
                            ini.Set(report.Item1.ToString(), "deposit_depth", report.Item3.ToString());
                            ini.Set(report.Item1.ToString(), "top_left_corner", report.Item4.ToString());
                            ini.Set(report.Item1.ToString(), "top_right_corner", report.Item5.ToString());
                            ini.Set(report.Item1.ToString(), "bottom_left_corner", report.Item6.ToString());

                            Drone.Program.Me.CustomData = ini.ToString();
                        }

                        break;
                    case "docking_request_pending":
                        ProcessDockingRequest();
                        break;
                    case "recall_miner":
                        Drone.LogToLcd("Recalling miner");
                        Drone.Program.IGC.SendUnicastMessage(MinerAddress, "recall", "recall");
                        break;
                    case "recall_surveyor":
                        Drone.LogToLcd("Recalling surveyor");
                        Drone.Program.IGC.SendUnicastMessage(SurveyorAddress, "recall", "recall");
                        break;
                    case "deploy_miner":
                        Drone.LogToLcd("Launching miner");

                        List<IMyProgrammableBlock> miners = new List<IMyProgrammableBlock>();
                        Drone.Grid().GetBlocksOfType(miners, pb => MyIni.HasSection(pb.CustomData, "miner"));

                        //We only support one miner for now
                        // Set the deposit details in the miners config
                        IMyProgrammableBlock miner = miners.First();
                        MyIni minerConfig = new MyIni();
                        MyIniParseResult result;
                        if (!minerConfig.TryParse(miner.CustomData, out result))
                        {
                            Drone.LogToLcd(miner.CustomData);
                            throw new Exception($"Error parsing config: {result.ToString()}");
                        }

                        Vector3D MiningSite = DepositCentre + 10 * Vector3D.Normalize(DepositNormal);
                        minerConfig.Set("miner", "mining_site", MiningSite.ToString());

                        Vector3D TunnelEnd = DepositCentre - DepositDepth * Vector3D.Normalize(DepositNormal);
                        minerConfig.Set("miner", "tunnel_end", TunnelEnd.ToString());

                        miner.CustomData = minerConfig.ToString();

                        miner.TryRun("launch");
                        break;
                    case "deploy_surveyor":
                        Drone.LogToLcd("Launching miner");

                        List<IMyProgrammableBlock> surveyors = new List<IMyProgrammableBlock>();
                        Drone.Grid().GetBlocksOfType(surveyors, pb => MyIni.HasSection(pb.CustomData, "surveyor"));
                        IMyProgrammableBlock surveyor = surveyors.First();

                        surveyor.TryRun("launch");
                        break;
                    case "echo":
                        Drone.LogToLcd("Echo");
                        DroneKlaxon.Play();
                        break;
                    case "":
                        // Just Ignore empty arguments
                        break;
                    default:
                        Drone.LogToLcd($"\nDrone received unrecognized callback: {callback}");
                        break;
                }
            }

            private IMyShipConnector GetFreeDockingPort()
            {
                IMyShipConnector dockingPort = Drone.Grid().GetBlockWithName("Docking Port 1") as IMyShipConnector;
                List<IMyShipConnector> connectors = new List<IMyShipConnector>();

                Drone.Grid().GetBlocksOfType<IMyShipConnector>(connectors, connector =>
                {
                    return MyIni.HasSection(connector.CustomData, "docking_port") && connector.Status == MyShipConnectorStatus.Unconnected;
                });

                return connectors.FirstOrDefault();
            }

            public override string Name()
            {
                return "drone_controller";
            }
        }
    }
}
