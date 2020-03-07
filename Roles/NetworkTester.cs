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
        public class NetworkTester : Role
        {
            private bool UseCallbacks;
            private bool Sender;
            private string MessageType;
            String Recipients;

            String channel = "handshake";

            public NetworkTester(MyIni config)
            {
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                this.Drone = drone;

                foreach (string recipient in Recipients.Split(','))
                {
                    string RecipientName = recipient.Split(':')[0];
                    long RecipientAddress = Int64.Parse(recipient.Split(':')[1]);
                    Drone.RegisterUnicastRecipient(RecipientName, RecipientAddress);
                    Drone.ListenToChannel(channel);
                    Drone.NetworkService.RegisterBroadcastCallback(channel, channel);
                }

                this.State = 0;
                this.Drone.Wake();

                Drone.LogToLcd("Me=" + Drone.Program.Me.EntityId.ToString());

            }

            public override void Perform()
            {
                if (!UseCallbacks)
                {
                    if(Drone.NetworkService.GetUnicastListener().HasPendingMessage)
                    {
                        MyIGCMessage message = Drone.NetworkService.GetUnicastListener().AcceptMessage();
                        ParseUnicast(message);
                    }
                }

                Drone.Log("Checking for messages");
                if (Drone.NetworkService.GetBroadcastListenerForChannel(channel).HasPendingMessage)
                {
                    MyIGCMessage message = Drone.NetworkService.GetBroadcastListenerForChannel(channel).AcceptMessage();
                    Drone.Log(message.Data.ToString());
                    Drone.Log(message.Source.ToString());
                }

                if (Sender)
                {
                    if(MessageType == "Vector3D")
                    {
                        MyTuple<bool, Vector3D> payload = new MyTuple<bool, Vector3D>();
                        payload.Item1 = true;
                        payload.Item2 = Drone.Remote.GetPosition();

                        Drone.Log($"Sending Position to miner1: {Drone.NetworkService.UnicastRecipients["miner1"]}");
                        long address = Drone.NetworkService.UnicastRecipients["miner1"];
                        Drone.Program.IGC.SendUnicastMessage<MyTuple<bool, Vector3D>>(address, "vector", payload);
                        //Drone.NetworkService.UnicastMessage("tester1", "vector", payload);
                    }
                    else if(MessageType == "EntityId")
                    {
                        MyTuple<bool, long> payload = new MyTuple<bool, long>();
                        payload.Item1 = true;
                        payload.Item2 = Drone.Program.Me.EntityId;

                        Drone.NetworkService.UnicastMessage("tester1", "entity_id", payload);
                        payload.Item1 = false;
                        Drone.NetworkService.UnicastMessage("tester2", "entity_id", payload);
                    }
                    else if(MessageType == channel)
                    {
                        Drone.NetworkService.BroadcastMessage(channel, $"My address is: {Drone.Program.Me.EntityId}");
                    }
                    else
                    {
                        throw new Exception("Message type not recognized");
                    }
                }
                
                //Else do nothing, wait for callbacks
            }

            public void ParseConfig(MyIni config)
            {
                if (!config.Get(Name(), "unicast_recipients").TryGetString(out Recipients))
                    throw new Exception("unicast_recipients is missing");

                if (!config.Get(Name(), "use_callbacks").TryGetBoolean(out UseCallbacks))
                    throw new Exception("use_callbacks is missing");

                if(!config.Get(Name(), "sender").TryGetBoolean(out Sender))
                    throw new Exception("sender is missing");
                
                if(!config.Get(Name(), "message_type").TryGetString(out MessageType))
                    throw new Exception("message type is missing");
            }
        
            public override void HandleCallback(string callback)
            {
                Drone.LogToLcd($"running callback: {callback}");
                switch (callback)
                {
                    // named callbacks are from broadcast messages
                    // "unicast" denotes that there is a unicast message
                    // the tag of the unicast message tells the drone how to interpret it
                    case "unicast":
                        if (Drone.NetworkService.GetUnicastListener().HasPendingMessage)
                        {
                            Drone.LogToLcd($"No Message!");
                        }
                        else
                        {
                            Drone.LogToLcd($"Message Received.");
                        }

                        MyIGCMessage message = Drone.NetworkService.GetUnicastListener().AcceptMessage();
                        ParseUnicast(message);
                        break;
                    case "handshake":
                        message = Drone.NetworkService.GetBroadcastListenerForChannel("handshake").AcceptMessage();
                        Drone.LogToLcd($"Message source: {message.Source}");
                        break;
                    case "":
                        // Just Ignore empty arguments
                        break;
                    default:
                        Drone.LogToLcd($"\nDrone received unrecognized callback: {callback}");
                        break;
                }
            }

            public void ParseUnicast(MyIGCMessage unicast)
            {
                Drone.LogToLcd($"Parsing message with tag: {unicast.Tag}");
                switch (unicast.Tag)
                {
                    case "vector":
                        MyTuple<bool, Vector3D> vectorTuple = (MyTuple < Boolean, Vector3D>)unicast.Data;

                        if(vectorTuple.Item1)
                        {
                            Drone.LogToLcd($"Received Vector: {vectorTuple.Item2.ToString()}");
                        }
                        else
                        {
                            Drone.Log($"Received Vector: {vectorTuple.Item2.ToString()}");
                        }

                        MyTuple<string, Vector3D> echo = new MyTuple<string, Vector3D>("Position received, reciprocating", Drone.Remote.GetPosition());
                        Drone.Program.IGC.SendUnicastMessage(unicast.Source, "echo", echo);

                        break;
                    case "entity_id":
                        MyTuple<bool, long> idTuple = (MyTuple<Boolean, long>)unicast.Data;

                        if(idTuple.Item1)
                        {
                            Drone.LogToLcd($"Received Entity ID: {idTuple.Item2}");
                        }
                        else
                        {
                            Drone.Log($"Received Entity ID: {idTuple.Item2}");
                        }
                        
                        break;
                    case "echo":

                        break;
                    default:
                        Drone.LogToLcd("Tag not recognized!");
                        Drone.Log("Tag not recognized!");
                        break;
                }
            }

            public override string Name()
            {
                return "network_tester";
            }
        }
    }
}
