using Unity.Netcode;
using System;

namespace Virgis {
    public struct SerializableColorArray : INetworkSerializable, IEquatable<SerializableColorArray>
    {
        private int[] m_Colors;
        public Guid guid { get; private set; }

        public int[] Colors {
            get { return m_Colors; }
            set { 
                m_Colors = value;
                guid = Guid.NewGuid();
            }
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
                    m_Colors = new int[0];
                    return;
                }
                m_Colors = new int[length];
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
