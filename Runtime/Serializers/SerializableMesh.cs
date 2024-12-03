using VirgisGeometry;
using Unity.Netcode;
using UnityEngine;
using Draco;
using Draco.Encoder;
using System;
using System.Linq;
using System.Collections.Generic;


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

        /// <summary>
        /// Delegate type for Mesh Vertex changed event
        /// </summary>
        /// <param name="vID">the array of vertex IDs</param>
        /// <param name="values">the array of new Vector3 positions</param>
        public delegate void OnVertexChangedDelegate(int[] vIDs, Vector3[] values);

        /// <summary>
        /// The callback to be invoked when the vertex gets changed
        /// </summary>
        public OnVertexChangedDelegate OnVertexChanged;

        public DMesh3 DMesh3
        {
            get { return m_Dmesh; }
            set {
                value.CompactInPlace();
                m_Dmesh = value;
                SubMesh = new(value);
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
        }

        private async void MeshDeserialize()
        {
            DracoMeshLoader decoder = new(false);
            m_Mesh = await decoder.ConvertDracoMeshToUnity(m_Data, true, true);
            OnMeshChanged.Invoke(m_Mesh);
        }

        public void SendUpdateSubmesh()
        {
            SetDirty(true);
            List<Vector3> tmp = new();
            foreach (Vector3d v in SubMesh.Vertices()) tmp.Add((Vector3)v);
            OnVertexChanged.Invoke(SubMesh.VertexIndices().ToArray(), tmp.ToArray());
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            int[] vIDs = SubMesh.VertexIndices().ToArray();
            writer.WriteValueSafe(vIDs);
            Vector3[] vertices = new Vector3[vIDs.Length];
            foreach (int vID in vIDs)
            {
                Vector3d v3d = SubMesh.GetVertex(vID);
                v3d.ChangeAxisOrderTo(AxisOrder.EUN);
                writer.WriteValueSafe((float)v3d.x);
                writer.WriteValueSafe((float)v3d.y);
                writer.WriteValueSafe((float)v3d.z);
            }
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
            reader.ReadValueSafe(out int[] vIDs);
            Vector3[] vertices = new Vector3[vIDs.Length];
            for (int i = 0; i < vIDs.Length; i++)
            {
                reader.ReadValueSafe(out float x);
                reader.ReadValueSafe(out float y);
                reader.ReadValueSafe(out float z);
                vertices[i] = new Vector3(x, y, z);
            }
            OnVertexChanged.Invoke(vIDs, vertices);
        }

        public bool IsMesh { get { return m_Mesh != null; } }
    }
}
