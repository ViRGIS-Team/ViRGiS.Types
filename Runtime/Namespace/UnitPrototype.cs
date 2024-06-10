using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Virgis
{
    /// <summary>
    /// Acceptable value for the Shape field in Symbology
    /// </summary>
    public enum Shapes
    {
        None,
        Spheroid,
        Cuboid,
        Cylinder,
    }

    /// <summary>
    /// Acceptable values for color-mode
    /// </summary>
    public enum ColorMode
    {
        MultibandColor,
        SinglebandColor,
        SinglebandGrey
    }

    public class UnitPrototype : TestableObject
    {
        /// <summary>
        /// Color used for the unit of symbology.
        /// 
        /// Can be in either integer[0 .. 255] format or float[0..1] format
        /// </summary>
        [JsonProperty(PropertyName = "color", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableColor>))]
        public SerializableColor Color = new();

        /// <summary>
        /// The transfor to be applied to the unit of symnbology
        /// </summary>
        [JsonProperty(PropertyName = "transform", Required = Required.Always)]
        public JsonTransform Transform = new();
        /// <summary>
        /// The name of a field in the metadata to be used a label for the data entity
        /// </summary>
        [JsonProperty(PropertyName = "label")]
        public string Label;

        /// <summary>
        /// The shape to be used by the unit of symbology.
        /// 
        /// Must contain an instance of Shapes
        /// </summary>
        [JsonProperty(PropertyName = "shape", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Shapes Shape;

        /// <summary>
        /// Color mode to be used for raster layers
        /// </summary>
        [JsonProperty(PropertyName = "color-mode", DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue("SinglebandGrey")]
        public ColorMode ColorMode;
        /// <summary>
        /// PDAL Colorinterp string
        /// </summary>
        [JsonProperty(PropertyName = "colorinterp")]
        public Dictionary<string, object> ColorInterp;

        public bool GetCI ( out Dictionary<string, object> ci)
        {
            if (ColorMode == ColorMode.SinglebandColor && ColorInterp != null)
            {
                ci = new(ColorInterp);
                ci["type"] = "filters.colorinterp";
                ci["dimension"] = ColorInterp.TryGetValue("dimension", out object t) ?
                    t : "Z";
                return true;
            }
            ci = null;
            return false;
        }
    }
}
