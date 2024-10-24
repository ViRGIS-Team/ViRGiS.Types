using System;
using VirgisGeometry;
using Unity.Netcode;
using UnityEngine;

namespace Virgis
{
    public class SerializableMesh : INetworkSerializable, IEquatable<SerializableMesh>
    {
        private DMesh3 dmesh;


        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader) {
                // De-Serialize the data being synchronized
                Debug.Log("Deserialize Mesh");
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int vertexCount);
                if (vertexCount == 0) return;
                reader.ReadValueSafe(out int triCount);
                reader.ReadValueSafe(out bool clockwise);
                reader.ReadValueSafe(out bool hasColors);
                reader.ReadValueSafe(out bool hasUVs);
                reader.ReadValueSafe(out bool hasNormals);

                AxisOrder axisOrder = new();
                ByteUnpacker.ReadValuePacked<AxisType>(reader, out axisOrder.Axis1);
                ByteUnpacker.ReadValuePacked<AxisType>(reader, out axisOrder.Axis2);
                ByteUnpacker.ReadValuePacked<AxisType>(reader, out axisOrder.Axis3);

                dmesh = new(hasNormals, hasColors, hasUVs);
                dmesh.axisOrder = axisOrder;
                dmesh.Clockwise = clockwise;

                for (int i = 0; i< vertexCount; i++)
                {
                    reader.ReadValueSafe(out double x);
                    reader.ReadValueSafe(out double y);
                    reader.ReadValueSafe(out double z);
                    NewVertexInfo vi = new(new(x, y, z));
                    if (hasColors )
                    {
                        reader.ReadValueSafe(out float r);
                        reader.ReadValueSafe(out float g);
                        reader.ReadValueSafe(out float b);
                        vi.c = new Vector3f(r, g, b);
                        vi.bHaveC = true;
                    }
                    if (hasUVs )
                    {
                        reader.ReadValueSafe(out float u);
                        reader.ReadValueSafe(out float v);
                        vi.uv = new(u,v);
                        vi.bHaveUV = true;
                    }
                    if (hasNormals)
                    {
                        reader.ReadValueSafe(out float nx);
                        reader.ReadValueSafe(out float ny);
                        reader.ReadValueSafe(out float nz);
                        vi.n = new(nx,ny,nz);
                        vi.bHaveN = true;
                    };
                    dmesh.AppendVertex(vi);
                };
                
                for(int i = 0; i < triCount; i++)
                {
                    reader.ReadValueSafe(out int v0);
                    reader.ReadValueSafe(out int v1);
                    reader.ReadValueSafe(out int v2);
                    dmesh.AppendTriangle(v0,v1,v2);
                }
            } else {
                Debug.Log("Serialize Mesh");
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(dmesh.VertexCount);
                if (dmesh.VertexCount == 0) return;
                writer.WriteValueSafe(dmesh.TriangleCount);
                writer.WriteValueSafe(dmesh.Clockwise);
                writer.WriteValueSafe(dmesh.HasVertexColors);
                writer.WriteValueSafe(dmesh.HasVertexUVs);
                writer.WriteValueSafe(dmesh.HasVertexNormals);

                BytePacker.WriteValuePacked<AxisType>(writer, dmesh.axisOrder.Axis1);
                BytePacker.WriteValuePacked<AxisType>(writer, dmesh.axisOrder.Axis2);
                BytePacker.WriteValuePacked<AxisType>(writer, dmesh.axisOrder.Axis3);

                foreach (NewVertexInfo vi in dmesh.VerticesAll())
                {
                    writer.WriteValueSafe(vi.v.x);
                    writer.WriteValueSafe(vi.v.y);
                    writer.WriteValueSafe(vi.v.z);
                    if (dmesh.HasVertexColors)
                    {
                        writer.WriteValueSafe(vi.c.x);
                        writer.WriteValueSafe(vi.c.y);
                        writer.WriteValueSafe(vi.c.z);
                    };
                    if (dmesh.HasVertexUVs) 
                    {
                        writer.WriteValueSafe(vi.uv.x);
                        writer.WriteValueSafe(vi.uv.y);
                    };
                    if (dmesh.HasVertexNormals)
                    {
                        writer.WriteValueSafe(vi.n.x);
                        writer.WriteValueSafe(vi.n.y);
                        writer.WriteValueSafe(vi.n.z);
                    }
                };
                foreach( Index3i tri in dmesh.Triangles())
                {
                    writer.WriteValueSafe(tri.a);
                    writer.WriteValueSafe(tri.b);
                    writer.WriteValueSafe(tri.c);
                }
            }
        }

        public static implicit operator DMesh3(SerializableMesh mesh)
        {
            return mesh.dmesh;
        }

        public static implicit operator SerializableMesh(DMesh3 mesh)
        {
            return new() {dmesh = mesh };
        }
    
        public bool Equals(SerializableMesh other)
        {
            return dmesh.IsSameMesh(other.dmesh, false, true, true, true, false);
        }

        public bool IsMesh { get { return dmesh != null; } }
    }
}
