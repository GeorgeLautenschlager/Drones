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

            public NetworkTester(MyIni config)
            {
                ParseConfig(config);
            }

            public override void InitWithDrone(Drone drone)
            {
                this.Drone = drone;
                this.State = 0;
                this.Drone.Wake();
            }

            public void perform()
            {
                if(!UseCallbacks)
                {
                    if(Drone.NetworkService.GetUnicastListener().HasPendingMessage)
                    {
                        MyIGCMessage message = Drone.NetworkService.GetUnicastListener.AcceptMessage();
                        ParseUnicast(message);
                    }
                }

                if(Sender)
                {
                    if(message_type == "Vector3D")
                    {
                        MyTuple<bool, Vector3D> payload = new MyTuple<bool, Vector3D>();
                        payload.Item1 = true;
                        payload.Item2 = Drone.Remote.GetPosition();

                        Drone.NetworkService.UnicastMessage("tester1", "vector", payload);
                        payload.Item1 = false;
                        Drone.NetworkService.UnicastMessage("tester2", "vector", payload);
                    }
                    else if(message_type == "EntityId")
                    {
                        MyTuple<bool, long> payload = new MyTuple<bool, long>();
                        payload.Item1 = true;
                        payload.Item2 = Drone.Program.Me.EntityId;

                        Drone.NetworkService.UnicastMessage("tester1", "entity_id", payload);
                        payload.Item1 = false;
                        Drone.NetworkService.UnicastMessage("tester2", "entity_id", payload);
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
                string recipients;
                if (config.Get(Name(), "unicast_recipients").TryGetString(recipients))
                {
                    foreach(string recipient in recipients.Split(","))
                    {
                        recipient_name = recipient.Split(":")[0];
                        recipient_address = Int64.TryParse(recipient.Split[1]);
                        Drone.NetworkService.RegisterUnicastRecipient(recipient_name, recipient_address);
                    }
                }
                else
                {
                    throw new Exception("unicast_recipients is missing");
                }

                if(!config.TryGetBool(Name(), "use_callbacks").TryGetBoolean(out UseCallbacks))
                    throw new Exception("use_callbacks is missing");

                if(!config.TryGetBool(Name(), "sender").TryGetBoolean(out Sender))
                    throw new Exception("sender is missing");
                
                if(!config.TryGetBool(Name(), "message_type").TryGetBoolean(out MessageType))
                    throw new Exception("message type is missing");
            }
        
            public override void HandleCallback(string callback)
            {
                switch (callback)
                {
                    // named callbacks are from broadcast messages
                    // "unicast" denotes that there is a unicast message
                    // the tag of the unicast message tells the drone how to interpret it
                    case "unicast":
                        MyIGCMessage message = Drone.NetworkService.GetUnicastListener.AcceptMessage();
                        ParseUnicast(message);
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
                switch (unicast.Tag)
                {
                    case "vector":
                        MyTuple<bool, Vector3D> dataTuple = message.Data as MyTuple<Boolean, Vector3D>;

                        if(dataTuple.Item1)
                        {
                            Drone.LogToLcd($"Received Vector: {dataTuple.Item2.ToString()}");
                        }
                        else
                        {
                            Drone.Log($"Received Vector: {dataTuple.Item2.ToString()}");
                        }

                        MyTuple<string, Vector3D> echo = new MyTuple<string, Vector3D>("Position received, reciprocating", Drone.Remote.GetPosition());
                        Program.IGC.SendUnicastMessage(unicast.source, "echo", echo);

                        break;
                    case "entity_id":
                        MyTuple<bool, Long> dataTuple = message.Data as MyTuple<Boolean, long>;

                        if(dataTuple.Item1)
                        {
                            Drone.LogToLcd($"Received Entity ID: {dataTuple.Item2}");
                        }
                        else
                        {
                            Drone.Log($"Received Entity ID: {dataTuple.Item2}");
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
        }
    }
}
