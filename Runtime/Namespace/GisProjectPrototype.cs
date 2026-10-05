using Newtonsoft.Json;
using System;
using GeoJSON.Net.Geometry;
using System.Collections.Generic;

namespace Virgis
{
    public abstract class GisProjectPrototype : TestableObject
    {
        public abstract string path { get; set; }   
        protected abstract string Type { get;}
        protected abstract string Version { get;}

        public  string GetVersion()
        {
            return $"{Type}:{Version}";
        }

        [JsonProperty(PropertyName = "version", Required = Required.Always)]
        public string ProjectVersion;

        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name;

        [JsonProperty(PropertyName = "guid")]
        private string _mGuid;

        [JsonIgnore]
        public Guid Guid
        {
            get
            {
                if (_mGuid != null) return Guid.Parse(_mGuid);
                return Guid.Empty;
            }
            set
            {
                _mGuid = value.ToString();
            }
        }

        [JsonProperty(PropertyName = "origin", Required = Required.Always)]
        public Point Origin;

        [JsonProperty(PropertyName = "default_proj", Required = Required.Always)]
        public string ProjectCrs;

        [Obsolete("Map Scale is not used in Virgis 3.0")]
        [JsonProperty(PropertyName = "map_scale")]
        public float MapScale;

        [JsonProperty(PropertyName = "recordsets", Required = Required.Always)]
        public List<RecordSetPrototype> RecordSets;
    }
}
