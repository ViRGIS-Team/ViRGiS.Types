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
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Virgis {

    public class DataMesh : VirgisFeature{

        public readonly SerializableMesh SerialMesh = new();
        [FormerlySerializedAs("MeshFilter")] public MeshFilter meshFilter;
        [FormerlySerializedAs("MeshColliders")] public MeshCollider[] meshColliders;

        protected Dictionary<int, int> MVertexMap; // holds the map between the DMesh vertex ids and the Unity Mesh vertex ids


        public override void OnNetworkSpawn(){
            base.OnNetworkSpawn();
            SerialMesh.OnMeshChanged += SetMesh;
            if (SerialMesh.IsMesh) SetMesh (SerialMesh.Mesh);
        }

        public override void OnNetworkDespawn(){
            base.OnNetworkSpawn();
            SerialMesh.OnMeshChanged -= SetMesh;
        }
        
        /// <summary>
        /// Called when the SerializeableMesh is updated
        /// </summary>
        /// <param name="newValue"></param>
        private void SetMesh(Mesh newValue){

            // load mesh as unity mesh and add to MeshFilter

            if (IsListening && ! IsServer)
            {
                Vector2[] uv = newValue.uv2;
                MVertexMap = new();
                for (int i = 0; i < uv.Length; i++)
                {
                    uv[i].x = Mathf.Round(uv[i].x);
                    uv[i].y = Mathf.Round(uv[i].y);
                    try
                    {
                        MVertexMap.Add((int)uv[i].y, i);
                    }
                    catch (Exception e)
                    {
                        _ = e;
                        Debug.Log("Duplicate Vertex in VertexMap");
                    } 
                }
                newValue.uv4 = uv;
            } else
            {
                Vector2[] uv = newValue.uv4;
                MVertexMap = new ();
                for (int i = 0; i < uv.Length; i++)
                {
                    MVertexMap.Add((int)uv[i].y,i);
                }
            }
            newValue.RecalculateBounds();
            meshFilter.mesh = newValue;
            UpdateUnityMesh();
        }

        /// <summary>
        /// Helper to create a UV from the Colors - the v component is set to the vertex ID since this gets scrambled through draco
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="map"> Vertex ID map</param>
        /// <returns></returns>
        protected static Vector2[] ToUV(byte[] colors, int[] map)
        {
            Vector2[] uv = new Vector2[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                uv[i] = new Vector2(colors[i], map[i]);
            }
            return uv;
        }

        protected void UpdateUnityMesh() {
            Mesh mesh = meshFilter.sharedMesh;
            mesh.UploadMeshData(false);

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
                meshColliders[0].sharedMesh = mesh;
                meshColliders[0].sharedMesh.UploadMeshData(false);
                meshColliders[1].sharedMesh = imesh;
                meshColliders[0].sharedMesh.UploadMeshData(false);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        public DMesh3 GetMesh() {
            return SerialMesh.DMesh3;
        }

        public void MakeConvex() {
            meshColliders.ToList().ForEach(item => item.convex = true);
        }

        public void MakeKinematic(){
            meshColliders.ToList().ForEach(Destroy);
        }
    }
}
