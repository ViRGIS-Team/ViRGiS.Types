using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

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
        SinglebandGrey,
        Category,
        SingleColor
    }

    /// <summary>
    /// Type of Color Interpretation as per OGC SE
    /// </summary>
    public enum ColorMapType
    {
        Categorize,
        Interpolate
    }

    /// <summary>
    /// An Element of a Color Map Definition
    /// </summary>
    public class ColorMapElement
    {
        /// <summary>
        /// Color used for the element.
        /// 
        /// Can be in either integer[0 .. 255] format or float[0..1] format
        /// </summary>
        [JsonProperty(PropertyName = "color", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableColor>))]
        public SerializableColor Color = new();

        /// <summary>
        /// Threshold as defined in OGC SE
        /// 
        /// NOTE = thresholds must be normalised to the interval [0..1]
        /// </summary>
        [JsonProperty(PropertyName = "threshold", Required = Required.AllowNull)]
        public object Threshold;
    }

    /// <summary>
    /// 
    /// </summary>
    public class ColorMap : TestableObject
    {
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ColorMapType Type;

        [JsonProperty(PropertyName = "values", Required = Required.AllowNull)]
        [JsonConverter(typeof(ColorMapConverter))]
        public List<ColorMapElement> Values = new();

        /// <summary>
        /// As per OGC SE - values are null, "succeeding" or "preceeding"
        /// </summary>
        [JsonProperty(PropertyName = "thresholds-belong")]
        public string ThresholdsBelong;


        public Gradient GetGradient()
        {
            //set up color gradient
            Gradient grad = new();

            GradientColorKey[] colors = new GradientColorKey[Values.Count];
            GradientAlphaKey[] alphas = new GradientAlphaKey[Values.Count];

            float threshold = 0;

            for (int i = 0; i < Values.Count; i++)
            {
                ColorMapElement el = Values[i];
                if (ThresholdsBelong == "preceeding")
                {
                    if (el.Threshold != null) threshold = Convert.ToSingle(el.Threshold);
                }
                colors[i] = new(el.Color, threshold);
                alphas[i] = new(el.Color.a, threshold);

                if (ThresholdsBelong != "preceeding")
                    if (el.Threshold != null) threshold = Convert.ToSingle(el.Threshold);
            }


            grad.SetKeys(colors, alphas);
            grad.mode = GradientMode.PerceptualBlend;
            return grad;
        }

        public Color GetCategoryValue(float value)
        {
            for (int i = 0; i < Values.Count; i++)
            {
                if (ThresholdsBelong == "preceeding")
                {
                    if (i == Values.Count - 1) return (Color)Values[i].Color;
                    if (Convert.ToSingle(Values[i+ 1].Threshold) > value ) return (Color)Values[i].Color;
                }
                else
                {
                    if (Values[i].Threshold == null) return (Color)Values[i].Color;
                    if (Convert.ToSingle(Values[i].Threshold) > value) return (Color)Values[i].Color;
                }
            }
            throw new Exception("Incorect ColorMap detected");
        }
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

        [JsonProperty(PropertyName = "color-map")]
        public ColorMap ColorMap;

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

    public class ColorMapConverter : JsonConverter
    {
        public ColorMapConverter()
        {

        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ColorMapElement).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.StartArray:
                    JArray jarray = JArray.Load(reader);
                    IList<JObject> sets = jarray.Select(c => (JObject)c).ToList();
                    List<ColorMapElement> result = new List<ColorMapElement>();
                    foreach (JObject set in sets)
                    {
                        result.Add(set.ToObject(typeof(ColorMapElement)) as ColorMapElement);
                    }
                    return result;
            }

            throw new JsonReaderException("expected null, object or array token but received " + reader.TokenType);
        }


        public override void WriteJson(JsonWriter writer, object vector, JsonSerializer serializer)
        {
            serializer.Serialize(writer, vector);
        }
    }
}
