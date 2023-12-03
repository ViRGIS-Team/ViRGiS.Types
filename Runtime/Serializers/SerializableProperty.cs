using System;
using Unity.Collections;

namespace Virgis
{
    public struct SerializableProperty : IEquatable<SerializableProperty>
    {
        public float Value;
        public FixedString64Bytes Key;

        public bool Equals(SerializableProperty other)
        {
            return Value == other.Value && Key == other.Key;
        }
    }
}