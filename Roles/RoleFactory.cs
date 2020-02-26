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
        public class RoleFactory
        {
            public static Role Build(string roleName, MyIni config)
            {
                Role builtRole;

                switch (roleName)
                {
                    case "miner":
                        builtRole = new Miner(config);
                        break;
                    case "drone_controller":
                        builtRole = new DroneController(config);
                        break;
                    case "tester":
                        builtRole = new Tester(config);
                        break;
                    default:
                        throw new Exception($"Unable to build role: {roleName}");
                        break;

                }

                return builtRole;
            }
        }
    }
}
