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
        public class Survey
        {
            public long Asteroid;
            private Vector3D TopLeft;
            private Vector3D TopRight;
            private Vector3D BottomLeft;
            private long ParentAddress;
            private string State;
            private string DepositType;
            private double Depth;

            public Survey(long asteroidId, double depth, string depositType)
            {
                State = "initial";
                Asteroid = asteroidId;
                Depth = depth;
                DepositType = depositType;
            }

            public void Mark(Vector3D point)
            {
                switch (State)
                {
                    case "initial":
                        TopLeft = point;
                        State = "TopLeftMarked";
                        break;
                    case "TopLeftMarked":
                        TopRight = point;
                        State = "TopRightMarked";
                        break;
                    case "TopRightMarked":
                        BottomLeft = point;
                        State = "BottomLeftMarked";
                        break;
                }
            }

            public MyTuple<long, string, double, Vector3D, Vector3D, Vector3D> Report()
            {
                if (TopLeft == null || TopRight == null || BottomLeft == null)
                    throw new Exception("Survey not complete!");

                // TODO: Can I introduce a dummy type to make this nicer to work with?
                return new MyTuple<long, string, double, Vector3D, Vector3D, Vector3D>(
                    Asteroid,
                    DepositType,
                    Depth,
                    TopLeft,
                    TopRight,
                    BottomLeft
                );
            }
        }
    }
}
