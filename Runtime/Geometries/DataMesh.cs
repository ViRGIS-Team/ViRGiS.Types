/* MIT License

Copyright (c) 2020 - 23 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using UnityEngine;
using UnityEngine.Rendering;
using g3;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Mathematics;
using Unity.Collections;

namespace Virgis {

    public class DataMesh : VirgisFeature
    {
        protected DMesh3 m_mesh;
        protected DMeshAABBTree3 m_aabb; // AABB Tree for current mesh

        public NetworkVariable<SerializableMesh> umesh = new();
        public NetworkVariable<SerializableColorArray> colorArray = new();

        [SerializeField]
        public ComputeShader colorShader;


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            umesh.OnValueChanged += SetMesh;
            if (umesh.Value != null && umesh.Value.IsMesh) SetMesh(new SerializableMesh(), umesh.Value);
            colorArray.OnValueChanged += OnColorisation;
            if (colorArray.Value.Colors != null) OnColorisation(new SerializableColorArray(), colorArray.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkSpawn();
            umesh.OnValueChanged -= SetMesh;
            colorArray.OnValueChanged -= OnColorisation;
        }

        public void OnColorisation(SerializableColorArray previousValue, SerializableColorArray newValue)
        {
            if (newValue.Colors == null) return;
            Vector2[] uv = new Vector2[newValue.Colors.Length];
            for (int i = 0; i < newValue.Colors.Length; i++)
            {
                uv[i] = new Vector2(newValue.Colors[i], 0);
            };
            MeshFilter mf = GetComponent<MeshFilter>();
            mf.sharedMesh.uv4 = uv;
        }


        private void SetMesh(SerializableMesh previousValue, SerializableMesh newValue)
        {
            if (newValue == previousValue || !newValue.IsMesh) return;
            MeshFilter mf = GetComponent<MeshFilter>();
            MeshCollider[] mc = GetComponents<MeshCollider>();

            // load mesh as dmesh and process
            m_mesh = newValue;
            m_aabb = new DMeshAABBTree3(m_mesh, true);

            // lead mesh as unity mesh and add to MeshFilter
            Mesh mesh = (Mesh)m_mesh;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mf.mesh = mesh;

            NetworkManager nm = GetComponent<NetworkObject>().NetworkManager;
            if (nm.IsServer)
            {
                //StartCoroutine(m_mesh.ColorisationCoroutine(20, (colors) =>
                //{
                //    colorArray.Value = new SerializableColorArray() { Colors = colors };
                //}
                //));

                StartCoroutine(TestCoroutine(m_mesh, (colors) =>
                {
                    colorArray.Value = new SerializableColorArray() { Colors = colors };
                }));
            };

            // create the mesh colliders
            Mesh imesh = new()
            {
                indexFormat = mesh.indexFormat,

                vertices = mesh.vertices,
                triangles = mesh.triangles.Reverse().ToArray(),
                uv = mesh.uv
            };

            imesh.RecalculateBounds();
            imesh.RecalculateNormals();

            try
            {
                mc[0].sharedMesh = mesh;
                mc[1].sharedMesh = imesh;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public IEnumerator TestCoroutine(DMesh3 mesh, Action<int[]> callback)
        {
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();
            Debug.Log("Start Coloristion");
            int triangleCount = mesh.TriangleCount;
            uint3[] Triangles_arr = new uint3[triangleCount];
            for (int i = 0; i < triangleCount; i++) {
                Triangles_arr[i] = (uint3)(int3)mesh.GetTriangle(i);
            }
            uint[] Colors_arr = new uint[mesh.VertexCount];
            uint[] Flag_arr = new uint[triangleCount];

            List<int> Degree = new();
            foreach (int vID in m_mesh.VertexIndices())
            {
                Degree.Add(m_mesh.GetVtxEdgeCount(vID));
            }

            for (uint i = 0; i < 6; i++)
            {
                int val = Degree.Max();
                int idx = Degree.IndexOf(val);
                Degree[idx] = 0;
                Colors_arr[idx] = i + 1;
            }

            colorShader.SetInt("TriangleCount", triangleCount);

            ComputeBuffer Triangles_buf = new ComputeBuffer(triangleCount, 12);
            Triangles_buf.SetData(Triangles_arr);

            ComputeBuffer Colors_buff = new ComputeBuffer(mesh.VertexCount, 4);
            Colors_buff.SetData(Colors_arr);

            ComputeBuffer Flag_buff = new ComputeBuffer(triangleCount, 4);

            int kernel = colorShader.FindKernel("CSMain");

            colorShader.SetBuffer(kernel, "Triangles", Triangles_buf);
            colorShader.SetBuffer(kernel, "Colors", Colors_buff);
            colorShader.SetBuffer(kernel, "completeFlag", Flag_buff);

            int threadgroup = (int)(triangleCount / 16) + 1 ;

            int itr = 0;
            Debug.Log($"start the GPU after {stopwatch.Elapsed.TotalSeconds}");

            while (true)
            {
                Flag_buff.SetData(Flag_arr);
                colorShader.Dispatch(kernel, threadgroup, 1, 1);
                AsyncGPUReadbackRequest req = AsyncGPUReadback.Request(Flag_buff);
                while (!req.done)
                {
                    yield return null;
                }

                if (req.hasError) throw new Exception();

                uint[] changearray = req.GetData<uint>().ToArray();
                long changes = changearray.Sum<uint>(x => (long)x);
                Debug.Log($"Changes ; {changes}");
                if (changes == 0) break;
                itr++;
            }

            AsyncGPUReadbackRequest req2 = AsyncGPUReadback.Request(Colors_buff);
            while (!req2.done)
            {
                yield return null;
            }

            if (req2.hasError) throw new Exception();
            NativeArray<int> colors = req2.GetData<int>();


            callback(colors.ToArray());
            stopwatch.Stop();
            Flag_buff.Dispose();
            Colors_buff.Dispose();
            Triangles_buf.Dispose();
            colors.Dispose();
            Debug.Log($"{triangleCount} triangles took {stopwatch.Elapsed.TotalSeconds}, {itr + 1} iterations");
        }
             
        public DMesh3 GetMesh() {
            return m_mesh;
        }

        public override Dictionary<string, object> GetInfo() {
            if (m_mesh != null)
                return m_mesh.FindMetadata("properties") as Dictionary<string, object>;
            else
                return transform.parent.GetComponent<IVirgisFeature>().GetInfo();
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new System.NotImplementedException();
        }

        public void MakeConvex() {
            MeshCollider[] mcs = gameObject.GetComponents<MeshCollider>();
            mcs.ToList().ForEach(item => item.convex = true);
        }
    }
}
