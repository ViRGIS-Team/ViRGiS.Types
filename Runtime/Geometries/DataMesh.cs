/* MIT License

Copyright (c) 2020 - 21 Runette Software

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
using Virgis;
using System.Collections.Generic;
using System;
using System.Linq;

public class DataMesh : VirgisFeature
{
    private DMesh3 m_mesh;

    public SerializeableMesh umesh = new();

    public void Awake()
    {
        umesh.OnValueChanged += SetMesh;
    }

    public void Start()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.material = GetLayer().GetMaterial(4);
    }

    private void SetMesh(Mesh nextMesh)
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider[] mc = GetComponents<MeshCollider>();
        mf.mesh = null;
        Mesh mesh = nextMesh;
        Mesh imesh = new()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,

            vertices = mesh.vertices,
            triangles = mesh.triangles.Reverse<int>().ToArray(),
            uv = mesh.uv
        };

        imesh.RecalculateBounds();
        imesh.RecalculateNormals();

        mf.mesh = mesh;
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
