using Newtonsoft.Json;

namespace Virgis
{
    public class UnitPrototype : TestableObject
    {
        /// <summary>
        /// Color used for the unit of symbology.
        /// 
        /// Can be in either integer[0 .. 255] format or float[0..1] format
        /// </summary>
        [JsonProperty(PropertyName = "color", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableColor>))]
        public SerializableColor Color;

        /// <summary>
        /// The transfor to be applied to the unit of symnbology
        /// </summary>
        [JsonProperty(PropertyName = "transform", Required = Required.Always)]
        public JsonTransform Transform;
        /// <summary>
        /// The name of a field in the metadata to be used a label for the data entity
        /// </summary>
        [JsonProperty(PropertyName = "label")]
        public string Label;
    }
}
