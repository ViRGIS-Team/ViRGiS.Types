using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
// ReSharper disable Unity.PerformanceCriticalCodeInvocation

namespace Virgis
{

    public class SerializableSymbology : NetworkVariableBase
    {
        
        public DataUnitPrototype DataUnit = new();
        public UnitPrototype Point = new();
        public UnitPrototype Line = new();
        public UnitPrototype Body = new();
        
        /// <summary> 
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate( DataUnitPrototype  newValue );
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;
        
        public override void WriteDelta(FastBufferWriter writer)
        {
            // do nothing
        }

        public override void WriteField(FastBufferWriter writer)
        {
            writer.WriteValueSafe(DataUnit);
            writer.WriteValueSafe(Point);
            writer.WriteValueSafe(Line);
            writer.WriteValueSafe(Body);
        }

        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out DataUnit);
            reader.ReadValueSafe(out Point);
            reader.ReadValueSafe(out Line);
            reader.ReadValueSafe(out Body);
            OnValueChanged?.Invoke(DataUnit);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // do nothing
        }
        
        public void FromSymbology(Dictionary<string, UnitPrototype> symbology)
        {
            foreach (KeyValuePair<string, UnitPrototype> pair in symbology)
            {
                string key = pair.Key;
                switch (key)
                {
                    case "point": Point = pair.Value; break;
                    case "line": Line = pair.Value; break;
                    case "body": Body = pair.Value; break;
                }
            }
            SetDirty(true);
        }
        
        public void SetDataUnit(DataUnitPrototype data)
        {
            DataUnit = data;
            SetDirty(true);
        }
    }
}