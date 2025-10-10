using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        [JsonProperty(PropertyName = "source")]
        public string m_source;
        [JsonIgnore]
        public virtual string Source
        {
            get { return m_source; }
            set { m_source = value; }
        }
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

    /// <summary>
    /// Acceptable values for the Source field of a recordset
    /// </summary>
    public enum SourceType
    {
        File,
        WFS,
        OAPIF,
        WMS,
        WCS,
        PG,
        AWS,
        GCS,
        Azure,
        Alibaba,
        Openstack,
        TCP,
    }

    /// <summary>
    /// Prototype for the Recordset Properties field
    /// </summary>
    public class PropertiesPrototype
    {
        /// <summary>
        /// DEM or DTM to map these values onto
        /// </summary>
        [JsonProperty(PropertyName = "dem")]
        public string m_Dem;

        [JsonIgnore]
        public virtual string Dem
        {
            get
            { return m_Dem; }
        }
        /// <summary>
        /// Header string to be used when converting raster bands to point cloud data for vizualisation
        /// identifies the properties names that the raster bands are mapped to in order
        /// </summary>
        [JsonProperty(PropertyName = "header-string")]
        public string headerString;
        /// <summary>
        /// PDAL Filter String
        /// </summary>
        [JsonProperty(PropertyName = "filter")]
        public List<Dictionary<string, object>> Filter;
        /// <summary>
        /// Bounding Box
        /// </summary>
        [JsonProperty(PropertyName = "bbox")]
        public List<double> BBox;
        /// <summary>
        /// GDAL Source Type
        /// </summary>
        [JsonProperty(PropertyName = "source-type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(SourceType.File)]
        public SourceType SourceType;
        /// <summary>
        /// Open Read only ?
        /// </summary>
        [JsonProperty(PropertyName = "read-only", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool ReadOnly;
    }
}
