using Newtonsoft.Json;
using System.ComponentModel;
using GeoJSON.Net.Geometry;

namespace Virgis
{
    public class RecordSetPrototype : TestableObject
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
    }
}
