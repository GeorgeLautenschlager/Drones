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
        public abstract class Serializable
        {
            protected Dictionary<string, Field> fields = new Dictionary<string, Field>();
            /// <summary>
            /// Stores all fields that need to be serialized in the fields dictionary
            /// </summary>
            public abstract void SaveToFields();

            /// <summary>
            /// Applies all fields stored in a dictionary
            /// Remember fields that are not saved can not be loaded!!!
            /// </summary>
            /// <param name="fields">Dictionary to take load fields from</param>
            public abstract void LoadFields(Dictionary<string, Field> fields);

            /// <summary>
            /// Serializes this object and its fields to a string
            /// </summary>
            /// <returns></returns>
            public string Serialize()
            {
                SaveToFields();
                return Field.DicToString(fields);
            }

            /// <summary>
            /// Gets the protected fields variable after the SaveToFields function is called
            /// </summary>
            /// <returns>Dictionary of fields</returns>
            public Dictionary<string, Field> GetFields()
            {
                SaveToFields();
                return fields;
            }
        }
    }
}
