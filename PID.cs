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
        public class PID
        {
            // u(t) = Kp*e(t) + Ki|e(t')dt' + Kd(de(t)/dt)

            double _Kp; // Proportional Gain
            double _Ki; // Integral Gain
            double _Kd; // Derivative Gain
            double _minOutput;
            double _maxOutput;
            double _lastError = 0;
            double _errorIntegral = 0;
            double _integralDecay = 0;
            bool _firstRun = true;
            bool _useIntegralDecay = false;

            public PID(double kP, double kI, double kD, double minOutput = 5, double maxOutput = 5)
            {
                _Kp = kP;
                _Ki = kI;
                _Kd = kD;
                _minOutput = minOutput;
                _maxOutput = maxOutput;
            }

            public PID(double kP, double kI, double kD, double integralDecay)
            {
                _Kp = kP;
                _Ki = kI;
                _Kd = kD;
                _integralDecay = integralDecay;
                _useIntegralDecay = true;
            }

            /// <summary>
            /// Calculates the output value of a PID loop, given an error and a time step.
            /// </summary>
            /// <param name="error">The difference between the Desired and Actual measurement.</param>
            /// <param name="timeStep">How long it's been since this method has been called (in seconds).</param>
            /// <returns>The value needed to correct the difference in measurement.</returns>
            public double CorrectError(double error, double timeStep)
            {
                // Derivative term
                double dInput = error - _lastError;
                double errorDerivative = _firstRun ? 0 : dInput / timeStep;

                // Integral term
                if (!_useIntegralDecay)
                {
                    _errorIntegral += error * timeStep;
                    _errorIntegral = MathHelperD.Clamp(_errorIntegral, _minOutput, _maxOutput);
                }
                else _errorIntegral = (_errorIntegral * _integralDecay) + (error * timeStep);

                // Compute the output
                double output = _Kp * error + _Ki * _errorIntegral + _Kd * errorDerivative;

                // Save the error for the next use
                _lastError = error;
                _firstRun = false;
                return output;
            }

            public void Reset()
            {
                _errorIntegral = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }
    }
}
