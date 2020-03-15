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
using System.Diagnostics;

namespace IngameScript
{
    partial class Program
    {
        public class Tester : Role
        {
            /* 
                * This is a test role
            */

            public Tester(MyIni config)
            {
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                Drone = drone;
                State = 0;
            }

            public void ParseConfig(MyIni config)
            {
                
            }

            public override void Perform()
            {
                Vector3D position = new Vector3D(0, 0, 0);

                if (Drone.ManeuverService.AlignBlockTo(position, Drone.Remote))
                {
                    Drone.Sleep();
                    Vector3D translationVector = position - Drone.Remote.GetPosition();

                    Vector3D transformedTranslationVector = Vector3D.TransformNormal(translationVector, MatrixD.Transpose(Drone.Remote.WorldMatrix));
                    Vector3D proj, dir;

                    Drone.LogToLcd($"Position: {Drone.Remote.GetPosition()}");
                    Drone.LogToLcd($"Dist: {transformedTranslationVector.Length()}");
                    dir = new Vector3D(Drone.Remote.WorldMatrix.Forward);
                    proj = Vector3D.ProjectOnVector(ref transformedTranslationVector, ref dir);
                    Drone.LogToLcd($"z: {proj.Length()}");
                    dir = new Vector3D(Drone.Remote.WorldMatrix.Right);
                    proj = Vector3D.ProjectOnVector(ref transformedTranslationVector, ref dir);
                    Drone.LogToLcd($"x: {proj.Length()}");
                    dir = new Vector3D(Drone.Remote.WorldMatrix.Down);
                    proj = Vector3D.ProjectOnVector(ref transformedTranslationVector, ref dir);
                    Drone.LogToLcd($"y: {proj.Length()}");
                }
                else
                {
                    Drone.Wake();
                }
                
            }

            public override string Name()
            {
                return "tester";
            }

            public override void HandleCallback(string callback)
            {
                
            }
        }
    }
}
