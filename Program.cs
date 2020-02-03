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
        Drone drone;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remotes, rc => rc.CustomName == "Drone Brain" && rc.IsSameConstructAs(Me));
            if (remotes == null || remotes.Count == 0)
                throw new Exception("Drone has no Brain!");
            IMyRemoteControl remote = remotes.First();
            
            ManeuverService maneuverService = new ManeuverService(this, remote, 10);
            NetworkService networkService = new NetworkService(this, remote);

            drone = new Drone(this, maneuverService, networkService);
            //TODO: pull roles from Custom Data
            Role[] roles = new Role[1] { new Miner(drone) };
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
            drone.Act();
        }
    }
}
