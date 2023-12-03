using Unity.Netcode;
using System;

namespace Virgis {
    public struct SerializableColorArray : INetworkSerializable, IEquatable<SerializableColorArray>
    {
        public int[] colors;

        public bool Equals(SerializableColorArray other)
        {
            return colors == other.colors;
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
                    colors = new int[0];
                    return;
                }
                colors = new int[length];
                reader.ReadValueSafe(out colors);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                if (colors == null || colors.Length == 0)
                {
                    writer.WriteValueSafe(0);
                    return;
                }
                writer.WriteValueSafe(colors.Length);
                writer.WriteValueSafe(colors);
            }
        }
    }
}
