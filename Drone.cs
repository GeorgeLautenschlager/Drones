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
            public Program Program;
            public Role[] roles;
            public ManeuverService ManeuverService;
            public NetworkService NetworkService;

            public Drone(Program program, ManeuverService maneuverService, NetworkService networkService)
            {
                this.Program = program;
                this.ManeuverService = maneuverService;
                this.NetworkService = networkService;
                Program.Echo("Drone equipped");
            }

            public void SetRoles(Role[] roles)
            {
                this.roles = roles;

                string[] roleNames = new string[this.roles.Length];
                roleNames = this.roles.Select(role => role.ToString()).ToArray();
                Program.Echo($"Initializing drone with roles: {String.Join(",", roleNames)}");
            }

            public void Act()
            {
                if (this.roles == null || this.roles.Length == 0)
                    throw new Exception("No Roles assigned!");

                // TODO: support multiple roles
                this.roles[0].Perform();
            }

            public void FlyToCoordinates(Vector3D position)
            {
                ManeuverService.GoToPosition(position);
            }

            //Set up a Broadcast listener and callback so this drone can respond to messages on the given channel.
            public void ListenToChannel(string channel)
            {
                NetworkService.RegisterBroadcastListener(channel);
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
