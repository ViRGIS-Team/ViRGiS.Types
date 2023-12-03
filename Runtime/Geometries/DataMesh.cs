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
using g3;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.Netcode;

namespace Virgis {

    public class DataMesh : VirgisFeature
    {
        protected DMesh3 m_mesh;
        protected DMeshAABBTree3 m_aabb; // AABB Tree for current mesh

        public NetworkVariable<SerializableMesh> umesh = new();
        public NetworkVariable<SerializableColorArray> colorArray = new();


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            umesh.OnValueChanged += SetMesh;
            if (umesh.Value != null && umesh.Value.IsMesh) SetMesh(new SerializableMesh(), umesh.Value);
            colorArray.OnValueChanged += OnColorisation;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkSpawn();
            umesh.OnValueChanged -= SetMesh;
            colorArray.OnValueChanged -= OnColorisation;
        }

        public void OnColorisation(SerializableColorArray previousValue, SerializableColorArray newValue)
        {
            if (newValue.colors == null) return;
            Vector2[] uv = new Vector2[newValue.colors.Length];
            for (int i = 0; i < newValue.colors.Length; i++)
            {
                uv[i] = new Vector2(newValue.colors[i], 0);
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
                StartCoroutine(m_mesh.ColorisationCoroutine((colors) =>
                {
                    colorArray.Value = new SerializableColorArray() { colors = colors };
                }
                ));
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
