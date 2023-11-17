using System;
using Unity.Netcode;
using Unity.Collections;

namespace Virgis
{
    public struct SerializableProperty : INetworkSerializable, IEquatable<SerializableProperty>
    {
        public float Value;
        public int Owner;
        public FixedString64Bytes Name;

        public bool Equals(SerializableProperty other)
        {
            return Value == other.Value && Owner == other.Owner && Name == other.Name;
        }

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Value);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Owner);
        }
        // ~INetworkSerializable
    }
}
