using System;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

namespace Virgis
{
    public struct SerializableMaterialHash : INetworkSerializable, IEquatable<SerializableMaterialHash>
    {
        public FixedString64Bytes Name;
        public Color Color;

        public SerializableProperty[] properties;


        public bool Equals(SerializableMaterialHash other)
        {
            return Name == other.Name && Color == other.Color;
        }

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                // De-Serialize the data being synchronized
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Name);
                if (Name == "") return;
                reader.ReadValueSafe(out Color);
                reader.ReadValueSafe(out int numprops);
                properties = new SerializableProperty[numprops];
                for (int i = 0; i < numprops; i++)
                {
                    reader.ReadValueSafe(out FixedString64Bytes Key);
                    reader.ReadValueSafe(out float Value);
                    properties[i] = (new() { Key = Key, Value = Value });
                }
            }
            else {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Name);
                if (Name == "") return;
                writer.WriteValueSafe(Color);
                if (properties == null)
                {
                    writer.WriteValueSafe(0);
                    return;
                }
                writer.WriteValueSafe(properties.Length);
                foreach( SerializableProperty prop in properties)
                {
                    writer.WriteValueSafe(prop.Key);
                    writer.WriteValueSafe(prop.Value);
                }
            }
        }
    }
}
