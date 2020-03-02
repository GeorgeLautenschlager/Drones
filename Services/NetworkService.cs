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
            private Drone Drone;
            public Dictionary<string, long> UnicastRecipients;

            public NetworkService(Program program, Drone drone)
            {
                Program = program;
                this.Drone = drone;
                this.UnicastRecipients = new Dictionary<string, long>();
                GetUnicastListener().SetMessageCallback("unicast");
            }

            #region Broadcast
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
            #endregion

            #region Unicast
            public void UnicastMessage(string recipient, string channel, Object message)
            {
                long address = UnicastRecipients[recipient];
                this.Drone.LogToLcd($"Sending message to: {recipient}({address}), on channel: {channel}");
                if (address == null)
                    throw new Exception($"Address not found for {recipient}");
                Program.IGC.SendUnicastMessage(address, channel, message);
            }

            public IMyUnicastListener GetUnicastListener()
            {
                return Program.IGC.UnicastListener;
            }

            public void RegisterUnicastRecipient(string name, long entityId)
            {
                UnicastRecipients.Add(name, entityId);
            }
            #endregion
        }
    }
}
