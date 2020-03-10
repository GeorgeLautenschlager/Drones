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
        /*
         * This is the entry point and ultimate container of all drone logic. Behaviours may live elsewhere, and actions will
         * probably live in statis utility classes, but messages passed to or from a drone do so through the interface of this class.
        */
        public class Drone
        {

            private const float DEFAULT_SPEED_LIMIT = 5;
            public Program Program;
            public List<Role> Roles;
            public ManeuverService ManeuverService;
            public NetworkService NetworkService;
            private List<IMyGyro> Gyros = new List<IMyGyro>();
            private List<IMyThrust>Thrusters = new List<IMyThrust>();
            private List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();
            private List<IMyGasTank> FuelTanks = new List<IMyGasTank>();
            private List<IMyTerminalBlock> InventoryBlocks = new List<IMyTerminalBlock>();
            public IMyShipConnector DockingConnector;
            public IMyRemoteControl Remote;
            public IMyTextPanel CallbackLog;
            public MyFixedPoint MaxCargo = 0;

            public Drone(Program program, List<Role> roles)
            {
                this.Program = program;
                this.NetworkService = new NetworkService(this.Program, this);

                this.Roles = roles;
                foreach(Role role in this.Roles)
                {
                    role.drone = this;
                    role.InitWithDrone(this);
                }

                InitializeBrain();
                InitializeBlocks();

                List<IMyTextPanel> lcds = new List<IMyTextPanel>();
                Grid().GetBlocksOfType<IMyTextPanel>(lcds, rc => rc.CustomName == "callback_log" && rc.IsSameConstructAs(Program.Me));
                CallbackLog = lcds.First();

                this.ManeuverService = new ManeuverService(this.Program, Remote, this);

                Program.Echo("Drone Initialized");
                LogToLcd($"My address is: {Program.Me.EntityId.ToString()}");
            }

            private void InitializeBrain()
            {
                List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();
                Grid().GetBlocksOfType<IMyRemoteControl>(remotes, rc => rc.CustomName == "Drone Brain" && rc.IsSameConstructAs(Program.Me));
                if (remotes == null || remotes.Count == 0)
                    throw new Exception("Drone has no Brain!");
                IMyRemoteControl remote = remotes.First();
                
                this.Remote = remote;
            }

            private void InitializeBlocks()
            {
                Grid().GetBlocksOfType<IMyGyro>(Gyros, block => block.IsSameConstructAs(Program.Me));
                if (Gyros == null || Gyros.Count == 0)
                    throw new Exception("Drone has no Gyros!");
                
                Grid().GetBlocksOfType<IMyThrust>(Thrusters, block => block.IsSameConstructAs(Program.Me));
                if (Thrusters == null || Thrusters.Count == 0)
                    throw new Exception("Drone has no Thrusters!");

                Grid().GetBlocksOfType<IMyBatteryBlock>(Batteries, block => block.IsSameConstructAs(Program.Me));
                if (Batteries == null || Batteries.Count == 0)
                    throw new Exception("Drone has no Batteries!");

                Grid().GetBlocksOfType<IMyGasTank>(FuelTanks, block => block.IsSameConstructAs(Program.Me));
                if (FuelTanks == null || FuelTanks.Count == 0)
                    throw new Exception("Drone has no Fuel Tanks!");

                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                //TODO: make sure you don't grab a connector on the other grid
                Grid().GetBlocksOfType(connectors, block => block.IsSameConstructAs(Program.Me));
                if (connectors == null || connectors.Count == 0)
                    throw new Exception("No docking connector found!");
                DockingConnector = connectors.First();

                InventoryBlocks = new List<IMyTerminalBlock>();
                Grid().GetBlocksOfType<IMyTerminalBlock>(InventoryBlocks, block => block.InventoryCount > 0 && block.IsSameConstructAs(Program.Me));

                if (InventoryBlocks != null && InventoryBlocks.Count > 1)
                {
                    foreach (IMyTerminalBlock block in InventoryBlocks)
                    {
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            MaxCargo += block.GetInventory(i).MaxVolume;
                        }
                    }
                }
            }

            public void Perform()
            {
                foreach (Role role in Roles)
                {
                    role.Perform();
                }
            }

            public void Startup()
            {
                foreach(IMyBatteryBlock battery in Batteries)
                {
                    battery.ChargeMode = ChargeMode.Auto;
                }

                foreach(IMyThrust thruster in Thrusters)
                {
                    thruster.Enabled = true;
                }

                foreach(IMyGyro gyro in Gyros)
                {
                    gyro.Enabled = true;
                }
            }

            public void OpenFuelTanks()
            {
                foreach(IMyGasTank tank in FuelTanks)
                {
                    tank.Stockpile = false;
                }
            }

            public void Shutdown()
            {
                foreach(IMyBatteryBlock battery in Batteries)
                {
                    battery.ChargeMode = ChargeMode.Recharge;
                }

                foreach(IMyThrust thruster in Thrusters)
                {
                    thruster.Enabled = false;
                }

                foreach(IMyGyro gyro in Gyros)
                {
                    gyro.Enabled = false;
                }

                foreach(IMyGasTank tank in FuelTanks)
                {
                    tank.Stockpile = true;
                }
            }

            public void Wake()
            {
                Program.Runtime.UpdateFrequency = UpdateFrequency.Update10;
            }

            public void Sleep()
            {
                Program.Runtime.UpdateFrequency = UpdateFrequency.None;
            }

            //Set up a Broadcast listener and callback so this drone can respond to messages on the given channel.
            public void ListenToChannel(string channel)
            {
                NetworkService.RegisterBroadcastListener(channel);
            }

            public void HandleCallback(string callback)
            {
                foreach (Role role in Roles)
                {
                    role.HandleCallback(callback);
                }
            }

            public MyFixedPoint CurrentCargo()
            {
                MyFixedPoint currentCargo = 0;

                foreach (IMyTerminalBlock block in InventoryBlocks)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        currentCargo += block.GetInventory(i).CurrentVolume;
                    }
                }

                return currentCargo;
            }

            public void Log(string text)
            {
                this.Program.Echo(text);
            }

            public void LogToLcd(string text)
            {
                if (CallbackLog == null)
                    return;

                CallbackLog.WriteText($"{text}\n", true);
            }

            public IMyGridTerminalSystem Grid()
            {
                return Program.GridTerminalSystem;
            }
        
            public void AllStop()
            {
                foreach(IMyGyro gyro in Gyros)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = false;
                }

                foreach(IMyThrust thruster in Thrusters)
                {
                    thruster.Enabled = true;
                    thruster.ThrustOverride = 0;
                }
            }
        
            public bool FlyTo(Vector3D position, IMyTerminalBlock reference, bool align, double speedLimit = -1)
            {
                LogToLcd($"{DateTime.Now}");
                Vector3D translationVector = position - reference.GetPosition();

                Log($"D: {translationVector.Length()}");
                if(translationVector.Length() < 0.1)
                {
                    Log("Arrived at target, All Stop.");
                    AllStop();
                    return true;
                }

                double targetSpeed;
                if (speedLimit == -1)
                {
                    targetSpeed = Math.Pow(translationVector.Length(), 1 / 2.1);
                }
                else
                {
                    targetSpeed = speedLimit;
                }

                Vector3D targetVelocity = Vector3D.Normalize(translationVector) * targetSpeed;
                Vector3D velocityDelta = targetVelocity - Remote.GetShipVelocities().LinearVelocity;

                LogToLcd($"dV: {velocityDelta.Length()}\n");
                if (velocityDelta.Length() < MathHelper.Clamp(translationVector.Length()/1000, 0.25, 5))
                    return false;

                Vector3D transformedVelocityDelta = Vector3D.TransformNormal(velocityDelta, MatrixD.Transpose(Remote.WorldMatrix));

                Vector3D projection;
                Vector3D directionVector;

                //Z (Forward and Backward)
                directionVector = new Vector3D(Remote.WorldMatrix.Down);
                projection = Vector3D.ProjectOnVector(ref transformedVelocityDelta, ref directionVector);
                LogToLcd($"dVz: {projection.Length()}m/s");
                this.ManeuverService.SetThrust(projection, align);

                //X (Up and Down)
                directionVector = new Vector3D(Remote.WorldMatrix.Right);
                projection = Vector3D.ProjectOnVector(ref transformedVelocityDelta, ref directionVector);
                LogToLcd($"dVx: {projection.Length()}m/s");
                this.ManeuverService.SetThrust(projection, align);

                //Y (Left and Right
                directionVector = new Vector3D(Remote.WorldMatrix.Forward);
                projection = Vector3D.ProjectOnVector(ref transformedVelocityDelta, ref directionVector);
                LogToLcd($"dVy: {projection.Length()}m/s");
                this.ManeuverService.SetThrust(projection, align);
                LogToLcd($"{DateTime.Now}\n\n");

                return false;
            }

            public void RegisterUnicastRecipient(string name, long address)
            {
                NetworkService.RegisterUnicastRecipient(name, address);
            }
        }
    }
}
