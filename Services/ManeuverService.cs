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
        public class ManeuverService
        {
            #region Properties

            private Program Program;
            private IMyRemoteControl Remote;
            private List<IMyGyro> Gyros;
            private List<IMyThrust> ThrustersAll;
            private Dictionary<Base6Directions.Direction, List<IMyThrust>> Thrusters;
            private Dictionary<Base6Directions.Direction, float> MaxThrusters;

            private DateTime LastChecked;
            private Vector3D CurrentWaypoint;
            private Vector3D VelocityVector = new Vector3D();
            private Vector3D AccelerationVector = new Vector3D();
            private Vector3D Pos;
            private PID PitchPID, YawPID, RollPID;

            private double DistanceAccuracy;
            private const float MinAngleRad = 0.3f;
            private const double CTRL_COEFF = 0.2;
            private const int MinRemoteControlDistance = 200;
            private const double eps = 1E-4;

            #endregion Properties


            public ManeuverService(Program program, IMyRemoteControl remote, double DistanceAccuracy = 0.4)
            {
                this.Program = program;
                this.Remote = remote;
                this.DistanceAccuracy = DistanceAccuracy;

                // Check if a Remote Control or Cockpit is found.
                if (this.Remote == null)
                    throw new Exception("No Remote Controller found.");

                // Get Gyros.
                this.Gyros = new List<IMyGyro>();
                gridRef().GetBlocksOfType(this.Gyros, gyro => true);
                if (this.Gyros == null || this.Gyros.Count == 0)
                    throw new Exception("No Gyros found.");

                // Initialize the thrusters.
                InitThrusters();

                // Set PID's, used for for alignment.
                PitchPID = new PID(5, 0, 3, 0.75);
                YawPID = new PID(5, 0, 3, 0.75);
                RollPID = new PID(5, 0, 3, 0.75);
            }

            private IMyGridTerminalSystem gridRef()
            {
                return this.Program.GridTerminalSystem;
            }

            // TODO: add an optional roll alignmentVector
            public bool AlignTo(Vector3D alignmentVector, IMyTerminalBlock block)
            {
                // TODO: make these instance variables
                var orientationAccuracy = 0.01;
                var orientationCorrectionSpeedFactor = 0.1;

                // Check angle.
                if (VectorHelper.AngleBetween(alignmentVector, block.WorldMatrix.Forward) <= orientationAccuracy)
                {
                    DisableGyroOverride();
                    return true;
                }

                double yawCorrectionAngle, pitchCorrectionAngle;
                GetRotationAngles(
                  alignmentVector,
                  block.WorldMatrix.Forward,
                  block.WorldMatrix.Left,
                  block.WorldMatrix.Up,
                  out yawCorrectionAngle,
                  out pitchCorrectionAngle
                );

                var correctionFrequency = this.Program.Runtime.TimeSinceLastRun.TotalMilliseconds / 1000;

                // Determine pitch and yaw speeds
                double pitchSpeed, yawSpeed;
                pitchSpeed = PitchPID.CorrectError(pitchCorrectionAngle, correctionFrequency) * orientationCorrectionSpeedFactor;
                yawSpeed = YawPID.CorrectError(yawCorrectionAngle, correctionFrequency) * orientationCorrectionSpeedFactor;

                SetGyroOverrides(pitchSpeed, yawSpeed, 0, Gyros, block.WorldMatrix);

                return false;
            }

            private void SetGyroOverrides(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyros, MatrixD worldMatrix)
            {
                var bodyAlignmentVector = Vector3D.TransformNormal(new Vector3D(-pitchSpeed, yawSpeed, rollSpeed), worldMatrix);

                foreach (IMyGyro gyro in gyros)
                {
                    Vector3 transformedRotationVec = Vector3D.TransformNormal(bodyAlignmentVector, Matrix.Transpose(gyro.WorldMatrix));

                    var pitchOverride = transformedRotationVec.X;
                    var yawOverride = transformedRotationVec.Y;
                    var rollOverrid = transformedRotationVec.Z;

                    gyro.Pitch = pitchOverride;
                    gyro.Yaw = yawOverride;

                    gyro.GyroOverride = true;
                }
            }

            public bool FlyToPosition(Vector3D position, IMyTerminalBlock block)
            {
                if (DistanceTo(position) <= DistanceAccuracy)
                {
                    return true;
                }

                // Get the velocity in XYZ directions.
                CalculateVelocity();
                Vector3D velocityNorm = Vector3D.Normalize(VelocityVector);

                // Distance of block in XYZ directions to target position.
                var xyzDist = VectorHelper.GetXYZDistance(block, position);
                var distX = Math.Abs(xyzDist.X);
                var distY = Math.Abs(xyzDist.Y);
                var distZ = Math.Abs(xyzDist.Z);

                // Ship mass
                var mass = Remote.CalculateShipMass().TotalMass;

                // Up
                if (xyzDist.Z > DistanceAccuracy)
                    SetThrustDirect(Base6Directions.Direction.Up, VelocityVector.Length() * 1000, mass, distZ, velocityNorm);

                // Down
                else if (xyzDist.Z < DistanceAccuracy * -1)
                    SetThrustDirect(Base6Directions.Direction.Down, VelocityVector.Length() * 1000, mass, distZ, velocityNorm);

                // Left
                if (xyzDist.X > DistanceAccuracy)
                    SetThrustDirect(Base6Directions.Direction.Left, VelocityVector.Length() * 1000, mass, distX, velocityNorm);

                // Right
                else if (xyzDist.X < DistanceAccuracy * -1)
                    SetThrustDirect(Base6Directions.Direction.Right, VelocityVector.Length() * 1000, mass, distX, velocityNorm);

                // Forward
                if (xyzDist.Y > DistanceAccuracy)
                    SetThrustDirect(Base6Directions.Direction.Forward, VelocityVector.Length() * 1000, mass, distY, velocityNorm);

                // Backward
                else if (xyzDist.Y < DistanceAccuracy * -1)
                    SetThrustDirect(Base6Directions.Direction.Backward, VelocityVector.Length() * 1000, mass, distY, velocityNorm);

                return false;
            }

            #region Replace this
            private void InitThrusters()
            {
                // Get Thrusters.
                this.ThrustersAll = new List<IMyThrust>();
                gridRef().GetBlocksOfType<IMyThrust>(this.ThrustersAll);

                if (this.ThrustersAll == null || this.ThrustersAll.Count == 0)
                {
                    Program.Echo("WARNING: No Thrusters");
                    return;
                }

                // Init Dictionaries.
                this.Thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
                this.MaxThrusters = new Dictionary<Base6Directions.Direction, float>();

                // Set Dictionaries.
                Thrusters[Base6Directions.Direction.Forward] = new List<IMyThrust>();
                Thrusters[Base6Directions.Direction.Backward] = new List<IMyThrust>();
                Thrusters[Base6Directions.Direction.Up] = new List<IMyThrust>();
                Thrusters[Base6Directions.Direction.Down] = new List<IMyThrust>();
                Thrusters[Base6Directions.Direction.Left] = new List<IMyThrust>();
                Thrusters[Base6Directions.Direction.Right] = new List<IMyThrust>();

                MaxThrusters[Base6Directions.Direction.Forward] = 0;
                MaxThrusters[Base6Directions.Direction.Backward] = 0;
                MaxThrusters[Base6Directions.Direction.Up] = 0;
                MaxThrusters[Base6Directions.Direction.Down] = 0;
                MaxThrusters[Base6Directions.Direction.Left] = 0;
                MaxThrusters[Base6Directions.Direction.Right] = 0;

                // Set thrusters to each direction list according to its relative direction.
                foreach (var t in ThrustersAll)
                {
                    t.Enabled = true;

                    // If not wokring, then skip this thruster.
                    if (!t.IsWorking)
                        continue;

                    // Set override to disabled.
                    t.ThrustOverride = 0f;
                    // Get direction of the thruster.
                    Base6Directions.Direction tDir = Remote.WorldMatrix.GetClosestDirection(t.WorldMatrix.Backward);
                    Thrusters[tDir].Add(t);
                    MaxThrusters[tDir] += t.MaxThrust;
                }
            }

            public Vector3D GetPosition()
            {
                return Remote.GetPosition();
            }
            public Dictionary<string, Vector3D> GetOrientation()
            {
                Matrix m = Remote.WorldMatrix;

                Vector3D forward = m.Forward;
                Vector3D backward = m.Backward;
                Vector3D up = m.Up;
                Vector3D down = m.Down;
                Vector3D right = m.Right;
                Vector3D left = m.Left;

                return new Dictionary<string, Vector3D>()
                {
                    {"pos", this.GetPosition() },
                    {"forward", forward },
                    {"up", up },
                    {"right", right },
                    {"backward", backward },
                    {"down", down },
                    {"left", left }
                };
            }

            public double DistanceTo(Vector3D vPos, IMyTerminalBlock block = null)
            {
                if (block == null)
                    block = Remote;
                return VectorHelper.DistanceBetween(vPos, block);
            }

            public bool GoToPosition(Vector3D vPos, IMyTerminalBlock block = null, bool useAutoPilot = true, bool alignTo = true)
            {
                if (block == null)
                    block = Remote;

                if (block != Remote)
                {
                    // Apply offset.
                    Vector3D offset = (Remote.GetPosition() - block.GetPosition());
                    vPos = (vPos + offset);
                }

                double distance = DistanceTo(vPos, Remote);
                this.Program.Echo(distance.ToString());

                if (distance <= DistanceAccuracy && VelocityVector.Length() <= 0.05)
                {
                    Reset();
                    return true;
                }

                // Use Remote Control when the minimal distance has been reached.
                if (distance >= MinRemoteControlDistance && useAutoPilot)
                {
                    if (!Remote.IsAutoPilotEnabled)
                    {
                        // Set waypoint.
                        Remote.AddWaypoint(vPos, "GoToPosition");
                        CurrentWaypoint = vPos;

                        // Use RC for flying.
                        EnableAutoPilot(true);
                    }
                }
                else
                {
                    // Disable the auto pilot if enabled.
                    EnableAutoPilot(false);

                    // Align if required.
                    if (alignTo && !AlignBlockTo(vPos, block))
                    {
                        return false;
                    }

                    if (GoToWithThrusters(vPos, block))
                    {
                        Reset();
                        return true;
                    }
                }

                return false;
            }

            public void EnableAutoPilot(bool enable, bool precise = true)
            {
                if (Remote != null)
                {
                    if (enable && !Remote.IsAutoPilotEnabled)
                    {
                        DisableThrustersOverride();
                        DisableGyroOverride();

                        Remote.SetAutoPilotEnabled(true);
                        Remote.SetCollisionAvoidance(true);
                        Remote.SetDockingMode(precise);
                    }
                    else if (!enable && Remote.IsAutoPilotEnabled)
                    {
                        Remote.ClearWaypoints();
                        Remote.SetAutoPilotEnabled(false);
                        Remote.SetCollisionAvoidance(false);
                        Remote.SetDockingMode(false);
                    }
                }
            }
            public void DisableGyroOverride()
            {
                foreach (var gyro in Gyros)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = false;
                }
            }
            public void DisableThrustersOverride()
            {
                foreach (var thrust in ThrustersAll)
                    thrust.ThrustOverride = 0;
            }
            public void Reset()
            {
                CurrentWaypoint = new Vector3D(-1, -1, -1);
                EnableAutoPilot(false);
                DisableThrustersOverride();
                DisableGyroOverride();
            }

            public bool AlignBlockTo(Vector3D vPos, IMyTerminalBlock block = null)
            {
                if (block == null)
                    block = Remote;

                Matrix m = block.WorldMatrix;
                Vector3D forward = m.Forward;
                Vector3D left = m.Left;
                Vector3D up = m.Up;

                Vector3D vTarget = (vPos - block.GetPosition());
                vTarget.Normalize();

                // Check angle.
                var angleF = VectorHelper.AngleBetween(vTarget, m.Forward);
                this.Program.Echo(string.Format("Angle: {0}", angleF));
                if (angleF <= 0.01)
                {
                    DisableGyroOverride();
                    return true;
                }

                double yaw, pitch;
                GetRotationAngles(vTarget, forward, left, up, out yaw, out pitch);

                // Correct with PID.
                var freq = this.Program.Runtime.TimeSinceLastRun.TotalMilliseconds / 1000;
                pitch = PitchPID.CorrectError(pitch, freq) * 0.1;     // * speedFactor; apply factor to reduce the speed if needed.
                yaw = YawPID.CorrectError(yaw, freq) * 0.1;           // * speedFactor; apply factor to reduce the speed if needed.

                // Apply gyro overrides.
                ApplyGyroOverride(pitch, yaw, 0, Gyros, m);

                // Return not aligned for now. Will keep aligning :)
                return false;
            }

            public bool AlignBlockAs(Vector3D tForward, Vector3D tDown, IMyTerminalBlock block = null, bool reverse = false)
            {
                if (Remote.IsAutoPilotEnabled)
                    return false;

                if (block == null)
                    block = Remote;

                var m = block.WorldMatrix;
                var angleHV = VectorHelper.AngleBetween(tForward, m.Forward);
                var angleRoll = VectorHelper.AngleBetween(tDown, m.Down);

                // If aligned enough, then stop the rotations.
                if (angleHV <= 0.0004 && angleRoll <= 0.0004)
                {
                    this.Program.Echo(angleHV.ToString());
                    this.Program.Echo(angleRoll.ToString());
                    DisableGyroOverride();
                    return true;
                }

                // Get speed factor to apply for the rotation.
                var xyFactor = 0.1;
                var zFactor = 0.05;

                double yaw, pitch, roll;
                var alignTrans = Vector3D.TransformNormal(tDown, MatrixD.Transpose(m));
                CalculateGyroOverrides(tForward, m, tDown, alignTrans, out yaw, out pitch, out roll);

                var freq = this.Program.Runtime.TimeSinceLastRun.TotalMilliseconds / 1000;
                pitch = PitchPID.CorrectError(pitch, freq) * xyFactor;
                yaw = YawPID.CorrectError(yaw, freq) * xyFactor;
                roll = RollPID.CorrectError(roll, freq) * zFactor;

                ApplyGyroOverride(pitch, yaw, roll, Gyros, m);

                return false;
            }

            private void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyroList, MatrixD shipMatrix)
            {
                var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed);
                var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix);

                foreach (IMyGyro g in gyroList)
                {
                    var gyroMatrix = g.WorldMatrix;
                    Vector3 transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix));

                    var pitch = transformedRotationVec.X;
                    var yaw = transformedRotationVec.Y;
                    var roll = transformedRotationVec.Z;

                    g.Pitch = pitch;
                    g.Yaw = yaw;
                    g.Roll = roll;

                    g.GyroOverride = true;
                }
            }

            private void CalculateGyroOverrides(Vector3D targetVec, MatrixD shipMatrix, Vector3D alignVec, Vector3D alignVecTransformed, out double yaw, out double pitch, out double roll)
            {
                var upVec = shipMatrix.Up;
                var fwdVec = shipMatrix.Forward;
                var lftVec = shipMatrix.Left;
                var dwnVec = shipMatrix.Down;

                var projUp = VectorHelper.VectorProject(targetVec, upVec);
                var rejection = targetVec - projUp;

                yaw = VectorHelper.AngleBetween(fwdVec, rejection);
                pitch = VectorHelper.AngleBetween(targetVec, rejection);
                roll = 0;

                // Left or Right
                // Multiplied by -1 to convert from RH to LH
                yaw = -1 * Math.Sign(lftVec.Dot(targetVec)) * yaw;

                // Up or Down
                pitch = Math.Sign(upVec.Dot(targetVec)) * pitch;

                // In Front of or Behind
                if (Math.Abs(pitch) < eps && Math.Abs(yaw) < eps && targetVec.Dot(fwdVec) < 0)
                    yaw = Math.PI;

                if (!Vector3D.IsZero(alignVec) && !Vector3D.IsZero(alignVecTransformed))
                {
                    roll = -10 * alignVecTransformed.X / alignVecTransformed.LengthSquared();

                    // Inverted or not
                    if (Math.Abs(roll) < eps && alignVec.Dot(dwnVec) < 0)
                        roll = Math.PI;
                }
            }

            private double CalculateStopDistance(double velocity, double acceleration)
            {
                Program.Echo($"velocity * velocity / (2 * acceleration) = {velocity} * {velocity} / (2 * {acceleration})");
                return Math.Abs(velocity * velocity / (2 * acceleration));
            }

            private void SetThrustDirect(Base6Directions.Direction headingDir, double velocity, float mass, double dist, Vector3D velocityNorm)
            {
                // Get oposite direction.
                Base6Directions.Direction opositeDir = Base6Directions.GetOppositeDirection(headingDir);

                // Use the oposite thrusters to calculate the acc needed for breaking...
                double acc = CalculateAcceleration(Thrusters[opositeDir], mass, velocityNorm);
                double stopDist = 2; //CalculateStopDistance(velocity, acc);
                Program.Echo($"stopping distance: {stopDist}");
                if (double.IsNaN(stopDist))
                    stopDist = 0;

                // Calculate the power percentage to use based on distance.
                double powerPercent = MathHelper.Clamp(dist / 200, 0.05, 1) * 100;

                // Do we need to break?

                string fdist = string.Format("{0:N2}", dist);
                string fstopDist = string.Format("{0:N2}", stopDist * 10);
                Program.Echo($"stopping distance: {fstopDist}");

                if (stopDist * 10.0d >= dist)
                {
                    Program.Echo($"Coasting");
                    SetThrust(headingDir, 0);
                }
                else
                {
                    Program.Echo($"I wanna go fast: {powerPercent}");
                    SetThrust(headingDir, powerPercent);
                }
            }

            private void SetThrust(Base6Directions.Direction dir, double percent)
            {
                // Set the thrusters of teh required direction.
                foreach (var thruster in Thrusters[dir])
                    thruster.ThrustOverridePercentage = ((float)percent / 100);

                // Disable override for the oposite thrusters.
                foreach (var thruster in Thrusters[Base6Directions.GetOppositeDirection(dir)])
                    thruster.ThrustOverride = 0;
            }

            private double CalculateAcceleration(List<IMyThrust> thrusters, float shipMass, Vector3D velocityNorm)
            {
                float maxThrust = 0;
                foreach (var t in thrusters)
                {
                    if (!t.IsWorking)
                        continue;

                    Vector3D proj = VectorHelper.VectorProject(t.WorldMatrix.Forward, velocityNorm);
                    double percentage = proj.Length();
                    maxThrust += (float)(t.MaxThrust * percentage);
                }

                double myAcceleration = (maxThrust / shipMass);
                if (double.IsNaN(myAcceleration))
                {
                    myAcceleration = 0;
                }
                return myAcceleration;
            }

            public bool GoToWithThrusters(Vector3D vPos, IMyTerminalBlock block = null)
            {
                // Set block to RC if nothing else is specfied.
                if (block == null)
                    block = Remote;

                Program.Echo($"DistanceTo(vPos) <= DistanceAccuracy = {DistanceTo(vPos)} <= {DistanceAccuracy}");
                if (DistanceTo(vPos) <= DistanceAccuracy)
                {
                    Program.Echo("Arrived at Destination.");
                    return true;
                }

                // Get the velocity in XYZ directions.
                CalculateVelocity();
                Vector3D velocityNorm = Vector3D.Normalize(VelocityVector);

                double velT = VelocityVector.Length() * 1000;
                double velX = VelocityVector.X * 1000;
                double velY = VelocityVector.Y * 1000;
                double velZ = VelocityVector.Z * 1000;

                // Distance of block in XYZ directions to target position.
                var xyzDist = VectorHelper.GetXYZDistance(block, vPos);
                var distX = Math.Abs(xyzDist.X);
                var distY = Math.Abs(xyzDist.Y);
                var distZ = Math.Abs(xyzDist.Z);

                // Ship mass
                var mass = Remote.CalculateShipMass().TotalMass;

                // Up
                if (xyzDist.Z > DistanceAccuracy)
                    SetThrustDirect(Base6Directions.Direction.Up, velT, mass, distZ, velocityNorm);

                // Down
                else if (xyzDist.Z < DistanceAccuracy * -1)
                    SetThrustDirect(Base6Directions.Direction.Down, velT, mass, distZ, velocityNorm);

                // Left
                if (xyzDist.X > DistanceAccuracy)
                    SetThrustDirect(Base6Directions.Direction.Left, velT, mass, distX, velocityNorm);

                // Right
                else if (xyzDist.X < DistanceAccuracy * -1)
                    SetThrustDirect(Base6Directions.Direction.Right, velT, mass, distX, velocityNorm);

                // Forward
                if (xyzDist.Y > DistanceAccuracy)
                    SetThrustDirect(Base6Directions.Direction.Forward, velT, mass, distY, velocityNorm);

                // Backward
                else if (xyzDist.Y < DistanceAccuracy * -1)
                    SetThrustDirect(Base6Directions.Direction.Backward, velT, mass, distY, velocityNorm);

                return false;
            }

            private void CalculateVelocity()
            {
                var stamp = DateTime.Now;
                var currentPos = Remote.GetPosition();
                var lastVelocity = VelocityVector;

                if (LastChecked != DateTime.MinValue && (stamp - LastChecked).TotalMilliseconds > 100)
                {
                    var elapsedTime = (stamp - LastChecked).TotalMilliseconds;
                    VelocityVector.X = (float)(currentPos.X - Pos.X) / (float)elapsedTime;
                    VelocityVector.Y = (float)(currentPos.Y - Pos.Y) / (float)elapsedTime;
                    VelocityVector.Z = (float)(currentPos.Z - Pos.Z) / (float)elapsedTime;

                    AccelerationVector.X = (VelocityVector.X - lastVelocity.X) / elapsedTime;
                    AccelerationVector.Y = (VelocityVector.Y - lastVelocity.Y) / elapsedTime;
                    AccelerationVector.Z = (VelocityVector.Z - lastVelocity.Z) / elapsedTime;
                }
                LastChecked = stamp;
                Pos = currentPos;
            }

            private void GetRotationAngles(Vector3D v_target, Vector3D v_front, Vector3D v_left, Vector3D v_up, out double yaw, out double pitch)
            {
                //Dependencies: ProjectVector() | GetAngleBetween()
                var projectTargetUp = VectorHelper.VectorProject(v_target, v_up);
                var projTargetFrontLeft = v_target - projectTargetUp;

                yaw = VectorHelper.AngleBetween(v_front, projTargetFrontLeft);
                pitch = VectorHelper.AngleBetween(v_target, projTargetFrontLeft);

                //---Check if yaw angle is left or right
                //multiplied by -1 to convert from right hand rule to left hand rule
                yaw = -1 * Math.Sign(v_left.Dot(v_target)) * yaw;

                //---Check if pitch angle is up or down
                pitch = Math.Sign(v_up.Dot(v_target)) * pitch;

                //---Check if target vector is pointing opposite the front vector
                if (Math.Abs(pitch) < eps && Math.Abs(yaw) < eps && v_target.Dot(v_front) < 0)
                    yaw = Math.PI;
            }

            #endregion
        }
    }
}
