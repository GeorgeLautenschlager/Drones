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
        public class Serializer
        {
            public static T DeSerialize<T>(string obj) where T : Serializable, new()
            {
                T result = new T();
                Dictionary<string, Field> fieldsFromString = StringToFields(obj);
                result.LoadFields(fieldsFromString);
                return result;
            }

            public static string Serialize(Serializable obj)
            {
                return obj.Serialize();
            }

            public static Dictionary<string, Field> StringToFields(string fields)
            {
                Dictionary<string, Field> result = new Dictionary<string, Field>();
                string fieldName = "";
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] == '}')
                    {
                        return result;
                    }
                    else if (fields[i] == ':' && fields[i + 1] == '{')
                    {
                        string subField = fields.Substring(i + 2);
                        int subEnd = ClosingBracket(subField);
                        subField = subField.Substring(0, subEnd);
                        result.Add(fieldName, parseField(subField));
                        i += 2 + subEnd;
                        fieldName = "";
                    }
                    else
                    {
                        fieldName += fields[i];
                    }
                }
                return result;
            }

            public static Field parseField(string field)
            {
                string value = "";
                if (field.IndexOf(':') != -1)
                {
                    return new Field(StringToFields(field));
                }
                for (int i = 0; i < field.Length; i++)
                {
                    value += field[i];
                }
                return new Field(StringToBytes(value));
            }

            public static string BytesToString(byte[] ba)
            {
                string hex = BitConverter.ToString(ba);
                return hex.Replace("-", "");
            }

            public static byte[] StringToBytes(string byteString)
            {
                int NumberChars = byteString.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(byteString.Substring(i, 2), 16);
                }
                return bytes;
            }

            public static int ClosingBracket(string s)
            {
                int nextOpening = s.IndexOf('{');
                int nextClosing = s.IndexOf('}');
                while (nextOpening != -1 && nextOpening < nextClosing)
                {
                    nextOpening = s.IndexOf('{', nextOpening + 1);
                    nextClosing = s.IndexOf('}', nextClosing + 1);
                }
                return nextClosing;
            }
        }
    }
}
