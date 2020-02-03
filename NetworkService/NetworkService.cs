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
            private IMyRemoteControl Remote;

            public NetworkService(Program program, IMyRemoteControl remote)
            {
                this.Program = program;
                this.Remote = remote;
            }

            public void BroadcastMessage(string channel, BroadcastMessage message)
            {
                Program.Echo($"Sending Message: {message.Action}");
                Program.IGC.SendBroadcastMessage(channel, message, TransmissionDistance.TransmissionDistanceMax);
            }

            public BroadcastMessage GetNextBroadcaseMesage(string channel)
            {
                //deserialize message and return it
                List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
                Program.IGC.GetBroadcastListeners(listeners);

                if (listeners[0].HasPendingMessage)
                {
                    MyIGCMessage message = new MyIGCMessage();
                    message = listeners[0].AcceptMessage();

                    BroadcastMessage deserializedMessage = MessageFactory.Get<BroadcastMessage>(message.Data.ToString());
                    Program.Echo($"Message received: {deserializedMessage.Action}");
                }

                return new BroadcastMessage();
            }

            public void RegisterBroadcastListener(string channel)
            {
                Program.IGC.RegisterBroadcastListener(channel);
            }
        }
    }
}
