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
        class VectorHelper
        {
            public static Vector3D GetDirection(IMyTerminalBlock b, Base6Directions.Direction dir)
            {
                return b.CubeGrid.GridIntegerToWorld(b.Position + Base6Directions.GetIntVector(dir));
            }

            public static Vector3 Normalize(Vector3 vectorIn)
            {
                Vector3 vectorOut = new Vector3();
                double dist = Math.Sqrt((vectorIn.X * vectorIn.X) + (vectorIn.Y * vectorIn.Y) + (vectorIn.Z * vectorIn.Z));
                vectorOut.X = (float)(vectorIn.X / dist);
                vectorOut.Y = (float)(vectorIn.Y / dist);
                vectorOut.Z = (float)(vectorIn.Z / dist);
                return vectorOut;
            }

            public static Vector3 Cross(Vector3 a, Vector3 b)
            {
                double x = (a.Y * b.Z) - (b.Y * a.Z);
                double y = (b.X * a.Z) - (a.X * b.Z);
                double z = (a.X * b.Y) - (b.X * a.Y);
                return new Vector3(x, y, z);
            }

            public static double AngleBetween(Vector3D a, Vector3D b)
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;

                // Clamped due to floating point errors
                if (Vector3D.IsUnit(ref a) && Vector3D.IsUnit(ref b))
                    return Math.Acos(MathHelper.Clamp(a.Dot(b), -1, 1));

                return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }

            public static int AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
            {
                Vector3 perp = Cross(fwd, targetDir);
                float dir = Vector3.Dot(perp, up);

                if (dir > 0.0)
                    return 1;
                else if (dir < 0.0)
                    return -1;
                else
                    return 0;
            }

            public static Vector3D CalculateHeadingVector(Vector3D targetVec, Vector3D velocityVec, bool driftComp)
            {
                if (!driftComp)
                    return targetVec;

                if (velocityVec.LengthSquared() < 100)
                    return targetVec;

                if (targetVec.Dot(velocityVec) > 0)
                    return VectorReflection(velocityVec, targetVec);
                else
                    return -velocityVec;
            }

            public static Vector3D VectorReflection(Vector3D a, Vector3D b)
            {
                Vector3D project_a = VectorProject(a, b);
                Vector3D reject_a = a - project_a;
                return project_a - reject_a;
            }

            public static void GetRotationAngles(Vector3D v_target, Vector3D v_front, Vector3D v_left, Vector3D v_up, out double yaw, out double pitch)
            {
                var projectTargetUp = VectorProject(v_target, v_up);
                var projTargetFrontLeft = v_target - projectTargetUp;

                yaw = VectorAngleBetween(v_front, projTargetFrontLeft);

                if (Vector3D.IsZero(projTargetFrontLeft) && !Vector3D.IsZero(projectTargetUp)) //check for straight up case
                    pitch = MathHelper.PiOver2;
                else
                    pitch = VectorAngleBetween(v_target, projTargetFrontLeft); //pitch should not exceed 90 degrees by nature of this definition

                // Check if yaw angle is left or right
                // Multiplied by -1 to convert from right hand rule to left hand rule
                yaw = -1 * Math.Sign(v_left.Dot(v_target)) * yaw;

                // Check if pitch angle is up or down
                pitch = Math.Sign(v_up.Dot(v_target)) * pitch;

                // Check if target vector is pointing opposite the front vector
                if (Math.Abs(yaw) <= 1E-6 && v_target.Dot(v_front) < 0)
                {
                    yaw = Math.PI;
                }
            }

            public static double DistanceBetween(Vector3D targetPos, IMyTerminalBlock block)
            {
                return Math.Round(Vector3D.Distance(targetPos, block.GetPosition()), 2);
            }

            public static double DistanceBetween(Vector3D targetPos, Vector3D myPos)
            {
                return Math.Round(Vector3D.Distance(targetPos, myPos), 2);
            }

            public static double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }

            public static Vector3D GetXYZDistance(IMyTerminalBlock block, Vector3D targetPos)
            {
                Vector3D myPos = block.GetPosition();
                Vector3D targetVector = Vector3D.Subtract(targetPos, myPos);
                double targetDistance = targetVector.Length();
                targetVector.Normalize();

                PlaneD forwardReversePlane = new PlaneD(block.WorldMatrix.Forward, 0);
                PlaneD leftRightPlane = new PlaneD(block.WorldMatrix.Left, 0);
                PlaneD upDownPlane = new PlaneD(block.WorldMatrix.Up, 0);

                float forwardReverseDistance = Convert.ToSingle(Math.Sin(forwardReversePlane.DotNormal(targetVector)) * targetDistance);
                float leftRightDistance = Convert.ToSingle(Math.Sin(leftRightPlane.DotNormal(targetVector)) * targetDistance);
                float upDownDistance = Convert.ToSingle(Math.Sin(upDownPlane.DotNormal(targetVector)) * targetDistance);

                Vector3D xyzDistance = new Vector3D();
                xyzDistance.X = leftRightDistance;
                xyzDistance.Y = forwardReverseDistance;
                xyzDistance.Z = upDownDistance;

                return xyzDistance;
            }

            public static Vector3D VectorProject(Vector3D a, Vector3D b)
            {
                return a.Dot(b) / b.LengthSquared() * b;
            }
        }
    }
}
