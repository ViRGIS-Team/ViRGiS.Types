using System;
using VirgisGeometry;
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
        private bool clockwise;
        private bool hasColors;
        private bool hasUVs;


        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader) {
                // De-Serialize the data being synchronized
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int vertexCount);
                if (vertexCount == 0) return;
                reader.ReadValueSafe(out int triCount);
                reader.ReadValueSafe(out clockwise);
                reader.ReadValueSafe(out hasColors);
                reader.ReadValueSafe(out hasUVs);
                vertices = new Vector3[vertexCount];
                reader.ReadValueSafe(out vertices);
                tris = new int[triCount];
                reader.ReadValueSafe(out tris);
                colors = new Color32[vertexCount];
                if (hasColors) reader.ReadValueSafe(out colors);
                uvs = new Vector2[vertexCount];
                if (hasUVs) reader.ReadValueSafe(out uvs);
            } else {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(vertices.Length);
                if (vertices.Length == 0) return;
                writer.WriteValueSafe(tris.Length);
                writer.WriteValueSafe(clockwise);
                writer.WriteValueSafe(hasColors);
                writer.WriteValueSafe(hasUVs);
                writer.WriteValueSafe(vertices);
                writer.WriteValueSafe(tris);
                if (hasColors) writer.WriteValueSafe(colors);
                if (hasUVs) writer.WriteValueSafe(uvs);
            }
        }

        public static implicit operator DMesh3(SerializableMesh mesh)
        {
            DMesh3 dmesh = new DMesh3(false, mesh.hasColors, mesh.hasUVs, false);
            dmesh.Clockwise = mesh.clockwise;
            for (int i=0; i<mesh.vertices.Length;i++)
            {
                NewVertexInfo info = new(mesh.vertices[i], (Color)mesh.colors[i], mesh.uvs[i]);
                dmesh.AppendVertex(info);
            }

            int[] tris = mesh.tris;
            for (int i = 0; i < tris.Length; i += 3)
            {
                dmesh.AppendTriangle(tris[i], tris[i + 1], tris[i + 2]);
            }
            return dmesh;
        }

        public static implicit operator SerializableMesh(DMesh3 mesh)
        {
            SerializableMesh smesh = new();
            smesh.clockwise = mesh.Clockwise;
            smesh.vertices = new Vector3[mesh.VertexCount];
            smesh.colors = new Color32[mesh.VertexCount];
            smesh.uvs = new Vector2[mesh.VertexCount];
            smesh.hasColors = mesh.HasVertexColors;
            smesh.hasUVs = mesh.HasVertexUVs;
            NewVertexInfo data;
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                if (mesh.IsVertex(i))
                {
                    data = mesh.GetVertexAll(i);
                    smesh.vertices[i] = (Vector3)data.v;
                    if (data.bHaveC)
                        smesh.colors[i] = (Color)data.c;
                    if (data.bHaveUV)
                        smesh.uvs[i] = (Vector2)data.uv;
                }
            }
            smesh.tris = new int[mesh.TriangleCount * 3];
            int j = 0;
            foreach (Index3i tri in mesh.Triangles())
            {
                smesh.tris[j * 3] = tri.a;
                smesh.tris[j * 3 + 1] = tri.b;
                smesh.tris[j * 3 + 2] = tri.c;
                j++;
            }
            return smesh;
        }
    
        public bool Equals(SerializableMesh other)
        {
            return vertices.Length == other.vertices.Length;
        }

        public bool IsMesh { get { return vertices != null && vertices.Length > 0; } }
    }
}
