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
        public class Field
        {
            private byte[] value;
            public Dictionary<string, Field> children = new Dictionary<string, Field>();

            /// <summary>
            /// Creates a field with value of a simple byte array
            /// </summary>
            /// <param name="value">Value of the field</param>
            public Field(byte[] value)
            {
                this.value = value;
            }

            /// <summary>
            /// Creates a field with children from a serializable object
            /// </summary>
            /// <param name="sObject">Object to store</param>
            public Field(Serializable sObject)
            {
                sObject.SaveToFields();
                children = sObject.GetFields();
            }

            /// <summary>
            /// Creates a field with children from a vector
            /// </summary>
            /// <param name="value"></param>
            public Field(Vector3 value)
            {
                children["x"] = new Field(value.X);
                children["y"] = new Field(value.Y);
                children["z"] = new Field(value.Z);
            }

            /// <summary>
            /// Creates a field with value converted from a float
            /// </summary>
            /// <param name="value"></param>
            public Field(float value)
            {
                this.value = BitConverter.GetBytes(value);
            }

            public Field(long value)
            {
                this.value = LongToBytes(value);
            }

            /// <summary>
            /// Creates a field with value converted from a double
            /// </summary>
            /// <param name="value"></param>
            public Field(double value)
            {
                this.value = BitConverter.GetBytes(value);
            }

            /// <summary>
            /// Creates a field with value converted from a integer
            /// </summary>
            /// <param name="value"></param>
            public Field(int value)
            {
                this.value = BitConverter.GetBytes(value);
            }

            /// <summary>
            /// Creates a field with value converted from a boolean
            /// </summary>
            /// <param name="value"></param>
            public Field(bool value)
            {
                this.value = BitConverter.GetBytes(value);
            }

            /// <summary>
            /// Creates a field with value converted from a string
            /// </summary>
            /// <param name="value"></param>
            public Field(string value)
            {
                this.value = System.Text.Encoding.ASCII.GetBytes(value);
            }

            /// <summary>
            /// Sets the children of a field
            /// Used for storing for storage when type is unknown try to used serializable objects for anything else
            /// </summary>
            /// <param name="children">Children fields</param>
            public Field(Dictionary<string, Field> children)
            {
                this.children = children;
            }

            /**
            Functions that convert the byte value to a usable object
            Make sure that the correct get function is used!!
                */

            /// <summary>
            /// Gets the byte value as a vector
            /// </summary>
            /// <returns></returns>
            public Vector3 GetVector3()
            {
                Vector3 vector = new Vector3();
                vector.X = children["x"].GetFloat();
                vector.Y = children["y"].GetFloat();
                vector.Z = children["z"].GetFloat();
                return vector;
            }
            /// <summary>
            /// Gets the byte value as a float
            /// </summary>
            /// <returns></returns>
            public float GetFloat()
            {
                return BitConverter.ToSingle(value, 0);
            }

            /// <summary>
            /// Gets the byte value as a double
            /// </summary>
            /// <returns></returns>
            public double GetDouble()
            {
                return BitConverter.ToDouble(value, 0);
            }

            /// <summary>
            /// Gets the byte value as an integer
            /// </summary>
            /// <returns></returns>
            public int GetInt()
            {
                return BitConverter.ToInt32(value, 0);
            }

            /// <summary>
            /// Gets the byte value as an integer
            /// </summary>
            /// <returns>Value as bool</returns>
            public bool GetBool()
            {
                return BitConverter.ToBoolean(value, 0);
            }

            /// <summary>
            /// Gets the byte value as a string
            /// </summary>
            /// <returns>Value as string</returns>
            public string GetString()
            {
                return System.Text.Encoding.ASCII.GetString(value);
            }

            /// <summary>
            /// Gets the byte value as an object of type T
            /// </summary>
            /// <returns>Object of type T</returns>
            public T GetObject<T>() where T : Serializable, new()
            {
                T obj = new T();
                obj.LoadFields(children);
                return obj;
            }

            public long GetLong()
            {
                return BytesToLong(value);
            }

            private static byte[] LongToBytes(long l)
            {
                byte[] result = new byte[8];
                for (int i = 7; i >= 0; i--)
                {
                    result[i] = (byte)(l & 0xFF);
                    l >>= 8;
                }
                return result;
            }

            private static long BytesToLong(byte[] b)
            {
                long result = 0;
                for (int i = 0; i < 8; i++)
                {
                    result <<= 8;
                    result |= (uint)(b[i] & 0xFF);
                }
                return result;
            }

            /// <summary>
            /// Gets the value
            /// </summary>
            /// <returns>Byte array</returns>
            public byte[] GetBytes()
            {
                return value;
            }

            /// <summary>
            /// Coverts a dictionary of fields to a string
            /// </summary>
            /// <param name="fields"></param>
            /// <returns></returns>
            public static string DicToString(Dictionary<string, Field> fields)
            {
                string result = "";
                foreach (KeyValuePair<string, Field> child in fields)
                {
                    result += child.Key + ":" + child.Value.ToString();
                };
                return result;
            }

            /// <summary>
            /// Converts a Field to a string representation
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string result = "{";
                if (value != null)
                    result += Serializer.BytesToString(value);
                else
                    result += DicToString(children);

                result += "}";
                return result;
            }
        }
    }
}
