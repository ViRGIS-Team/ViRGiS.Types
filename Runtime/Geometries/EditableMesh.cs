﻿/* MIT License

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
using gs;
using Virgis;
using System.Collections.Generic;
using System;
using System.Linq;

public class EditableMesh : DataMesh
{   
    private bool m_BlockMove = false; // is entity in a block-move state
    private DMesh3 m_oldDmesh; // holds the previous mesh during editing
    private int m_selectedVertex;
    private GameObject m_sphere;

    private Vector3 m_currentHit; // current hit vertex
    private int? m_currentHitTri; // current hit triangle
    private int n = 0;
    private List<Int32> m_nRing;
    private bool m_selectOn = false;

    public override void Selected(SelectionType button) {

        if (m_selectOn)
            UnSelected(SelectionType.SELECT);
        m_selectOn = true;
        m_oldDmesh = new DMesh3(m_mesh);
        transform.parent.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
        if (button == SelectionType.SELECTALL) {
            m_BlockMove = true;
        } else {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            m_currentHitTri = null;
            m_currentHit = transform.InverseTransformPoint(State.instance.lastHitPosition);
            m_aabb.Build();
            m_currentHitTri = m_aabb.FindNearestTriangle(m_currentHit);
            Vector3d V0 = new Vector3d();
            Vector3d V1 = new Vector3d();
            Vector3d V2 = new Vector3d();
            m_mesh.GetTriVertices(m_currentHitTri.Value, ref V0, ref V1, ref V2);
            Index3i tri = m_mesh.GetTriangle(m_currentHitTri.Value);
            Vector3d currentBari = MathUtil.BarycentricCoords(m_currentHit, V0, V1, V2);
            if (currentBari.x > currentBari.y && currentBari.x > currentBari.z)
                m_selectedVertex = tri.a;
            if (currentBari.y > currentBari.x && currentBari.y > currentBari.z)
                m_selectedVertex = tri.b;
            if (currentBari.z > currentBari.y && currentBari.z > currentBari.x)
                m_selectedVertex = tri.c;
            m_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_sphere.transform.position = transform.TransformPoint(mesh.vertices[m_selectedVertex]);
            m_sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            m_sphere.transform.parent = transform;
        }
    }

    public new void UnSelected(SelectionType button) {
        m_selectOn = false;
        m_selectedVertex = 0;
        transform.parent.SendMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
        m_BlockMove = false;
        Destroy(m_sphere);
        m_aabb = new DMeshAABBTree3(m_mesh, true);
        n = -1;
    }

    public override void MoveTo(MoveArgs args) {
        if (m_BlockMove) {
            if (args.translate != Vector3.zero) {
                transform.Translate(args.translate, Space.World);
                transform.parent.SendMessage("Translate", args);
            }
        } else {
            if (args.translate != Vector3.zero && m_selectOn) {
                MeshFilter mf = GetComponent<MeshFilter>();
                Mesh mesh = mf.sharedMesh;
                Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                Vector3d target = mesh.vertices[m_selectedVertex] + localTranslate;
                if (State.instance.editScale > 2) {
                    if (n != State.instance.editScale) {
                        n = State.instance.editScale;
                        //
                        // first we need to find the n-ring of vertices
                        //
                        // first get 1-ring
                        m_nRing = new List<int>();
                        List<Int32> inside = new List<int>();
                        m_nRing.Add(m_selectedVertex);
                        for (int i = 0; i < n; i++) {
                            int[] working = m_nRing.ToArray();
                            m_nRing.Clear();
                            foreach (int v in working) {
                                if (!inside.Contains(v))
                                    foreach (int vring in m_mesh.VtxVerticesItr(v)) {
                                        if (!inside.Contains(vring))
                                            m_nRing.Add(vring);
                                    }
                                inside.Add(v);
                            }
                        }
                    }
                    //
                    // create the deformer
                    // set the constraint that the selected vertex is moved to position
                    // set the contraint that the n-ring remains stationary
                    //
                    LaplacianMeshDeformer deform = new LaplacianMeshDeformer(m_mesh);
                    deform.SetConstraint(m_selectedVertex, target, 1, true);
                    foreach (int v in m_nRing) {
                        deform.SetConstraint(v, m_mesh.GetVertex(v), 10, false);
                    }
                    deform.SolveAndUpdateMesh();
                } else {
                    m_mesh.SetVertex(m_selectedVertex, target);
                }
                //
                // reset the Unity mesh
                // 
                if (m_sphere != null)
                    m_sphere.transform.localPosition = (Vector3) m_mesh.GetVertex(m_selectedVertex);
                List<Vector3> vtxs = new List<Vector3>();
                foreach (int v in m_mesh.VertexIndices())
                    vtxs.Add((Vector3)m_mesh.GetVertex(v));
                mesh.vertices = vtxs.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
        }
    }

    /// <summary>
    /// This is called by the parent to action the move
    /// </summary>
    /// <param name="args"></param>
    /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
    public void MoveAxisAction(MoveArgs args) {
        if (GetComponent<MeshFilter>().sharedMesh.bounds.Contains(transform.InverseTransformPoint(args.pos)))
        {
            if (args.translate != Vector3.zero)
                transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            transform.RotateAround(args.pos, axis, angle);
            Vector3 A = transform.localPosition;
            Vector3 B = transform.parent.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue) {
                transform.localScale = transform.localScale * RS;
                transform.localPosition = FP;
            }
        }
    } 

    /// <summary>
    /// Draws the mesh represented by dmeshin - which should be in local space coordinates
    /// i.e Map Space coordinates without CRs or projection but that could be affected by 
    /// layer level scaling
    /// </summary>
    /// <param name="dmeshin"></param>
    /// <param name="mat">Material to be used for mesh normally</param>
    /// <param name="Wf">Wirframe material to be used when the mesh is being edited</param>
    /// <returns></returns>
    public Transform Draw(DMesh3 dmeshin, UnitPrototype bodySymbology) {
        SerializableMaterialHash hash = new()
        {
            Name = "body",
            Color = bodySymbology.Color,
        };
        hash.properties = new SerializableProperty[]
        {
            new SerializableProperty()
            {
                Key = "_hasVertexColor",
                Value = dmeshin.HasVertexColors ? 1 : 0
            }
        };

        Spawn(transform.parent);
        SetMaterial(hash);
        umesh.Value = dmeshin;
        return transform;
    }

    public override Dictionary<string, object> GetInfo() {
        if (m_mesh != null)
            return m_mesh.FindMetadata("properties") as Dictionary<string, object>;
        else
            return transform.parent.GetComponent<IVirgisFeature>().GetInfo();
    }

    public override void SetInfo(Dictionary<string, object> meta) {
        throw new NotImplementedException();
    }

    public override void OnEdit(bool inSession) {
        if (inSession) {
            mr.material.SetInt("_Selected", 1);
        } else {
            mr.material.SetInt("_Selected", 0);
        }
    }

    public void MakeKinematic() {
        MeshCollider[] mcs = gameObject.GetComponents<MeshCollider>();
        mcs.ToList().ForEach(item => Destroy(item));
    }

    public override VirgisFeature AddVertex(Vector3 position) {
        Vector3d localPosition = (Vector3d) transform.InverseTransformPoint(position);
        int currentHitTri;
        m_aabb = new DMeshAABBTree3(m_mesh, true);
        currentHitTri = m_aabb.FindNearestTriangle(localPosition);
        m_aabb.Build();
        Vector3d V0 = new Vector3d();
        Vector3d V1 = new Vector3d();
        Vector3d V2 = new Vector3d();
        m_mesh.GetTriVertices(currentHitTri, ref V0, ref V1, ref V2);
        Index3i tri = m_mesh.GetTriangle(currentHitTri);
        Vector3d currentBari = MathUtil.BarycentricCoords(localPosition, V0, V1, V2);
        int edgeId = 0;
        if (currentBari.x > currentBari.y && currentBari.x > currentBari.z) 
            if (currentBari.y < currentBari.z) 
                edgeId = m_mesh.FindEdgeFromTri(tri.a, tri.c, currentHitTri);
            else
                edgeId = m_mesh.FindEdgeFromTri(tri.a, tri.b, currentHitTri);
        if (currentBari.y > currentBari.x && currentBari.y > currentBari.z)
            if (currentBari.x < currentBari.z)
                edgeId = m_mesh.FindEdgeFromTri(tri.b, tri.c, currentHitTri);
            else
                edgeId = m_mesh.FindEdgeFromTri(tri.b, tri.a, currentHitTri);
        if (currentBari.z > currentBari.y && currentBari.z > currentBari.x)
            if (currentBari.y < currentBari.x)
                edgeId = m_mesh.FindEdgeFromTri(tri.c, tri.a, currentHitTri);
            else
                edgeId = m_mesh.FindEdgeFromTri(tri.c, tri.b, currentHitTri);
        DMesh3.EdgeSplitInfo result = new DMesh3.EdgeSplitInfo();
        m_mesh.SplitEdge(edgeId, out result);
        m_mesh.SetVertex(result.vNew, localPosition);
        Mesh tempMesh = (Mesh)m_mesh;
        tempMesh.RecalculateBounds();
        tempMesh.RecalculateNormals();
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = tempMesh;
        UnSelected(SelectionType.SELECT);
        return this;
    }

    /// <summary>
    /// This is called when you want to delete the currently selected vertex
    /// </summary>
    public void Delete() {
        MeshFilter mf = GetComponent<MeshFilter>();
        m_mesh.RemoveVertex(m_selectedVertex);
        if (m_oldDmesh.IsClosed()) {
            MeshAutoRepair mr = new MeshAutoRepair(m_mesh);
            mr.Apply();
            m_mesh = mr.Mesh;
        } else {
            m_mesh.CompactInPlace();
        }
        Mesh tempMesh = (Mesh)m_mesh;
        tempMesh.RecalculateBounds();
        tempMesh.RecalculateNormals();
        mf.mesh = tempMesh;
        UnSelected(SelectionType.SELECT);
    }
}
