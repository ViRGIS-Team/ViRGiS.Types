using System;
using Unity.Netcode;
using UnityEngine;

namespace Virgis
{
    [Serializable]
    public class SerializableTexture : NetworkVariableBase
    {
        public Texture2D tex;

        /// <summary> 
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate( Texture2D newValue);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// Sets the <see cref="Value"/>, marks the <see cref="NetworkVariable{T}"/> dirty, and invokes the <see cref="OnValueChanged"/> callback
        /// if there are subscribers to that event.
        /// </summary>
        /// <param name="value">the new value of type `T` to be set/></param>
        public void Set(Texture2D value)
        {
            SetDirty(true);
            tex = value;
            OnValueChanged?.Invoke( tex);
        }

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            if (tex == null)
            {
                writer.WriteValueSafe<int>(0);
                return;
            }

            // Serialize the data we need to synchronize
            writer.WriteValueSafe(tex.EncodeToPNG());
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            tex = new Texture2D(1, 1);
            reader.ReadValueSafe(out byte[] received);
            tex.LoadImage(received);
            OnValueChanged?.Invoke(tex);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // Don'thing for this example
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            // Don'thing for this example
        }
    }
}
