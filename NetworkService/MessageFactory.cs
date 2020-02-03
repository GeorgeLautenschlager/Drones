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
        public class MessageFactory
        {
            public static Message Get(string name, string data)
            {
                var type = name.ToLower().Trim();
                if (type == "broadcast")
                    return Serializer.DeSerialize<BroadcastMessage>(data);
                else if (type == "" || type == "message")
                    return Serializer.DeSerialize<Message>(data);

                return null;
            }

            public static T Get<T>(string data) where T : Message, new()
            {
                return Serializer.DeSerialize<T>(data);
            }
        }
    }
}
