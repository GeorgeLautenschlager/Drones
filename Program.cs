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
            List<string> sections = new List<string>();
            config.GetSections(sections);

            List<Role> roles = new List<Role>();
            foreach (string section in sections)
            {
                roles.Add(RoleFactory.Build(section, config));
            }

            this.drone = new Drone(this, roles);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //Echo($"{DateTime.Now.ToString()}");
            drone.HandleCallback(argument);
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