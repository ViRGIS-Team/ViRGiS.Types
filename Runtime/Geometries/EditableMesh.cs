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
using System.Diagnostics;
using Unity.Netcode;

namespace Virgis
{

    public class EditableMesh : DataMesh{
        public GameObject MarkerShape;

        private GameObject marker; // Marked used to show the selected Vertex

        private DMesh3 m_OldDMesh; // Saves the DMesh3 for recovery on Save and Discrad
        private bool m_Changed; // True if the Mesh has been changed in an edit session

        private bool m_BlockMove = false; // is entity in a block-move state

        private int m_selectedVertex; // holds the current DMesh vertex ID for the selected vertex - note that in clients this NOT the same as the DMesh vertex id
        private int m_selectedTriangle; // holds the current Unity Mesh triangle ID - note that in clients this is not the same as the DMesh ytriangle ID
        private int n = -1; //indicator that the n-ring is built
        private bool m_selectOn = false;

        public override void OnNetworkSpawn(){
            umesh.KeepDmeshUpdatedOnClient = true;
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn(){
            base.OnNetworkDespawn();
        }

        public override void Selected(SelectionType button){
            if (m_selectOn)
            {
                UnSelected(SelectionType.SELECT);
            }
            m_selectOn = true;
            RaycastHit lastHit = State.instance.lastHit;
            m_selectedTriangle = lastHit.triangleIndex;
            Vector3 bary = lastHit.barycentricCoordinate;
            int[] triangles = (lastHit.collider as MeshCollider).sharedMesh.triangles;
            int selectedUVertex = 0;
            if (bary.x >= bary.y && bary.x >= bary.z) selectedUVertex = triangles[m_selectedTriangle * 3]; 
            else if (bary.y >= bary.x && bary.y >= bary.z) selectedUVertex = triangles[m_selectedTriangle * 3 + 1];
            else if (bary.z >= bary.x && bary.z >= bary.y) selectedUVertex = triangles[m_selectedTriangle * 3 + 2];
            
            
            // get the collider mesh to get the verticesMesh and get the DMesh3 vertex id
            Mesh cmesh = (State.instance.lastHit.collider as MeshCollider).sharedMesh;
            Vector2 uv = cmesh.uv4[ selectedUVertex];
            m_selectedVertex = (int)uv.y;

            //send to server
            SelectedRpc(button, (int)uv.y);

            if (button == SelectionType.SELECTALL)
            {
                m_BlockMove = true;
            }

            //Move the selected marker to the vertex
            marker = Instantiate(MarkerShape);
            marker.transform.parent = transform;
            marker.transform.localPosition = cmesh.vertices[m_selectedVertex];
        }

        [Rpc(SendTo.Server)]
        public void SelectedRpc(SelectionType button, int hitPosition){
            m_selectOn = true;
            transform.parent.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
            m_selectedVertex = hitPosition;
            if (button == SelectionType.SELECTALL)
            {
                m_BlockMove = true;
            }
            else
            {

            }
        }

        public new void UnSelected(SelectionType button){
            m_selectOn = false;
            m_selectedVertex = 0;
            m_BlockMove = false;
            UnSelectedRpc(button);
            Destroy(marker);
            n = -1;
        }

        [Rpc(SendTo.Server)]
        public void UnSelectedRpc(SelectionType button){
            transform.parent.SendMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
            m_selectOn = false;
            m_BlockMove = false;
            n = -1;
        }

        public override void Changed()
        {
            base.Changed();
            m_Changed = true;
        }

        /// <summary>
        /// This is called by the Avatar on the client to move the current vertex
        /// </summary>
        /// <param name="args"></param>
        public override void MoveTo(MoveArgs args) {
            if (!m_selectOn) return;
            Changed();
            Stopwatch timer = new();
            timer.Start();
            if (m_BlockMove)
            {
                if (args.translate != Vector3.zero)
                {
                    transform.Translate(args.translate, Space.World);
                    transform.parent.SendMessage("Translate", args);
                }
            }
            else
            {
                //if (!umesh.DMesh3.CheckValidity(out MeshResult res1))
                //{
                //    UnityEngine.Debug.Log("Move Vertex - Move Vertex given a defective mesh " + res1.ToString());
                //}
                Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                if (marker != null) marker.transform.localPosition += localTranslate;
                if (args.translate != Vector3.zero && m_selectOn)
                {
                    Vector3d target;
                    if (umesh.DMesh3.IsVertex(m_selectedVertex))
                    {
                        target = umesh.DMesh3.GetVertex(m_selectedVertex) + localTranslate;
                    } else 
                    {
                        UnityEngine.Debug.Log("Selected Vertex is not a Vertex");
                        return;
                    }

                    // check the current submesh is still correct
                    if (n != (int)args.scale)
                    {
                        n = (int)args.scale;

                        //
                        // create an n-ring Sub Mesh
                        //
                        umesh.SubMesh = new(umesh.DMesh3);
                        umesh.SubMesh.Compute(m_selectedVertex, n);
                    }
                    //
                    // create the deformer
                    // set the constraint that the selected vertex is moved to position
                    // set the contraint that the n-ring remains stationary
                    //
                    LaplacianMeshDeformer deform = new LaplacianMeshDeformer(umesh.SubMesh);
                    deform.SetConstraint(m_selectedVertex, target, 1, true);
                    foreach (int v in MeshIterators.BoundaryVertices(umesh.SubMesh))
                    {
                        if (v == m_selectedVertex) continue;
                        if (umesh.SubMesh.IsSubmeshInternalBoundaryVertex(v))
                        {
                            deform.SetConstraint(v, umesh.SubMesh.GetVertex(v), 1, true);
                        } else
                        {
                            deform.SetConstraint(v, umesh.SubMesh.GetVertex(v), 3, false);
                        }
                    }
                    deform.SolveAndUpdateMesh();
                    Mesh sm = MeshFilter.sharedMesh;
                    Vector3[] vertices = sm.vertices;

                    foreach (int vert in umesh.SubMesh.VertexIndices())
                    {
                        int vID = m_VertexMap[vert];
                        vertices[vID] = (Vector3)umesh.SubMesh.GetVertex(vert);
                    }
                    sm.vertices = vertices;
                    sm.UploadMeshData(false);
                    if (!NetworkManager.Singleton.IsHost)
                    {
                        MoveToRpc(
                            umesh.SubMesh.VertexIndices().ToArray(),
                            umesh.SubMesh.VertexValues().ToArray(),
                            umesh.SubMesh.axisOrder.ToArray()
                            );
                    };
                }
            }
            UpdateUnityMesh();
            timer.Stop();
            //UnityEngine.Debug.LogWarning($"Edit took : {timer.Elapsed.TotalSeconds} seconds");
            //if (!umesh.DMesh3.CheckValidity(out MeshResult res2))
            //{
            //    UnityEngine.Debug.Log("Move Vertex - MOve Vertex created a defective mesh " + res2.ToString());
            //}
        }

        [Rpc(SendTo.Server)]
        public void MoveToRpc(int[] vIDs, double[] values, byte[] axisOrder)
        {
            m_Changed = true;
            for (int i = 0; i < vIDs.Length; i++)
            {
                int pointer = 0;
                Vector3d val = new Vector3d(values[pointer++], values[pointer++], values[pointer++]) { axisOrder = new AxisOrder(axisOrder) };
                umesh.DMesh3.SetVertex(vIDs[i], val);
            }
        }

        /// <summary>
        /// This is called by the parent to action the move
        /// </summary>
        /// <param name="args"></param>
        /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public void MoveAxisAction(MoveArgs args){
            //if (GetComponent<MeshFilter>().sharedMesh.bounds.Contains(transform.InverseTransformPoint(args.pos)))
            //{
                Changed();
                if (args.translate != Vector3.zero)
                    transform.Translate(args.translate, Space.World);
                args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
                transform.RotateAround(args.pos, axis, angle);
                Vector3 A = transform.localPosition;
                Vector3 B = transform.InverseTransformPoint(args.pos);
                Vector3 C = A - B;
                float RS = args.scale;
                Vector3 FP = B + C * RS;
                if (FP.magnitude < float.MaxValue)
                {
                    transform.localScale = transform.localScale * RS;
                    transform.localPosition = FP;
                }
            //}
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
        public Transform Draw(DMesh3 dmeshin, UnitPrototype bodySymbology){
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
            umesh.DMesh3 = dmeshin;
            StartCoroutine(umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
            {
                umesh.Mesh.uv4 = DataMesh.ToUV(colors, umesh.DMesh3.VertexMap);
                umesh.MeshFinalize();
            }
            ));
            return transform;
        }


        public override void OnEdit(bool inSession)
        {
            if (inSession)
            {
                MeshRenderer.material.SetFloat("_Wireframe", 1);
                if (m_OldDMesh == null)
                {
                    m_OldDMesh = new (umesh.DMesh3);
                }
            }
            else
            {
                MeshRenderer.material.SetFloat("_Wireframe", 0);
            }
        }


        public override void OnEditEnd(bool save)
        {
            if (! save && m_Changed )
            {
                umesh.DMesh3 = m_OldDMesh;
                StartCoroutine(umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
                    {
                        umesh.Mesh.uv4 = DataMesh.ToUV(colors, umesh.DMesh3.VertexMap);
                        umesh.OnMeshChanged.Invoke(umesh.Mesh);
                    }
                ));
            }
            m_Changed = false;
        }

        public override void AddVertex(Vector3 position)
        {
            Changed();
            if (!umesh.DMesh3.CheckValidity(out MeshResult res1))
            {
                UnityEngine.Debug.Log("Add Vertex - Add Vertex given a defective mesh " + res1.ToString());
            }

            Vector3d localPosition = (Vector3d)transform.InverseTransformPoint(position);

            // get the hit triangle
            RaycastHit lastHit = State.instance.lastHit;
            int triangle = lastHit.triangleIndex;

            // get the collision mesh and find the DMesh vertices for the hit triangle
            Mesh cmesh = (State.instance.lastHit.collider as MeshCollider).sharedMesh;
            Vector2[] uv = cmesh.uv4;

            int vIDa = (int)uv[cmesh.triangles[3 * triangle]].y;
            int vIDb = (int)uv[cmesh.triangles[3 * triangle + 1]].y;
            int vIDc = (int)uv[cmesh.triangles[3 * triangle + 2]].y;

            Vector3 uVert0 = cmesh.vertices[vIDa];
            Vector3 uVert1 = cmesh.vertices[vIDb];
            Vector3 uVert2 = cmesh.vertices[vIDc];

            int currentHitTri = umesh.DMesh3.FindTriangle(vIDa, vIDb, vIDc);
            if (!umesh.DMesh3.IsTriangle(currentHitTri))
            {
                UnityEngine.Debug.Log("Bad Triangle when adding vertex to mesh");
                return;
            }
            Index3i tri = umesh.DMesh3.GetTriangle(currentHitTri);
            Vector3d v0 = umesh.DMesh3.GetVertex(vIDa);
            Vector3d v1 = umesh.DMesh3.GetVertex(vIDb);
            Vector3d v2 = umesh.DMesh3.GetVertex(vIDc);

            Vector3d currentBari = MathUtil.BarycentricCoords(ref localPosition, ref v0 , ref v1, ref v2);

            if ((currentBari.x + currentBari.y + currentBari.z) != 1)
            {
                UnityEngine.Debug.Log("invalid barycentric coords" + currentBari.ToString());
                return;
            }
            int edgeId = -1;
            if (currentBari.x > currentBari.y && currentBari.x > currentBari.z)
                if (currentBari.y < currentBari.z)
                    edgeId = umesh.DMesh3.FindEdgeFromTri(tri.a, tri.c, currentHitTri);
                else
                    edgeId = umesh.DMesh3.FindEdgeFromTri(tri.a, tri.b, currentHitTri);
            if (currentBari.y > currentBari.x && currentBari.y > currentBari.z)
                    if (currentBari.x < currentBari.z)
                        edgeId = umesh.DMesh3.FindEdgeFromTri(tri.b, tri.c, currentHitTri);
                    else
                        edgeId = umesh.DMesh3.FindEdgeFromTri(tri.b, tri.a, currentHitTri);
            if (currentBari.z > currentBari.y && currentBari.z > currentBari.x)
                if (currentBari.y < currentBari.x)
                    edgeId = umesh.DMesh3.FindEdgeFromTri(tri.c, tri.a, currentHitTri);
                else
                    edgeId = umesh.DMesh3.FindEdgeFromTri(tri.c, tri.b, currentHitTri);
            if (!umesh.DMesh3.IsEdge(edgeId))
            {
                UnityEngine.Debug.Log("Could not find the edge when adding vertex to mesh");
                return;
            }
            UnityEngine.Debug.Log($"Number of Verteces before edge split {umesh.DMesh3.VertexCount} ");
            umesh.DMesh3.SplitEdge(edgeId, out DMesh3.EdgeSplitInfo result);
            UnityEngine.Debug.Log($"Number of Verteces after edge split {umesh.DMesh3.VertexCount} ");
            umesh.DMesh3.SetVertex(result.vNew, localPosition);
            //if (!umesh.DMesh3.CheckValidity(out MeshResult res2))
            //{
            //    UnityEngine.Debug.Log("Add Vertex - Add Vertex created a defective mesh " + res2.ToString());
            //}
            umesh.RefreshUnityMesh();
            StartCoroutine(umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
            {
                umesh.Mesh.uv4 = DataMesh.ToUV(colors, umesh.DMesh3.VertexMap);
                umesh.OnMeshChanged.Invoke(umesh.Mesh);
            }
            ));
            UnSelected(SelectionType.SELECT);
        }
        /// <summary>
        /// This is called when you want to delete the currently selected vertex
        /// </summary>
        public override void RemoveVertex(Transform vertex = null)
        {
            Changed();
            //if (!umesh.DMesh3.CheckValidity(out MeshResult result))
            //{
            //    UnityEngine.Debug.Log("Remove Vertex - Remove Vertex given a defective mesh " + result.ToString());
            //}
            //bool isClosed = umesh.DMesh3.CachedIsClosed;

            // get the collider mesh to get the triangle and get the DMesh3 vertex ids
            Mesh cmesh = (State.instance.lastHit.collider as MeshCollider).sharedMesh;
            int v1 = (int)cmesh.uv4[cmesh.triangles[m_selectedTriangle * 3]].y;
            int v2 = (int)cmesh.uv4[cmesh.triangles[m_selectedTriangle * 3 + 1]].y;
            int v3 = (int)cmesh.uv4[cmesh.triangles[m_selectedTriangle * 3 + 2]].y;

            // get the DMesh3 triangle
            int triangle = umesh.DMesh3.FindTriangle(v1, v2, v3);

            MeshResult res = umesh.DMesh3.RemoveTriangle(triangle, true, false);
            if (res != MeshResult.Ok)
            {
                UnityEngine.Debug.Log(res.ToString());
                return;
            }

            //if (isClosed)
            //{
            //    int timestamp = umesh.DMesh3.Timestamp;
            //    MeshAutoRepair mr = new(umesh.DMesh3);
            //    if (!mr.Apply())
            //    {
            //        UnityEngine.Debug.Log("Mesh AutoRepair Failed");
            //        return;
            //    }
            //    if (timestamp == umesh.DMesh3.Timestamp)
            //    {
            //        UnityEngine.Debug.Log("Remove Vertex - MeshAutoRepair did nothing");
            //        return;
            //    }
            //}
            //if (!umesh.DMesh3.CheckValidity(out MeshResult res2))
            //{
            //    UnityEngine.Debug.Log("Remove Vertex - Remove Vertex created a defective mesh " + res2.ToString());
            //}
            umesh.RefreshUnityMesh();
            StartCoroutine(umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
            {
                umesh.Mesh.uv4 = DataMesh.ToUV(colors, umesh.DMesh3.VertexMap);
                umesh.OnMeshChanged.Invoke(umesh.Mesh);
            }
            ));
            UnSelected(SelectionType.SELECT);
        }
    }
}