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

            Role[] roles = new Role[1]{ new Miner() };

            string[] roleNames = new string[roles.Length];
            roleNames = roles.Select(role => role.ToString()).ToArray();

            Echo($"Building drone with roles: {String.Join(",", roleNames)}");

            drone = new Drone(this, roles);
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
            Echo("Are you alive?");
            drone.Act();
        }
    }
}
