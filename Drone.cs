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
            public Role[] roles;
            public ManeuverService ManeuverService;
            public NetworkService NetworkService;
            private List<IMyGyro> Gyros = new List<IMyGyro>();
            private List<IMyThrust> Thrusters = new List<IMyThrust>();
            private List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();
            private List<IMyGasTank> FuelTanks = new List<IMyGasTank>();
            public IMyRemoteControl Remote;

            public Drone(Program program, NetworkService networkService, IMyRemoteControl remote)
            {
                this.Program = program;
                this.NetworkService = new NetworkService();

                InitializeBrain();
                InitializeBlocks();
                InitializeRoles();

                Program.Echo("Drone Initialized");
            }

            private void InitializeBrain()
            {
                List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();
                GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remotes, rc => rc.CustomName == "Drone Brain" && rc.IsSameConstructAs(Me));
                if (remotes == null || remotes.Count == 0)
                    throw new Exception("Drone has no Brain!");
                IMyRemoteControl remote = remotes.First();
                
                this.Remote = remote;
            }

            private void InitializeBlocks()
            {
                GridTerminalSystem.GetBlocksOfType<IMyGyros>(Gyros);
                if (Gyros == null || Gyros.Count == 0)
                    throw new Exception("Drone has no Gyros!");
                
                GridTerminalSystem.GetBlocksOfType<IMyGyros>(Thrusters);
                if (Thrusters == null || Thrusters.Count == 0)
                    throw new Exception("Drone has no Thrusters!");

                GridTerminalSystem.GetBlocksOfType<IMyGyros>(Batteries);
                if (Batteries == null || Batteries.Count == 0)
                    throw new Exception("Drone has no Batteries!");

                GridTerminalSystem.GetBlocksOfType<IMyGyros>(FuelTanks);
                if (FuelTanks == null || FuelTanks.Count == 0)
                    throw new Exception("Drone has no Fuel Tanks!");
            }

            public void InitializeRoles()
            {
                // TODO: handle multiple roles
                // TODO: instantiate roles dynamically
                string roleString = Remote.CustomData as string;
                Role role = null;

                if (roleString == "miner")
                {
                    Echo("Assigning Role: Miner");
                    role = new Miner(drone);
                }
                
                if (roleString == "drone controller")
                {
                    Echo("Assigning Role: Drone Controller");
                    role = new DroneController(drone);
                }

                if (roleString == "tester")
                {
                    role = new Tester(drone);
                }

                if (role == null)
                    throw new Exception("Drone has no Roles assigned!");

                this.roles = new Role[1] { role };
            }

            public void Perform()
            {
                Wake();
                // TODO: support multiple roles
                this.roles[0].Perform();
            }

            public void Startup()
            {
                foreach(IMyBatteryBlock battery in Batteries)
                {
                    battery.ChargeMode = ChargeMode.Auto;
                }

                foreach(IMyThrust thruster in Thrusters)
                {
                    Thruster.Enabled = true;
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
                    Thruster.Enabled = false;
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
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }

            public void Sleep()
            {
                Runtime.UpdateFrequency = UpdateFrequency.Once;
            }
            
            // Begin moving to destination
            public void Move(Vector3D destination, string destinationName, bool dockingMode, Direction direction = Base6Directions.Direction.Forward)
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
            public bool Moving(Vector3D destination, Vector3D docking)
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
                    speedLimit = Math.Pow(distance, 1/2.1);
                    // If not docking, then we can apply a mulitplier proportional to the order of magnitude of distance
                    if (docking == false)
                        speedLimit *= Math.Log(distance);
                    //Don't apply speed limits outside of this valid range
                    Remote.SpeedLimit = MathHelper.Clamp(speedLimit, 1, 100);
                }

                return moveComplete;
            }

            //Set up a Broadcast listener and callback so this drone can respond to messages on the given channel.
            public void ListenToChannel(string channel)
            {
                NetworkService.RegisterBroadcastListener(channel);
            }

            private void Log(string text)
            {
                this.Program.Echo(text);
            }
        }
    }
}
