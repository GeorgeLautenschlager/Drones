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
            public IMyRemoteControl Remote;
            public IMyTextPanel CallbackLog;
            public MyFixedPoint MaxCargo = 0;

            public Drone(Program program, List<Role> roles)
            {
                this.Program = program;
                this.NetworkService = new NetworkService(this.Program);

                this.Roles = roles;
                foreach(Role role in this.Roles)
                {
                    role.drone = this;
                    role.InitWithDrone(this);
                }

                InitializeBrain();
                InitializeBlocks();
                this.ManeuverService = new ManeuverService(this.Program, Remote);
                CallbackLog = Grid().GetBlockWithName("callback_log") as IMyTextPanel;

                Program.Echo("Drone Initialized");
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
                Grid().GetBlocksOfType<IMyGyro>(Gyros);
                if (Gyros == null || Gyros.Count == 0)
                    throw new Exception("Drone has no Gyros!");
                
                Grid().GetBlocksOfType<IMyThrust>(Thrusters);
                if (Thrusters == null || Thrusters.Count == 0)
                    throw new Exception("Drone has no Thrusters!");

                Grid().GetBlocksOfType<IMyBatteryBlock>(Batteries);
                if (Batteries == null || Batteries.Count == 0)
                    throw new Exception("Drone has no Batteries!");

                Grid().GetBlocksOfType<IMyGasTank>(FuelTanks);
                if (FuelTanks == null || FuelTanks.Count == 0)
                    throw new Exception("Drone has no Fuel Tanks!");

                InventoryBlocks = new List<IMyTerminalBlock>();
                Grid().GetBlocksOfType<IMyTerminalBlock>(InventoryBlocks, block => block.InventoryCount > 0);

                foreach(IMyTerminalBlock block in InventoryBlocks)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        MaxCargo += block.GetInventory(i).MaxVolume;
                    }
                }
            }

            public void Perform()
            {
                Wake();

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
                Program.Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }

            public void Sleep()
            {
                Program.Runtime.UpdateFrequency = UpdateFrequency.Once;
            }
            
            // Begin moving to destination
            public void Move(Vector3D destination, string destinationName, bool dockingMode, Base6Directions.Direction direction = Base6Directions.Direction.Forward)
            {
                Remote.ClearWaypoints();
                Remote.FlightMode = FlightMode.OneWay;
                Remote.Direction = direction;
                Remote.SetDockingMode(dockingMode);
                Remote.SpeedLimit = DEFAULT_SPEED_LIMIT;
                Remote.AddWaypoint(destination, destinationName);
                Remote.SetAutoPilotEnabled(true);
            }

            // Oversee a move to the given destination, return true when it's done
            public bool Moving(Vector3D destination, bool docking)
            {
                double distance = Vector3D.Distance(Remote.GetPosition(), destination);
                bool moveComplete = false;

                if (distance < 1)
                {
                    Remote.SetAutoPilotEnabled(false);
                    Remote.SpeedLimit = DEFAULT_SPEED_LIMIT;
                    moveComplete = true;
                }
                else
                {
                    // If docking, speed limit should decrease rapidly with distance
                    double speedLimit = Math.Pow(distance, 1/2.1);
                    // If not docking, then we can apply a mulitplier proportional to the order of magnitude of distance
                    if (docking == false && distance > 2000)
                        speedLimit *= Math.Log(distance);
                    //Don't apply speed limits outside of this valid range
                    Remote.SpeedLimit = (float)MathHelper.Clamp(speedLimit, 1, 100);
                }

                return moveComplete;
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

                CallbackLog.WriteText(text, true);
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
                    thruster.ThrustOverride = 0;
                }
            }
        
            public bool FlyTo(Vector3D position)
            {
                Vector3D translationVector = position - Remote.CenterOfMass;
                if(translationVector.Length() < 0.1)
                {
                    return true;
                }

                Vector3D targetVelocity = Vector3D.Normalize(translationVector) * Math.Pow(translationVector.Length(), 1/2.1);
                Vector3D velocityDelta = targetVelocity - Remote.GetShipVelocities().LinearVelocity;
                Vector3D transformedVelocityDelta = Vector3D.TransformNormal(velocityDelta, MatrixD.Transpose(Remote.WorldMatrix));

                Vector3D projection;
                Vector3D directionVector;

                //X
                directionVector = new Vector3D(Remote.WorldMatrix.Right);
                projection = Vector3D.ProjectOnVector(ref transformedVelocityDelta, ref directionVector);
                this.ManeuverService.SetThrust(projection);
                Log("Setting X thrust to: ");

                //Z
                directionVector = new Vector3D(Remote.WorldMatrix.Down);
                projection = Vector3D.ProjectOnVector(ref transformedVelocityDelta, ref directionVector);
                this.ManeuverService.SetThrust(projection);
                Log("Setting Y thrust to: ");

                //Y
                directionVector = new Vector3D(Remote.WorldMatrix.Forward);
                projection = Vector3D.ProjectOnVector(ref transformedVelocityDelta, ref directionVector);
                this.ManeuverService.SetThrust(projection);
                Log("Setting Z thrust to: ");

                return false;
            }
        }
    }
}
