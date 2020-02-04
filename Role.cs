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
        public class Role
        {
            protected const string DockingRequestChannel = "docking_requests";
            protected Drone Drone;
            protected int State;

            public Role()
            {

            }

            public virtual void Perform()
            {
                drone.Shutdown();
            }

            public override string ToString()
            {
                return this.Name();
            }

            public virtual string Name()
            {
                return "Generic Role";
            }
        }
    }
}
