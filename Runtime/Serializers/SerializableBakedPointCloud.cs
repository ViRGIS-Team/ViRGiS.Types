using System;
using Unity.Netcode;
using UnityEngine;

namespace Virgis
{
    [Serializable]
    public class SerializableBakedPointCloud : NetworkVariableBase
    {
        public Texture2D PositionMap;
        public Texture2D ColorMap;

        public int width;

        public int PointCount;


        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate( Texture2D newPositions, Texture2D newColors, int PointCount);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// Sets the <see cref="Value"/>, marks the <see cref="NetworkVariable{T}"/> dirty, and invokes the <see cref="OnValueChanged"/> callback
        /// if there are subscribers to that event.
        /// </summary>
        /// <param name="value">the new value of type `T` to be set/></param>
        public void Set(Texture2D positions, Texture2D colors, int pc)
        {
            SetDirty(true);
            PositionMap = positions;
            ColorMap = colors;
            PointCount = pc;
            OnValueChanged?.Invoke( positions, colors, pc);
        }

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            if (PositionMap == null)
            {
                writer.WriteValueSafe(0);
                return;
            } else {
                writer.WriteValueSafe(width);
            }
            writer.WriteValueSafe(PointCount);

            // Serialize the data we need to synchronize
            writer.WriteValueSafe(PositionMap.EncodeToPNG());
            writer.WriteValueSafe(ColorMap.EncodeToPNG());
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out width);
            if (width == 0) return;
            reader.ReadValueSafe(out PointCount);

            PositionMap = new Texture2D(width, width, TextureFormat.RGBAFloat, false)
            {
                name = "Position Map",
                filterMode = FilterMode.Point
            };

            ColorMap = new Texture2D(width, width, TextureFormat.RGBA32, false)
            {
                name = "Color Map",
                filterMode = FilterMode.Point
            };

            // De-Serialize the data being synchronized

            byte[] positions = new byte[width * width];
            reader.ReadValueSafe(out positions);
            PositionMap.LoadImage(positions);
            byte[] colors = new byte[width * width];
            reader.ReadValueSafe(out colors);
            ColorMap.LoadImage(colors);
            OnValueChanged?.Invoke(PositionMap, ColorMap, PointCount);
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
