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
using VirgisGeometry;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Virgis {

    public class DataMesh : VirgisFeature{

        public SerializableMesh umesh = new();
        public MeshFilter MeshFilter;
        public MeshCollider[] MeshColliders;

        protected int[] m_VertexMap; // holds the map between the DMesh vertex ids and the Unity Mesh vertex ids


        public override void OnNetworkSpawn(){
            base.OnNetworkSpawn();
            umesh.OnMeshChanged += SetMesh;
            if (umesh.IsMesh) SetMesh (umesh.Mesh);
        }

        public override void OnNetworkDespawn(){
            base.OnNetworkSpawn();
            umesh.OnMeshChanged -= SetMesh;
        }

        private void SetMesh(Mesh newValue){

            // load mesh as unity mesh and add to MeshFilter
            if (!NetworkManager.IsServer)
            {
                Vector2[] uv = newValue.uv2;
                for (int i = 0; i < uv.Length; i++)
                {
                    uv[i].x = Mathf.Round(uv[i].x);
                    uv[i].y = Mathf.Round(uv[i].y);
                }
                newValue.uv4 = uv;
            }
            newValue.RecalculateBounds();
            MeshFilter.mesh = newValue;
            UpdateUnityMesh();
        }

        /// <summary>
        /// Helper to create a UV from the Colors - the v component is set to the vertex ID since this gets scrambled through draco
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        public static Vector2[] ToUV(byte[] colors)
        {
            Vector2[] uv = new Vector2[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                uv[i] = new Vector2(colors[i], i);
            };
            return uv;
        }

        protected void UpdateUnityMesh() {
            Mesh mesh = MeshFilter.sharedMesh;

            // create a map between the Unity Mesh vertices and the DMesh vertices on the server using UV4
            Vector2[] uvs = mesh.uv4;
            m_VertexMap = new int[uvs.Length];
            for (int i = 0; i < uvs.Length; i++)
            {
                m_VertexMap[(int)uvs[i].y] = i;
            }

            // create the mesh colliders
            Mesh imesh = new()
            {
                indexFormat = mesh.indexFormat,
                vertices = mesh.vertices,
                triangles = mesh.triangles.Reverse().ToArray(),
                uv = mesh.uv,
                uv4 = mesh.uv4,
            };

            imesh.RecalculateBounds();
            imesh.RecalculateNormals();

            try
            {
                MeshColliders[0].sharedMesh = mesh;
                MeshColliders[1].sharedMesh = imesh;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        public DMesh3 GetMesh() {
            return umesh.DMesh3;
        }

        public void MakeConvex() {
            MeshColliders.ToList().ForEach(item => item.convex = true);
        }

        public void MakeKinematic(){
            MeshColliders.ToList().ForEach(item => Destroy(item));
        }
    }
}
