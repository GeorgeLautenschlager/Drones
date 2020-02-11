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
        public class Test : Role
        {
            Drone drone;
            int State;

            public Test(Drone drone)
            {
                this.Drone = drone;
                this.State = 0;
            }

            public void perform()
            {
                switch (this.State)
                {
                    case 0:
                        //Turn to the right
                        break;
                    case 1:
                        //Translate Forward 10 metres
                        break;
                }
            }
        }
    }
}
