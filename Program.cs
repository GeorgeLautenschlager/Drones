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
    partial class Program : MyGridProgram
    {
        MyCommandLine _commandLine = new MyCommandLine();
        Drone drone;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remotes, rc => rc.CustomName == "Drone Brain" && rc.IsSameConstructAs(Me));
            if (remotes == null || remotes.Count == 0)
                throw new Exception("Drone has no Brain!");
            IMyRemoteControl remote = remotes.First();

            NetworkService networkService = new NetworkService(this, remote);
            ManeuverService maneuverService = new ManeuverService(this, remote, 1);
            Echo("Services ready, building drone.");
            drone = new Drone(this, maneuverService, networkService);

            //TODO: handle multiple roles
            string roleString = Me.CustomData as string;
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

            Role[] roles = new Role[1] { role };
            drone.SetRoles(roles);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{DateTime.Now.ToString()}");
            Echo("Callbacks First");

            handleCallback(argument);

            Echo("Now drone stuff");
            drone.Act();
        }

        public void handleCallback(string callback)
        {
            Echo($"Processing callback: {callback}");
            //TODO: this needs to be waaaaaay more general, obviously
            switch (callback)
            {
                case "docking_request_pending":
                    Echo("Responding to docking request");
                    DroneController dc = this.drone.roles[0] as DroneController;
                    dc.ProcessDockingRequest();
                    break;
                case "docking_request_granted":
                    Echo("Docking clearance received.");
                    Miner m = this.drone.roles[0] as Miner;
                    m.AcceptDockingClearance();
                    break;
            }
        }
    }
}