using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using Unity.Netcode;
using System.Text;

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
    /// A Graph Unit from a Data Layer
    /// </summary>
    public class DataUnitPrototype : TestableObject,INetworkSerializable
    {
        /// <summary>
        /// The name of the Data Unit
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name;
        /// <summary>
        /// Tranform to be applied to this Data Unit
        /// </summary>
        [JsonProperty(PropertyName = "transform")]
        public JsonTransform Transform = JsonTransform.Zero();
        /// <summary>
        /// The data vizualisation to use
        /// </summary>
        [JsonProperty(PropertyName = "representation", DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue("Points")]
        public DataUnitRepresent Representation;
        /// <summary>
        /// The name of the Table in the source Dataset that is the source of the data
        /// </summary>
        [JsonProperty(PropertyName = "source_table")]
        public string TableName;
        /// <summary>
        /// The range to be used as X value
        /// </summary>
        [JsonProperty(PropertyName = "x_range")]
        public string XRange;
        /// <summary>
        /// The range to be used as Y value
        /// </summary>
        [JsonProperty(PropertyName = "y_range")]
        public string YRange;
        /// <summary>
        /// The range to be used as labels
        /// </summary>
        [JsonProperty(PropertyName = "z_range")]
        public string ZRange;
        /// <summary>
        /// The range to be used as labels
        /// </summary>
        [JsonProperty(PropertyName = "label_range")]
        public string LabelRange;
        /// <summary>
        /// String that defines the axis order - should be "ENU" or "EUN"
        /// </summary>
        [JsonProperty(PropertyName = "axis_order")]
        public string AxisOrder;
        

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
                DataUnitPrototype newS = JsonConvert.DeserializeObject<DataUnitPrototype>(Encoding.UTF8.GetString(s));
                Name = newS.Name;
                Representation = newS.Representation;
                TableName = newS.TableName;
                XRange = newS.XRange;
                YRange = newS.YRange;
                ZRange = newS.ZRange;
                LabelRange = newS.LabelRange;
                AxisOrder = newS.AxisOrder;
            }
        }
    }
}
