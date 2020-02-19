﻿using Sandbox.Game.EntityComponents;
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
            public static Role Build(string roleName, MyIniValue config)
            {
                Role builtRole = null;

                switch (roleName)
                {
                    case "Miner":
                        builtRole = new Miner(config);
                        break;
                    //case "Drone Controller":
                    //    builtRole = new DroneController(config);
                    //    break;
                    //case "Tester":
                    //    builtRole = new Tester(config);
                    //    break;
                    default:
                        throw new Exception($"Unable to build role: {roleName}");
                        break;

                }

                return builtRole;
            }
        }
    }
}
