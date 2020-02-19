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
        private Drone drone;
        public Program()
        {
            MyIni config = ParseIni();
            List<MyIniKey> roleKeys = new List<MyIniKey>();
            config.GetKeys("Roles", roleKeys);

            List<Role> roles = new List<Role>();

            foreach (MyIniKey roleKey in roleKeys)
            {
                roles.Add(RoleFactory.Build(roleKey.ToString(), config.Get(roleKey)));
            }

            this.drone = new Drone(this, roles);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{DateTime.Now.ToString()}");
            handleCallback(argument);
            drone.Perform();
        }

        public MyIni ParseIni()
        {
            MyIni ini = new MyIni();

            MyIniParseResult config;
            if (!ini.TryParse(Me.CustomData, out config))
                throw new Exception($"Error parsing config: {config.ToString()}");

            return ini;
        }

        public void handleCallback(string callback)
        {
            Echo($"Processing callback: {callback}");
            //TODO: this needs to be waaaaaay more general, obviously
            switch (callback)
            {
                case "callback_docking_request_pending":
                    Echo("Responding to docking request");
                    DroneController dc = drone.roles[0] as DroneController;
                    dc.ProcessDockingRequest();
                    break;
                case "callback_docking_request_granted":
                    Echo("Docking clearance received.");
                    Miner m = drone.roles[0] as Miner;
                    m.AcceptDockingClearance();
                    break;
            }
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
    }
}