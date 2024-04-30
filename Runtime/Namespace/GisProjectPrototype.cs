using Newtonsoft.Json;
using System;
using GeoJSON.Net.Geometry;
using System.Collections.Generic;

namespace Virgis
{
    public abstract class GisProjectPrototype : TestableObject
    {
        public abstract string path {  set; }   
        protected abstract string TYPE { get;}
        protected abstract string VERSION { get;}

        public  string GetVersion()
        {
            return $"{TYPE}:{VERSION}";
        }

        [JsonProperty(PropertyName = "version", Required = Required.Always)]
        public string ProjectVersion;

        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name;

        [JsonProperty(PropertyName = "guid")]
        private string m_Guid;

        public Guid Guid
        {
            get
            {
                if (m_Guid != null) return Guid.Parse(m_Guid);
                return Guid.Empty;
            }
            set
            {
                m_Guid = value.ToString();
            }
        }

        [JsonProperty(PropertyName = "origin", Required = Required.Always)]
        public Point Origin;

        [JsonProperty(PropertyName = "default_proj", Required = Required.Always)]
        public string projectCrs;

        [JsonProperty(PropertyName = "grid-scale", Required = Required.Always)]
        public float GridScale;

        [JsonProperty(PropertyName = "recordsets", Required = Required.Always)]
        public List<RecordSetPrototype> RecordSets;
    }
}
