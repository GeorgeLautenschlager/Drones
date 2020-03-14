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
        public class Surveyor : Role
        {
            private Vector3D DeparturePoint;
            private new string State;
            private MyDetectedEntityInfo Obstacle;
            private Move Move;
            private DockingAttempt DockingAttempt;
            private Survey Survey;

            public Surveyor(MyIni config)
            {
                State = "initial";
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                Drone = drone;
                Drone.Program.IGC.UnicastListener.SetMessageCallback("unicast");
                Drone.Wake();
            }

            private void ParseConfig(MyIni config)
            {
                if (!config.Get(Name(), "parent_address").TryGetInt64(out ParentAddress))
                    throw new Exception("Parent address is missing");

                config.Get(Name(), "parent_address").TryGetString(out State);
            }

            public override void Perform()
            {
                Drone.Log($"Performing surveyor: {State}");
                switch (this.State)
                {
                    case "initial":
                        // Startup and Depart
                        Drone.Wake();
                        Drone.Startup();
                        Drone.DockingConnector.Disconnect();
                        Drone.DockingConnector.Enabled = false;
                        Drone.OpenFuelTanks();
                        Drone.Eye.EnableRaycast = true;

                        DeparturePoint = Drone.DockingConnector.GetPosition() + 15 * Drone.DockingConnector.WorldMatrix.Backward;
                        State = "departing";
                        break;
                    case "departing":
                        if (Move == null)
                            Move = new Move(Drone, new Queue<Vector3D>(new[] { DeparturePoint }), Drone.Remote, false);

                        // Departing
                        if (Move.Perform())
                        {
                            Move = null;
                            State = "point";
                        }
                        break;
                    case "point":
                        // Waiting to be oriented by the player
                        Drone.Sleep();
                        break;
                    case "preparing to move":
                        // fly to obstacle immediately in front and stop on the nearest bounding box corner
                        Drone.Log($"Scan Range: {Drone.Eye.AvailableScanRange}");
                        if (Drone.Eye.CanScan(20000))
                        {
                            Obstacle = Drone.Eye.Raycast(20000, 0, 0);
                            if (!Obstacle.IsEmpty())
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Clear();
                                sb.Append("EntityID: " + Obstacle.EntityId);
                                sb.AppendLine();
                                sb.Append("Name: " + Obstacle.Name);
                                sb.AppendLine();
                                sb.Append("Type: " + Obstacle.Type);
                                sb.AppendLine();
                                sb.Append("Velocity: " + Obstacle.Velocity.ToString("0.000"));
                                sb.AppendLine();
                                sb.Append("Relationship: " + Obstacle.Relationship);
                                sb.AppendLine();
                                sb.Append("Size: " + Obstacle.BoundingBox.Size.ToString("0.000"));
                                sb.AppendLine();
                                sb.Append("Position: " + Obstacle.Position.ToString("0.000"));
                                Drone.LogToLcd($"Found Obstacle: \n{sb}");

                                State = "shoot";
                            }
                        }
                        break;
                    case "shoot":
                        MyTuple<double, Vector3D> candidate = new MyTuple<double, Vector3D>(
                            (Obstacle.BoundingBox.GetCorners().First() - Drone.Remote.GetPosition()).Length(),
                            Obstacle.BoundingBox.GetCorners().First()
                        );

                        foreach (Vector3D corner in Obstacle.BoundingBox.GetCorners())
                        {
                            double distance = (corner - Drone.Remote.GetPosition()).Length();
                            if (distance < candidate.Item1)
                            {
                                candidate.Item1 = distance;
                                candidate.Item2 = corner;
                            }
                        }

                        Move = new Move(Drone, new Queue<Vector3D>(new[] { candidate.Item2 }), Drone.Remote, true);
                        State = "flying";
                        break;
                    case "flying":
                        if (Move.Perform())
                            State = "arrived";
                        break;
                    case "arrived":
                        Drone.Program.IGC.SendUnicastMessage(ParentAddress, "Notifications", "I have arrived at the survey site");
                        Drone.Sleep();
                        return;
                    case "surveying":
                        Drone.Sleep();
                        break;
                    case "returning":
                        Drone.Wake();

                        if (DockingAttempt == null)
                            DockingAttempt = new DockingAttempt(Drone, Drone.DockingConnector, ParentAddress, DockingRequestChannel);

                        if (DockingAttempt.Perform())
                            State = "shutting down";;
                        break;
                    case "shutting down":
                        DockingAttempt = null;
                        Drone.Shutdown();
                        Drone.Sleep();
                        break;
                }
            }

            public override void HandleCallback(string callback)
            {
                switch (callback)
                {
                    case "unicast":
                        ProcessUnicast();
                        break;
                    case "start_survey":
                        MyIni ini = new MyIni();
                        MyIniParseResult config;
                        if (!ini.TryParse(Drone.Program.Me.CustomData, out config))
                            throw new Exception($"Error parsing config: {config.ToString()}");

                        string depositType;
                        if (!ini.Get(Name(), "deposit_type").TryGetString(out depositType))
                            throw new Exception("deposit_type is missing");

                        double depth;
                        if (!ini.Get(Name(), "deposit_depth").TryGetDouble(out depth))
                            throw new Exception("depth is missing");

                        if (Drone.Eye.CanScan(500))
                            Obstacle = Drone.Eye.Raycast(500, 0, 0);

                        Survey = new Survey(Obstacle.EntityId, depth, depositType);
                        break;
                    case "mark":
                        Survey.Mark(Drone.Remote.GetPosition());
                        break;
                    case "submit_survey":
                        Drone.Program.IGC.SendUnicastMessage(ParentAddress, "survey_reports", Survey.Report());
                        break;
                    case "launch":
                        State = "initial";
                        Drone.Wake();
                        break;
                    case "recall":
                        Drone.Wake();
                        State = "returning";
                        break;
                    case "":
                        // Just Ignore empty arguments
                        break;
                    default:
                        Drone.LogToLcd($"\nDrone received unrecognized callback: {callback}");
                        break;
                }
            }

            private void ProcessUnicast()
            {
                MyIGCMessage message = Drone.NetworkService.GetUnicastListener().AcceptMessage();
                if (message.Data == null)
                    Drone.LogToLcd($"\nNo Message");

                // TODO: This could be a switch
                if (message.Tag == DockingRequestChannel && DockingAttempt != null)
                {
                    DockingAttempt.ProcessClearance(message);
                }
                else if (message.Tag == "recall")
                {
                    State = "returning";
                }
                else
                {
                    Drone.LogToLcd($"{message.Tag} is not a recognized message format.");
                }
            }

            public override string Name()
            {
                return "surveyor";
            }
        }
    }
}
