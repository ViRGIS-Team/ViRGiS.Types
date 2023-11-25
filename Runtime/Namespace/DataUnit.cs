using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Virgis
{

    public enum DataUnitRepresent{
        Line,
        Area,
        Manifold,
        Volume,
        Points,
        PointCloud
    }

    /// <summary>
    /// A Graph Unit from a Data Level
    /// </summary>
    public abstract class DataUnitPrototype : TestableObject
    {
        /// <summary>
        /// The name of the Data Unit
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name;
        /// <summary>
        /// The data vizualisation to use
        /// </summary>
        [JsonProperty(PropertyName = "representation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DataUnitRepresent Representation;
        /// <summary>
        /// The name of the Table in the source Dataset that is the source of the data
        /// </summary>
        [JsonProperty(PropertyName = "source_table")]
        public string TableName;
        /// <summary>
        /// The rage to be used as labels
        /// </summary>
        [JsonProperty(PropertyName = "label_range")]
        public string LabelRange;
        /// <summary>
        /// Dictionary of symbology units for this data unit
        /// </summary>
        [JsonProperty(PropertyName = "units")]
        public Dictionary<string, UnitPrototype> Units;
    }
}
