using Unity.Netcode;
using System;
using UnityEngine;

namespace Virgis {
    public struct SerializableColorArray : INetworkSerializable, IEquatable<SerializableColorArray>
    {
        private byte[] m_Colors;
        public Guid guid { get; private set; }

        public byte[] Colors {
            get { return m_Colors; }
            set { 
                m_Colors = value;
                guid = Guid.NewGuid();
            }
        }

        public Vector2[] ToUV()
        {
            Vector2[] uv = new Vector2[Colors.Length];
            for (int i = 0; i < Colors.Length; i++)
            {
                uv[i] = new Vector2(Colors[i], i);
            };
            return uv;
        }

        public bool Equals(SerializableColorArray other)
        {
            if (guid == null || other.guid == null) return false;
            return guid == other.guid;
        }

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int length);
                if (length == 0)
                {
                    m_Colors = new byte[0];
                    return;
                }
                m_Colors = new byte[length];
                reader.ReadValueSafe(out m_Colors);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                if (m_Colors == null || m_Colors.Length == 0)
                {
                    writer.WriteValueSafe(0);
                    return;
                }
                writer.WriteValueSafe(m_Colors.Length);
                writer.WriteValueSafe(m_Colors);
            }
        }
    }
}
