using System;
using System.Linq;
using g3;
using Unity.Netcode;
using UnityEngine;

namespace Virgis
{
    public class SerializableMesh : INetworkSerializable, IEquatable<SerializableMesh>
    {
        private Vector3[] vertices;

        private Color32[] colors;
        private Vector2[] uvs;
        private int[] tris;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader) {
                // De-Serialize the data being synchronized
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int vertexCount);
                if (vertexCount == 0) return;
                reader.ReadValueSafe(out int triCount);
                vertices = new Vector3[vertexCount];
                reader.ReadValueSafe(out vertices);
                colors = new Color32[vertexCount];
                reader.ReadValueSafe(out colors);
                uvs = new Vector2[vertexCount];
                reader.ReadValueSafe(out uvs);
                tris = new int[triCount];
                reader.ReadValueSafe(out tris);
            } else {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(vertices.Length);
                writer.WriteValueSafe(tris.Length);
                writer.WriteValueSafe(vertices);
                writer.WriteValueSafe(colors);
                writer.WriteValueSafe(uvs);
                writer.WriteValueSafe(tris);
            }
        }

        public static implicit operator Mesh(SerializableMesh mesh) {
            // create a new mesh and broadcast that
            Mesh tmesh = new();
            if (mesh.vertices.Length > 64000 || mesh.tris.Length > 64000)
                tmesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            tmesh.SetVertices(mesh.vertices);
            tmesh.SetColors(mesh.colors);
            tmesh.SetUVs(0,mesh.uvs);
            tmesh.SetTriangles(mesh.tris, 0);
            tmesh.RecalculateNormals();
            tmesh.RecalculateTangents();
            tmesh.RecalculateBounds();
            return tmesh;
        }

        public static implicit operator SerializableMesh(Mesh mesh){
            SerializableMesh smesh = new()
            {
                vertices = mesh.vertices,
                colors = mesh.colors32,
                uvs = mesh.uv,
                tris = mesh.triangles
            };
            return smesh;
        }
    
        public bool Equals(SerializableMesh other)
        {
            return vertices.Length == other.vertices.Length;
        }

        public bool IsMesh { get { return vertices != null && vertices.Length > 0; } }
    }
}
