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
            private Program program;
            private Role[] roles;

            public Drone(Program program, Role[] roles)
            {
                this.program = program;
                this.roles = roles;

                string[] roleNames = new string[roles.Length];
                roleNames = roles.Select(role => role.ToString()).ToArray();


                program.Echo($"Initializing drone with roles: {String.Join(",", roleNames)}");
            }

            public void Act()
            {
                Log("Cogito Ergo Sum");
                // ask my roles what to do
            }

            private void Log(string text)
            {
                this.program.Echo(text);
            }
        }
    }
}
