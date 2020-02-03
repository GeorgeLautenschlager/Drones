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
            private Program Program;
            private Role[] roles;
            private ManeuverService ManeuverService;
            private NetworkService NetworkService;

            public Drone(Program program, ManeuverService maneuverService, NetworkService networkService)
            {
                this.Program = program;
                this.roles = roles;
                this.ManeuverService = maneuverService;
                this.NetworkService = networkService;

                string[] roleNames = new string[roles.Length];
                roleNames = roles.Select(role => role.ToString()).ToArray();

                program.Echo($"Initializing drone with roles: {String.Join(",", roleNames)}");
            }

            public void SetRoles(Role[] roles)
            {
                this.roles = roles;
            }

            public void Act()
            {
                // TODO: support multiple roles
                this.roles.First().Perform();

                //Log("Going to mining site");
                //maneuverService.GoToPosition(new Vector3D(141232.17, -72348.93, -61066.09));
            }

            public void RequestDockingClearance()
            {
                Program.Echo("shutting down");
            }

            public void Shutdown()
            {
                Program.Echo("shutting down");
            }

            private void Log(string text)
            {
                this.Program.Echo(text);
            }
        }
    }
}
