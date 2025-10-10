using System;
using System.IO.Compression;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

namespace Virgis
{
    [Serializable]
    public class SerializableBakedPointCloud : NetworkVariableBase
    {
        public Texture2D PositionMap;
        public Texture2D ColorMap;
        public int PointCount;
        public float PixelSize;

        private byte[] m_Positions;
        private byte[] m_Colors;
        private int width;


        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate( Texture2D newPositions, Texture2D newColors, int PointCount, float PixelSize);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// Sets the <see cref="Value"/>, marks the <see cref="NetworkVariable{T}"/> dirty, and invokes the <see cref="OnValueChanged"/> callback
        /// if there are subscribers to that event.
        /// </summary>
        /// <param name="value">the new value of type `T` to be set/></param>
        public void Set(Texture2D positions, Texture2D colors, int pc, float px)
        {
            PositionMap = positions;
            width = positions.width;
            byte[] b_pos = PositionMap.GetRawTextureData();
            using (MemoryStream o_pos = new())
            {
                using (DeflateStream dstream = new(o_pos, System.IO.Compression.CompressionLevel.Optimal))
                {
                    dstream.Write(b_pos, 0, b_pos.Length);
                }
                m_Positions = o_pos.ToArray();
            }
            ColorMap = colors;
            byte[] b_col = ColorMap.GetRawTextureData();
            using (MemoryStream o_col = new())
            {
                using (DeflateStream dstream = new(o_col, System.IO.Compression.CompressionLevel.Optimal))
                {
                    dstream.Write(b_col, 0, b_col.Length);
                }
                m_Colors = o_col.ToArray();
            }
            PointCount = pc;
            PixelSize = px;
            SetDirty(true);
            OnValueChanged?.Invoke( PositionMap, ColorMap, PointCount, PixelSize);
        }

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            if (m_Positions == null)
            {
                writer.WriteValueSafe(0);
                return;
            } else {
                writer.WriteValueSafe(PointCount);
            }
            writer.WriteValueSafe(PixelSize);
            writer.WriteValueSafe(width);

            // Serialize the data we need to synchronize
            writer.WriteValueSafe(m_Positions);
            writer.WriteValueSafe(m_Colors);
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out PointCount);
            if (PointCount == 0) return;
            reader.ReadValueSafe(out PixelSize);
            reader.ReadValueSafe(out width);

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

            reader.ReadValueSafe(out byte[] b_pos);
            reader.ReadValueSafe(out byte[] b_col);

            NativeArray<byte> raw_pos = PositionMap.GetRawTextureData<byte>();
            using (MemoryStream i_pos = new(b_pos))
            using (MemoryStream o_pos = new())
            {
                using (DeflateStream dstream = new(i_pos, CompressionMode.Decompress))
                {
                    dstream.CopyTo(o_pos);
                }
                raw_pos.CopyFrom(o_pos.ToArray());
            };

            NativeArray<byte> raw_col = ColorMap.GetRawTextureData<byte>();
            using (MemoryStream i_col = new(b_col))
            using (MemoryStream o_col = new())
            {
                using (DeflateStream dstream = new(i_col, CompressionMode.Decompress))
                {
                    dstream.CopyTo(o_col);
                }
                raw_col.CopyFrom(o_col.ToArray());
            };

            PositionMap.Apply(false, false);
            ColorMap.Apply(false, false);

            OnValueChanged?.Invoke(PositionMap, ColorMap, PointCount, PixelSize);
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
