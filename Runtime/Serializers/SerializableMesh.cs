using System;
using VirgisGeometry;
using Unity.Netcode;
using UnityEngine;
using Unity.Mathematics;

namespace Virgis
{
    public class SerializableMesh : INetworkSerializable, IEquatable<SerializableMesh>
    {
        private double3[] vertices;
        private double3[] normals;
        private Color32[] colors;
        private double2[] uvs;
        private int3[] tris;
        private bool clockwise;
        private bool hasColors;
        private bool hasUVs;
        private bool hasNormals;
        private AxisOrder axisOrder;


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
                reader.ReadValueSafe(out hasNormals);
                vertices = new double3[vertexCount];
                reader.ReadValueSafe(out vertices);
                tris = new int3[triCount];
                reader.ReadValueSafe(out tris);
                normals = new double3[vertexCount];
                if (hasNormals) reader.ReadValueSafe(out normals);
                colors = new Color32[vertexCount];
                if (hasColors) reader.ReadValueSafe(out colors);
                uvs = new double2[vertexCount];
                if (hasUVs) reader.ReadValueSafe(out uvs);
                axisOrder = new AxisOrder();
                byte val;
                reader.ReadValueSafe(out val);
                axisOrder.Axis1 = (AxisType)Enum.ToObject(typeof(AxisType), (int)val);
                reader.ReadValueSafe(out val);
                axisOrder.Axis2 = (AxisType)Enum.ToObject(typeof(AxisType), (int)val);
                reader.ReadValueSafe(out val);
                axisOrder.Axis3 = (AxisType)Enum.ToObject(typeof(AxisType), (int)val);
            } else {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(vertices.Length);
                if (vertices.Length == 0) return;
                writer.WriteValueSafe(tris.Length);
                writer.WriteValueSafe(clockwise);
                writer.WriteValueSafe(hasColors);
                writer.WriteValueSafe(hasUVs);
                writer.WriteValueSafe(hasNormals);
                writer.WriteValueSafe(vertices);
                writer.WriteValueSafe(tris);
                if (hasNormals) writer.WriteValueSafe(normals);
                if (hasColors) writer.WriteValueSafe(colors);
                if (hasUVs) writer.WriteValueSafe(uvs);
                writer.WriteValueSafe(axisOrder.ToArray());
            }
        }

        public static implicit operator DMesh3(SerializableMesh mesh)
        {

            DMesh3 dmesh = new DMesh3(mesh.hasNormals, mesh.hasColors, mesh.hasUVs, false);
            dmesh.Clockwise = mesh.clockwise;
            dmesh.axisOrder = mesh.axisOrder;
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                NewVertexInfo info = new(mesh.vertices[i]);
                if (mesh.normals != null)
                {
                    info.n = (Vector3f)mesh.normals[i];
                    info.bHaveN = true;
                }
                if (mesh.colors != null )
                {
                    info.c = (Color)mesh.colors[i];
                    info.bHaveC = true;
                };
                if (mesh.uvs != null)
                {
                    info.uv = (Vector2f)mesh.uvs[i];
                    info.bHaveUV = true;
                }
                dmesh.AppendVertex(info);
            }

            int3[] tris = mesh.tris;
            for (int i = 0; i < tris.Length; i++)
            {
                dmesh.AppendTriangle(tris[i]);
            }
            return dmesh;
        }

        public static implicit operator SerializableMesh(DMesh3 mesh)
        {
            SerializableMesh smesh = new();
            smesh.clockwise = mesh.Clockwise;
            smesh.vertices = new double3[mesh.VertexCount];
            smesh.colors = new Color32[mesh.VertexCount];
            smesh.uvs = new double2[mesh.VertexCount];
            smesh.hasColors = mesh.HasVertexColors;
            smesh.hasNormals = mesh.HasVertexNormals;
            smesh.hasUVs = mesh.HasVertexUVs;
            smesh.axisOrder = mesh.axisOrder;
            NewVertexInfo data;
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                if (mesh.IsVertex(i))
                {
                    data = mesh.GetVertexAll(i);
                    smesh.vertices[i] = data.v;
                    if (data.bHaveC)
                        smesh.colors[i] = (Color)data.c;
                    if (data.bHaveUV)
                        smesh.uvs[i] = data.uv;
                    if (data.bHaveN)
                        smesh.normals[i] = data.n;
                }
            }
            smesh.tris = new int3[mesh.TriangleCount];
            for (int i = 0; i < mesh.TriangleCount; i++)
            {
                smesh.tris[i] = mesh.GetTriangle(i);
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
