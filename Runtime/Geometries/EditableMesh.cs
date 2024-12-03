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
using gs;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using Unity.Netcode;

namespace Virgis
{

    public class EditableMesh : DataMesh{
        public GameObject MarkerShape;

        private GameObject marker;

        private bool m_BlockMove = false; // is entity in a block-move state
        private DMesh3 m_oldDmesh; // holds the previous mesh during editing
        private int m_selectedVertex; // holds the current Unity Mesh vertex ID for the selected vertex - note that in clients this NOT the same as the DMesh vertex id
        private int n = -1; //indicator that the n-ring is built
        private bool m_selectOn = false;

        public override void OnNetworkSpawn(){
            base.OnNetworkSpawn();
            umesh.OnVertexChanged += UpdateSubmesh;
        }

        public override void OnNetworkDespawn(){
            base.OnNetworkDespawn();
            umesh.OnVertexChanged -= UpdateSubmesh;
        }

        public override void Selected(SelectionType button){
            if (m_selectOn)
            {
                UnSelected(SelectionType.SELECT);
            }
            m_selectOn = true;
            RaycastHit lastHit = State.instance.lastHit;
            int triangle = lastHit.triangleIndex;
            Vector3 bary = lastHit.barycentricCoordinate;
            int[] triangles = (lastHit.collider as MeshCollider).sharedMesh.triangles;
            if (bary.x >= bary.y && bary.x >= bary.z) m_selectedVertex = triangles[triangle * 3]; 
            else if (bary.y >= bary.x && bary.y >= bary.z) m_selectedVertex = triangles[triangle * 3 + 1];
            else if (bary.z >= bary.x && bary.z >= bary.y) m_selectedVertex = triangles[triangle * 3 + 2];
            
            
            // get the collider mesh to get the verticesMesh and get the DMesh3 vertex id
            Mesh cmesh = (lastHit.collider as MeshCollider).sharedMesh;
            Vector2 uv = cmesh.uv4[ m_selectedVertex];

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
            m_oldDmesh = new DMesh3(umesh.DMesh3);
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
            UpdateUnityMesh();
        }

        [Rpc(SendTo.Server)]
        public void UnSelectedRpc(SelectionType button){
            transform.parent.SendMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
            m_selectOn = false;
            m_BlockMove = false;
            n = -1;
        }

        /// <summary>
        /// This is called by the Avatar on the client to move the current vertex
        /// </summary>
        /// <param name="args"></param>
        public override void MoveTo(MoveArgs args){
            if (!m_BlockMove)
            {
                Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                marker.transform.localPosition += localTranslate;//= transform.InverseTransformPoint(args.pos);
                Mesh sm = MeshFilter.sharedMesh;
                Vector3[] vertices = MeshFilter.sharedMesh.vertices;
                vertices[m_selectedVertex] += localTranslate;
                sm.vertices = vertices;
            }
            MoveToRpc(args, m_State, !IsServer);
        }

        /// <summary>
        /// This is run on the server whenever there is a movbe event
        /// </summary>
        /// <param name="args"></param>
        protected override void _move(MoveArgs args){
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
                if (args.translate != Vector3.zero && m_selectOn)
                {
                    Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                    Vector3d target = umesh.DMesh3.GetVertex(m_selectedVertex) + localTranslate;
                    target.axisOrder = AxisOrder.EUN;

                    // check the current submesh is still correct
                    if (n != (int)args.scale)
                    {
                        n = (int)args.scale;

                        //
                        // create an n-ring Sub Mesh
                        //
                        umesh.SubMesh.Compute(m_selectedVertex, n);
                    }
                    //
                    // create the deformer
                    // set the constraint that the selected vertex is moved to position
                    // set the contraint that the n-ring remains stationary
                    //
                    LaplacianMeshDeformer deform = new LaplacianMeshDeformer(GetMesh());
                    deform.SetConstraint(m_selectedVertex, target, 1, true);
                    foreach (int v in MeshIterators.BoundaryVertices(umesh.SubMesh))
                    {
                        deform.SetConstraint(v, umesh.SubMesh.GetVertex(v), 10, false);
                    }
                    deform.SolveAndUpdateMesh();
                }
                timer.Stop();
                UnityEngine.Debug.LogWarning($"Edit took : {timer.Elapsed.TotalSeconds} seconds");
                umesh.SendUpdateSubmesh();
            }
        }

        protected void UpdateSubmesh(int[] vIDs, Vector3[] values){
            Mesh sm = MeshFilter.sharedMesh;
            Vector3[] vertices = sm.vertices;

            // map all of the change
            for (int i = 0; i< vIDs.Length; i++)
            {
                int vID = m_VertexMap[vIDs[i]];
                if (m_selectOn && vID == m_selectedVertex) continue;
                vertices[vID] = values[i];
            }
            sm.vertices = vertices;
        }

        /// <summary>
        /// This is called by the parent to action the move
        /// </summary>
        /// <param name="args"></param>
        /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public void MoveAxisAction(MoveArgs args){
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
                if (FP.magnitude < float.MaxValue)
                {
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
                umesh.Mesh.uv4 = DataMesh.ToUV(colors);
                umesh.MeshFinalize();
            }
            ));
            return transform;
        }


        public override void OnEdit(bool inSession){
            if (inSession)
            {
                MeshRenderer.material.SetFloat("_Wireframe", 1);
            }
            else
            {
                MeshRenderer.material.SetFloat("_Wireframe", 0);
            }
        }

        [Rpc(SendTo.Server)]
        public override void AddVertexRpc(Vector3 position){
            Vector3d localPosition = (Vector3d)transform.InverseTransformPoint(position);
            DMesh3 mesh = GetMesh();
            //m_aabb = new DMeshAABBTree3(mesh, true);
            //currentHitTri = m_aabb.FindNearestTriangle(localPosition);
            //m_aabb.Build();
            //Vector3d V0 = new Vector3d();
            //Vector3d V1 = new Vector3d();
            //Vector3d V2 = new Vector3d();
            //mesh.GetTriVertices(currentHitTri, ref V0, ref V1, ref V2);

            int currentHitTri = State.instance.lastHit.triangleIndex;
            Index3i tri = mesh.GetTriangle(currentHitTri);
            Vector3 currentBari = State.instance.lastHit.barycentricCoordinate; //MathUtil.BarycentricCoords(localPosition, V0, V1, V2);

            int edgeId = 0;
            if (currentBari.x > currentBari.y && currentBari.x > currentBari.z)
                if (currentBari.y < currentBari.z)
                    edgeId = mesh.FindEdgeFromTri(tri.a, tri.c, currentHitTri);
                else
                    edgeId = mesh.FindEdgeFromTri(tri.a, tri.b, currentHitTri);
            if (currentBari.y > currentBari.x && currentBari.y > currentBari.z)
                if (currentBari.x < currentBari.z)
                    edgeId = mesh.FindEdgeFromTri(tri.b, tri.c, currentHitTri);
                else
                    edgeId = mesh.FindEdgeFromTri(tri.b, tri.a, currentHitTri);
            if (currentBari.z > currentBari.y && currentBari.z > currentBari.x)
                if (currentBari.y < currentBari.x)
                    edgeId = mesh.FindEdgeFromTri(tri.c, tri.a, currentHitTri);
                else
                    edgeId = mesh.FindEdgeFromTri(tri.c, tri.b, currentHitTri);

            mesh.SplitEdge(edgeId, out DMesh3.EdgeSplitInfo result);
            mesh.SetVertex(result.vNew, localPosition);
            umesh.DMesh3 = mesh;
            UnSelected(SelectionType.SELECT);
        }

        /// <summary>
        /// This is called when you want to delete the currently selected vertex
        /// </summary>
        public void Delete(){
            MeshFilter mf = GetComponent<MeshFilter>();
            GetMesh().RemoveVertex(m_selectedVertex);
            if (m_oldDmesh.IsClosed())
            {
                MeshAutoRepair mr = new MeshAutoRepair(GetMesh());
                mr.Apply();
                umesh.DMesh3 = mr.Mesh;
            }
            else
            {
                GetMesh().CompactInPlace();
            }
            UnSelected(SelectionType.SELECT);
        }
    }
}
