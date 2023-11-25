using Newtonsoft.Json;
using System.ComponentModel;
using GeoJSON.Net.Geometry;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using System.Text;

namespace Virgis
{
    public class RecordSetPrototype : TestableObject,INetworkSerializable, IEquatable<RecordSetPrototype>
    {

        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id;
        [JsonProperty(PropertyName = "display-name")]
        public string DisplayName;
        [JsonProperty(PropertyName = "position")]
        public Point Position;
        [JsonProperty(PropertyName = "transform")]
        public JsonTransform Transform;
        [JsonProperty(PropertyName = "visible", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool Visible;
                /// <summary>
        /// Dictionary of symbology units for this layer
        /// </summary>
        [JsonProperty(PropertyName = "units")]
        public Dictionary<string, UnitPrototype> Units;

        /// <summary>
        /// List of Data Units for this layer
        /// </summary>
        [JsonProperty(PropertyName = "data_units")]
        public List<DataUnitPrototype> DataUnits;

        public bool Equals(RecordSetPrototype other)
        {
            return Id == other.Id && DisplayName == other.DisplayName;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter){
                byte[] s = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(s.Length);
                writer.WriteValueSafe(s);
            } else {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int byteCount);
                byte[] s = new byte[byteCount];
                reader.ReadValueSafe(out s);
                RecordSetPrototype newS = JsonConvert.DeserializeObject<RecordSetPrototype>(Encoding.UTF8.GetString(s));
                Id = newS.Id;
                DisplayName = newS.DisplayName;
                Position = newS.Position;
                Transform = newS.Transform;
                Visible = newS.Visible;
            }
        }
    }
}
