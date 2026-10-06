using VirgisGeometry;
using Unity.Netcode;
using UnityEngine;
using Draco;
using Draco.Encoder;
using System;
using Unity.Collections;


namespace Virgis
{
    public class SerializableMesh : NetworkVariableBase
    {
        private DMesh3 _mDmesh;
        private Mesh _mMesh;

        private byte[] _mData;

        public DSubmesh3 SubMesh;
        public bool KeepDmeshUpdatedOnClient = false;

        private long _updateNumber = 0;

        /// <summary>
        /// Delegate type for Mesh changed event
        /// </summary>
        /// <param name="newMesh">The new Unity mesh</param>
        public delegate void OnMeshChangedDelegate(Mesh newMesh);
        
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnMeshChangedDelegate OnMeshChanged;

        public DMesh3 DMesh3
        {
            get => _mDmesh;
            set {
                _mDmesh = value;
                RefreshUnityMesh();
                SetDirty(true);
                _updateNumber++;
            }
        }

        public void RefreshUnityMesh()
        {
            Debug.Log("Unity Mesh Refreshed");
            _mMesh = (Mesh)_mDmesh;
        }

        public Mesh Mesh { get { return _mMesh; } }

        public void MeshFinalize()
        {
            Debug.Log("Mesh Finalized");
            OnMeshChanged.Invoke(_mMesh);
            EncodeResult[] serResult = DracoEncoder.EncodeMesh(_mMesh, Vector3.one, 0.01f);
            _mData = serResult[0].data.ToArray();
            Array.ForEach(serResult, res => res.Dispose());
            SetDirty(true);
        }

        private async void MeshDeserialize()
        {
            Debug.Log("Mesh Deserialized");
            DracoMeshLoader decoder = new(false);
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData mesh = meshDataArray[0];
            DracoMeshLoader.DecodeResult result = await decoder.ConvertDracoMeshToUnity(mesh, _mData, true, true);
            if (!result.success)
            {
                throw new Exception("Mesh Deserialization failed");
            }
            _mMesh = new Mesh();
            _mMesh.MarkDynamic();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mMesh, DracoMeshLoader.defaultMeshUpdateFlags);
            if (result.calculateNormals)
            {
                _mMesh.RecalculateNormals();
            }
            _mMesh.RecalculateTangents();
            OnMeshChanged.Invoke(_mMesh);
            if (KeepDmeshUpdatedOnClient) UpdateDMesh();
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            Debug.Log("Read Delta");
            WriteField(writer);
        }

        public override void WriteField(FastBufferWriter writer)
        {
            Debug.Log("Write Mesh");
            if (_mData == null)
            {
                writer.WriteValueSafe(0);
            } else 
            {
                writer.WriteValueSafe(_mData.Length);
                writer.WriteValueSafe(_mData);
            }
        }

        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            Debug.Log("Read Mesh Mesh");
            reader.ReadValueSafe(out int size);
            if (size != 0) 
            {
                _mData = new byte[size];
                reader.ReadValueSafe(out _mData);
                MeshDeserialize();
            }
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            Debug.Log("Write Delta");
            ReadField(reader);
            if (keepDirtyDelta) return;
            ResetDirty();
        }

        public bool IsMesh { get { return _mMesh; } }

        protected void UpdateDMesh()
        {
            if (_mDmesh == null) _mDmesh = new();
            using (Mesh.MeshDataArray mda = Mesh.AcquireReadOnlyMeshData(_mMesh))
            {
                if (mda.Length > 1) throw new Exception("Too many submeshes");
                Mesh.MeshData md = mda[0];
                int buffers = md.vertexBufferCount;
                int position = md.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.Position);
                int pos_buf = md.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.Position);
                int color = md.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.Color);
                int col_buf = md.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.Color);
                int normal = md.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.Normal);
                int nor_buf = md.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.Normal);
                int uv1 = md.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.TexCoord0);
                int uv1_buf = md.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.TexCoord0);
                int uv2 = md.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.TexCoord1);
                int uv2_buf = md.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.TexCoord1);
                int uv3 = md.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.TexCoord2);
                int uv3_buf = md.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.TexCoord2);
                int uv4 = md.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.TexCoord3);
                int uv4_buf = md.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.TexCoord3);
                int uv = uv4;
                int uv_buf = uv4_buf;

                // find the uv layer that holds the dmesh vertex ids
                if (uv4 == -1)
                {
                    uv = uv3;
                    uv_buf = uv3_buf;
                    if (uv3 == -1)
                    {
                        uv = uv2;
                        uv_buf = uv2_buf;
                        if (uv2 == -1)
                        {
                            uv = uv1;
                            uv_buf = uv1_buf;
                        }
                    }
                }

                //Get Vertices and Update Dmesh
                NativeArray<float>[] vertexBuffers = new NativeArray<float>[buffers];
                int[] strides = new int[buffers];
                for (int i = 0; i < buffers; i++)
                {
                    vertexBuffers[i] = mda[0].GetVertexData<float>(i);
                    strides[i] = md.GetVertexBufferStride(i) / 4;
                }

                int pointer;
                _mDmesh.BeginUnsafeVerticesInsert();
                for (int i = 0; i < md.vertexCount; i++)
                {
                    NewVertexInfo vertex = new();
                    pointer = i * strides[pos_buf];
                    position /= 4;
                    vertex.v = new Vector3d(
                        vertexBuffers[pos_buf][pointer + position],
                        vertexBuffers[pos_buf][pointer + position + 1],
                        vertexBuffers[pos_buf][pointer + position + 2]
                        );
                    if (color != -1)
                    {
                        color /= 4;
                        pointer = i * strides[col_buf];
                        vertex.c = new Vector3f(
                            vertexBuffers[col_buf][pointer + color],
                            vertexBuffers[col_buf][pointer + color + 1],
                            vertexBuffers[col_buf][pointer + color + 2]
                            );
                        vertex.bHaveC = true;
                    };
                    if (normal != 0)
                    {
                        normal /= 4;
                        pointer = i * strides[nor_buf];
                        vertex.n = new Vector3f(
                            vertexBuffers[nor_buf][pointer + normal],
                            vertexBuffers[nor_buf][pointer + normal + 1],
                            vertexBuffers[nor_buf][pointer + normal + 2]
                            );
                        vertex.bHaveN = true;
                    }
                    if (uv1 != -1)
                    {
                        uv1 /= 4;
                        pointer = i * strides[uv1_buf];
                        vertex.uv = new Vector2f(
                            vertexBuffers[uv1_buf][pointer + uv1],
                            vertexBuffers[uv1_buf][pointer + uv1 + 1]
                            );
                        vertex.bHaveUV = true;
                    }
                    pointer = i * strides[uv_buf];
                    int vID = (int)vertexBuffers[uv_buf] [pointer + uv / 4 + 1];
                    if (_mDmesh.IsVertex(vID))
                    {
                        if (!_mDmesh.SetVertex(vID, vertex, true, true, true)) throw new Exception("DMesh SetVertex Failed");
                    } else
                    {
                        MeshResult mr = _mDmesh.InsertVertex(vID, ref vertex, true);
                        if (mr != MeshResult.Ok) throw new Exception($"DMesh InsertVertex Failed with : {mr.ToString()}");
                    }
                };
                _mDmesh.EndUnsafeVerticesInsert();

                // Get Triangles and update DMesh
                NativeArray<byte> triangles = mda[0].GetIndexData<byte>();
                int indexStride = 4;
                if (md.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16)
                {
                    indexStride = 2;
                }
                int triangleCount = triangles.Length / (indexStride * 3);
                int[] trimap = new int[triangleCount];
                for (int i = 0; i < triangleCount; i++)
                {
                    pointer = i * indexStride * 3;
                    int a, b, c;
                    if (md.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16)
                    {
                        a = BitConverter.ToInt16(triangles.GetSubArray(pointer, 2).AsReadOnlySpan());
                        b = BitConverter.ToInt16(triangles.GetSubArray(pointer + 2, 2).AsReadOnlySpan());
                        c = BitConverter.ToInt16(triangles.GetSubArray(pointer + 4, 2).AsReadOnlySpan());
                    } else
                    {
                        a = BitConverter.ToInt32(triangles.GetSubArray(pointer, 4).AsReadOnlySpan());
                        b = BitConverter.ToInt32(triangles.GetSubArray(pointer + 4, 4).AsReadOnlySpan());
                        c = BitConverter.ToInt32(triangles.GetSubArray(pointer + 8, 4).AsReadOnlySpan());
                    }
                    int tID = _mDmesh.FindTriangle(a, b, c);
                    if (tID == DMesh3.InvalidID)
                    {
                        tID = _mDmesh.AppendTriangle(a, b, c);
                    }
                    trimap[i] = tID;
                }
                foreach(int tri in _mDmesh.TriangleIndices())
                {
                    if (Array.Find(trimap, item => item == tri) == default)
                    {
                        _mDmesh.RemoveTriangle(tri, true, true);
                    }
                }
            }
        }
    }
}
