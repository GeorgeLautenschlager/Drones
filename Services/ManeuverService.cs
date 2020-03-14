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
            public IMyRemoteControl Remote;
            private Drone Drone;
            public List<IMyGyro> Gyros;
            public List<IMyThrust> ThrustersAll;
            public Dictionary<Base6Directions.Direction, List<IMyThrust>> Thrusters;
            public Dictionary<Base6Directions.Direction, float> MaxThrusters; // TODO: rename this to MaxThrustInDirections

            private DateTime LastChecked;
            private Vector3D CurrentWaypoint;
            private Vector3D VelocityVector = new Vector3D();
            private Vector3D AccelerationVector = new Vector3D();
            private Vector3D Pos;
            private PID PitchPID, YawPID, RollPID;
            public PID XPID, YPID, ZPID;

            private double DistanceAccuracy;
            private const float MinAngleRad = 0.3f;
            private const double CTRL_COEFF = 0.2;
            private const int MinRemoteControlDistance = 200;
            private const double eps = 1E-4;

            #endregion Properties


            public ManeuverService(Program program, IMyRemoteControl remote, Drone drone, double DistanceAccuracy = 0.4)
            {
                this.Program = program;
                this.Remote = remote;
                this.Drone = drone;
                this.DistanceAccuracy = DistanceAccuracy;

                // Check if a Remote Control or Cockpit is found.
                if (this.Remote == null)
                    throw new Exception("No Remote Controller found.");

                // Get Gyros.
                this.Gyros = new List<IMyGyro>();
                gridRef().GetBlocksOfType(this.Gyros, gyro => gyro.IsSameConstructAs(program.Me));
                if (this.Gyros == null || this.Gyros.Count == 0)
                    throw new Exception("No Gyros found.");

                // Initialize the thrusters.
                InitThrusters();

                // Set rotation PID controllers
                PitchPID = new PID(5, 0, 3, 0.75);
                YawPID = new PID(5, 0, 3, 0.75);
                RollPID = new PID(5, 0, 3, 0.75);

                // Set translation PID controllers
                XPID = new PID(2, 1, 0, 0.75);
                YPID = new PID(2, 1, 0, 0.75);
                ZPID = new PID(2, 1, 0, 0.75);
            }

            public void SetThrust(Vector3D deltaV, bool align)
            {
                Base6Directions.Direction direction = Base6Directions.GetClosestDirection(deltaV);
                Base6Directions.Direction oppositeDirection = Base6Directions.GetOppositeDirection(direction);
                //Program.Echo($"\nDirection: {direction.ToString()}");
                //Program.Echo($"\nOpposite: {oppositeDirection.ToString()}");
                float maxThrust = MaxThrusters[direction];

                //double decayFactor = 0.8;
                //if (align && direction != Base6Directions.Direction.Forward || direction != Base6Directions.Direction.Backward)
                //    decayFactor = 0.25;

                //double acceleration = MathHelper.Clamp(maxThrust / Remote.CalculateShipMass().TotalMass, 0.1, Math.Pow(deltaV.Length(), decayFactor));
                //Drone.LogToLcd($"{direction.ToString()} {acceleration.ToString()}m/s/s");

                double acceleration = 0;
                var freq = this.Program.Runtime.TimeSinceLastRun.TotalMilliseconds / 1000;

                if (direction == Base6Directions.Direction.Left || direction == Base6Directions.Direction.Right)
                {
                    acceleration = XPID.CorrectError(deltaV.Length(), freq);
                }
                else if (direction == Base6Directions.Direction.Up || direction == Base6Directions.Direction.Down)
                {
                    acceleration = YPID.CorrectError(deltaV.Length(), freq);
                }
                else if (direction == Base6Directions.Direction.Forward || direction == Base6Directions.Direction.Backward)
                {
                    acceleration = ZPID.CorrectError(deltaV.Length(), freq);
                }

                double force = Remote.CalculateShipMass().TotalMass * acceleration;

                foreach (IMyThrust thruster in Thrusters[direction])
                {
                    Drone.LogToLcd($"{thruster.CustomName}\n");
                    thruster.Enabled = true;
                    thruster.ThrustOverride = (float)force;
                }

                foreach (IMyThrust thruster in Thrusters[oppositeDirection])
                {
                    Drone.LogToLcd($"Nulling {thruster.CustomName} {thruster.CurrentThrust}");
                    thruster.ThrustOverride = 0f;
                    thruster.Enabled = false;
                    Drone.LogToLcd($"Nulled {thruster.CustomName} {thruster.CurrentThrust}");
                }
            }

            private IMyGridTerminalSystem gridRef()
            {
                return this.Program.GridTerminalSystem;
            }

            #region Replace this
            private void InitThrusters()
            {
                // Get Thrusters.
                this.ThrustersAll = new List<IMyThrust>();
                gridRef().GetBlocksOfType<IMyThrust>(this.ThrustersAll, thruster => thruster.IsSameConstructAs(this.Program.Me));

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
                    // Set override to disabled.
                    t.ThrustOverride = 0f;
                    // Get direction of the thruster.
                    Base6Directions.Direction tDir = Remote.WorldMatrix.GetClosestDirection(t.WorldMatrix.Backward);
                    if (t.IsSameConstructAs(Program.Me))
                    {
                        Thrusters[tDir].Add(t);
                        MaxThrusters[tDir] += t.MaxThrust;
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
