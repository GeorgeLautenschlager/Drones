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
        public class Message : Serializable
        {
            public string Action { get; set; }
            public long Source { get; set; }
            public Dictionary<string, Field> Arguments { get; set; }

            public Message()
            {
                Action = "Unknown";
                Source = long.Parse("0");
                Arguments = new Dictionary<string, Field>();
            }

            public override void LoadFields(Dictionary<string, Field> fields)
            {
                this.Action = fields["action"].GetString();
                this.Source = fields["source"].GetLong();
                this.Arguments = fields["arguments"].children;

                this.Load(fields);
            }

            public override void SaveToFields()
            {
                fields["action"] = new Field(this.Action);
                fields["source"] = new Field(this.Source);
                fields["arguments"] = new Field(this.Arguments);
                this.Save();
            }

            public virtual void Load(Dictionary<string, Field> fields)
            {
            }

            public virtual void Save()
            {
            }
        }
    }
}
