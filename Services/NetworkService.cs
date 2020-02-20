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
        public class NetworkService
        {
            private Program Program;
            public long DroneControllerEntityId;

            public NetworkService(Program program)
            {
                this.Program = program;
            }

            public void BroadcastMessage(string channel, string message)
            {
                Program.Echo($"Sending Message: {message} to channel {channel} at {DateTime.Now.ToString()}");
                Program.IGC.SendBroadcastMessage(channel, message, TransmissionDistance.TransmissionDistanceMax);
            }

            public void RegisterBroadcastListener(string channel)
            {
                Program.IGC.RegisterBroadcastListener(channel);
            }

            public void RegisterBroadcastCallback(string channel, string callback)
            {
                this.GetBroadcastListenerForChannel(channel).SetMessageCallback(callback);
            }

            public IMyBroadcastListener GetBroadcastListenerForChannel(string channel)
            {
                List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
                Program.IGC.GetBroadcastListeners(listeners, listener => listener.Tag == channel);

                if (listeners.Count != 1)
                {
                    throw new Exception($"There must be exactly one listener on channel {channel}.");
                }
                else
                {
                    return listeners.First();
                }
            }

            public void UnicastMessage(string recipient, string channel, Object message)
            {
                long address = AddressLookup(recipient);
                if (address == null)
                    throw new Exception($"Address not found for {recipient}");
                Program.IGC.SendUnicastMessage(address, channel, message);
            }

            public IMyUnicastListener GetUnicastListener()
            {
                return Program.IGC.UnicastListener;
            }

            public void RegisterUnicastCallback(string callback)
            {
                GetUnicastListener().SetMessageCallback(callback);
            }

            private long AddressLookup(string recipient)
            {
                long address = -1;

                if (recipient == "Drone Controller")
                {
                    address = DroneControllerEntityId;
                }

                return address;
            }
        }
    }
}
