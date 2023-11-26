using System;
using Unity.Netcode;
using Unity.Collections;

namespace Virgis
{
    public struct SerializableProperty : INetworkSerializable, IEquatable<SerializableProperty>
    {
        public float Value;
        public FixedString64Bytes Key;

        public bool Equals(SerializableProperty other)
        {
            return Value == other.Value && Key == other.Key;
        }

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Value);
            serializer.SerializeValue(ref Key);
        }
        // ~INetworkSerializable
    }
}
