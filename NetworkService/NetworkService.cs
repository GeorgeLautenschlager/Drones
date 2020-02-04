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
        public class NetworkService
        {
            private Program Program;
            private IMyRemoteControl Remote;

            public NetworkService(Program program, IMyRemoteControl remote)
            {
                this.Program = program;
                this.Remote = remote;
            }

            public void BroadcastMessage(string channel, string message)
            {
                Program.Echo($"Sending Message: {message}");
                Program.IGC.SendBroadcastMessage(channel, message, TransmissionDistance.TransmissionDistanceMax);
            }

            public void RegisterBroadcastListener(string channel)
            {
                Program.IGC.RegisterBroadcastListener(channel);
            }

            public void RegisterCallBack(string channel)
            {
               this.GetBroadcastListenerForChannel(channel).SetMessageCallback(channel);
            }

            public IMyBroadcastListener GetBroadcastListenerForChannel(string channel)
            {
                List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
                Program.IGC.GetBroadcastListeners(listeners, listener => listener.Tag == channel);

                if (listeners.Count != 1)
                {
                    throw new Exception($"There must be exactly one listener on channel {channel} to register a callback!");
                }
                else
                {
                    return listeners.First();
                }
            }
        }
    }
}
