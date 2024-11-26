using System;
using VirgisGeometry;
using Unity.Netcode;
using UnityEngine;
using Draco;
using Draco.Encoder;


namespace Virgis
{
    public class SerializableMesh : NetworkVariableBase, IEquatable<SerializableMesh>
    {
        private DMesh3 dmesh;
        private Mesh umesh;

        private byte[] data;

        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate(Mesh newMesh);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        public DMesh3 Value
        {
            get { return dmesh; }
            set {
                value.CompactInPlace();
                dmesh = value;
                Mesh mesh = (Mesh)value;
                OnValueChanged.Invoke(mesh);
                MeshSerialize(mesh);
            }
        }

        public static explicit operator Mesh(SerializableMesh smesh) => smesh.umesh;

        private void MeshSerialize(Mesh mesh)
        {
            EncodeResult[] serResult = DracoEncoder.EncodeMesh(mesh, Vector3.one, 0.01f);
            data = serResult[0].data.ToArray();
        }

        private async void MeshDeserialize()
        {
            DracoMeshLoader decoder = new(false);
            umesh = await decoder.ConvertDracoMeshToUnity(data, true, true);
            OnValueChanged.Invoke(umesh);
        }

    
        public bool Equals(SerializableMesh other)
        {
            return dmesh.IsSameMesh(other.dmesh, false, false, true, true, true, false);
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            // nothing
        }

        public override void WriteField(FastBufferWriter writer)
        {
            Debug.Log("Serialize Mesh");
            if (data == null)
            {
                writer.WriteValueSafe(0);
            } else 
            {
                writer.WriteValueSafe(data.Length);
                writer.WriteValueSafe(data);
            }
        }

        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            Debug.Log("Deserialize Mesh");
            reader.ReadValueSafe(out int size);
            if (size != 0) 
            {
                data = new byte[size];
                reader.ReadValueSafe(out data);
                MeshDeserialize();
            }
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // nothing
        }

        public bool IsMesh { get { return umesh != null; } }
    }
}
