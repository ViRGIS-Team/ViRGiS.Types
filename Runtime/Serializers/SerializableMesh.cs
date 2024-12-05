using VirgisGeometry;
using Unity.Netcode;
using UnityEngine;
using Draco;
using Draco.Encoder;
using System;


namespace Virgis
{
    public class SerializableMesh : NetworkVariableBase
    {
        private DMesh3 m_Dmesh;
        private Mesh m_Mesh;

        private byte[] m_Data;

        public DSubmesh3 SubMesh;

        /// <summary>
        /// Delegate type for Mesh changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnMeshChangedDelegate(Mesh newMesh);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnMeshChangedDelegate OnMeshChanged;

        public DMesh3 DMesh3
        {
            get { return m_Dmesh; }
            set {
                value.CompactInPlace();
                SubMesh = new(value);
                m_Dmesh = value;
                m_Mesh = (Mesh)m_Dmesh;
            }
        }

        public Mesh Mesh { get { return m_Mesh; } }

        public void MeshFinalize()
        {
            OnMeshChanged.Invoke(m_Mesh);
            EncodeResult[] serResult = DracoEncoder.EncodeMesh(m_Mesh, Vector3.one, 0.01f);
            m_Data = serResult[0].data.ToArray();
            Array.ForEach(serResult, res => res.Dispose());
            SetDirty(true);
        }

        private async void MeshDeserialize()
        {
            DracoMeshLoader decoder = new(false);
            m_Mesh = await decoder.ConvertDracoMeshToUnity(m_Data, true, true);
            OnMeshChanged.Invoke(m_Mesh);
        }

        //public void SendUpdateSubmesh()
        //{
        //    SetDirty(true);

        //}

        public override void WriteDelta(FastBufferWriter writer)
        {
            WriteField(writer);
        }

        public override void WriteField(FastBufferWriter writer)
        {
            Debug.Log("Serialize Mesh");
            if (m_Data == null)
            {
                writer.WriteValueSafe(0);
            } else 
            {
                writer.WriteValueSafe(m_Data.Length);
                writer.WriteValueSafe(m_Data);
            }
        }

        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            Debug.Log("Deserialize Mesh");
            reader.ReadValueSafe(out int size);
            if (size != 0) 
            {
                m_Data = new byte[size];
                reader.ReadValueSafe(out m_Data);
                MeshDeserialize();
            }
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            ReadField(reader);
        }

        public bool IsMesh { get { return m_Mesh != null; } }
    }
}
