using System;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Virgis
{
    public struct SerializableColorHash : INetworkSerializable, IEquatable<SerializableColorHash>
    {
        public FixedString64Bytes Name;
        public Color Color;

        public SerializableProperty Property1;
        public SerializableProperty Property2;
        public SerializableProperty Property3;

        public bool Equals(SerializableColorHash other)
        {
            return Name == other.Name && Color == other.Color;
        }

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Color);
            serializer.SerializeValue(ref Property1);
            serializer.SerializeValue(ref Property2);
            serializer.SerializeValue(ref Property3);
        }
        // ~INetworkSerializable
    }
}
