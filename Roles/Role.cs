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
            protected const string DispatchChannel = "dispatch";
            public Drone drone;
            protected int State;

            protected Drone Drone
            {
                get
                {
                    return drone;
                }

                set
                {
                    drone = value;
                }
            }

            public Role()
            {

            }

            public IMyRemoteControl Remote()
            {
                return Drone.Remote;
            }

            public virtual void Perform()
            {
                Drone.Program.Echo("No proper roles assigned. Shutting Down.");
                Drone.Shutdown();
                Drone.Sleep();
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
