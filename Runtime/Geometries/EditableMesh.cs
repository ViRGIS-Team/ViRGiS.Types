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

using System;
using UnityEngine;
using VirgisGeometry;
using System.Linq;
using Unity.Netcode;
using UnityEngine.Serialization;

namespace Virgis
{

    public class EditableMesh : DataMesh{
        private static readonly int Wireframe = Shader.PropertyToID("_Wireframe");
        [FormerlySerializedAs("MarkerShape")] public GameObject markerShape;

        private GameObject _marker; // Marked used to show the selected Vertex

        private DMesh3 _mOldDMesh; // Saves the DMesh3 for recovery on Save and Discard
        private Matrix4x4 _mOldTransform; // Saves the Transform for recovery on Save and Discard

        private bool _mChanged; // True if the Mesh has been changed in an edit session
        private bool _mBlockMove; // is entity in a block-move state

        private int _mSelectedVertex; // holds the current DMesh vertex ID for the selected vertex - note that in clients this NOT the same as the DMesh vertex id
        private int _mSelectedTriangle; // holds the current Unity Mesh triangle ID - note that in clients this is not the same as the DMesh ytriangle ID
        private int _n = -1; //indicator that the n-ring is built
        private bool _mSelectOn;

        public override void Selected(SelectionType button){
            if (_mSelectOn)
            {
                UnSelected(SelectionType.SELECT);
            }
            _mSelectOn = true;
            RaycastHit lastHit = State.Instance.LastHit;
            _mSelectedTriangle = lastHit.triangleIndex;
            Vector3 bary = lastHit.barycentricCoordinate;
            int[] triangles = ((MeshCollider)lastHit.collider).sharedMesh.triangles;
            int selectedUVertex = 0;
            if (bary.x >= bary.y && bary.x >= bary.z) selectedUVertex = triangles[_mSelectedTriangle * 3]; 
            else if (bary.y >= bary.x && bary.y >= bary.z) selectedUVertex = triangles[_mSelectedTriangle * 3 + 1];
            else if (bary.z >= bary.x && bary.z >= bary.y) selectedUVertex = triangles[_mSelectedTriangle * 3 + 2];
            
            
            // get the collider mesh to get the verticesMesh and get the DMesh3 vertex id
            Mesh cmesh = ((MeshCollider)State.Instance.LastHit.collider).sharedMesh;
            Vector2 uv = cmesh.uv4[ selectedUVertex];
            _mSelectedVertex = (int)uv.y;

            //send to server
            SelectedRpc(button, (int)uv.y);

            if (button == SelectionType.SELECTALL)
            {
                _mBlockMove = true;
            }

            //Move the selected marker to the vertex
            _marker = Instantiate(markerShape);
            _marker.transform.parent = transform;
            _marker.transform.localPosition = cmesh.vertices[_mSelectedVertex];
        }

        [Rpc(SendTo.Server)]
        private void SelectedRpc(SelectionType button, int hitPosition){
            _mSelectOn = true;
            transform.parent.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
            _mSelectedVertex = hitPosition;
            if (button == SelectionType.SELECTALL)
            {
                _mBlockMove = true;
            }
            else
            {

            }
        }

        public new void UnSelected(SelectionType button){
            _mSelectOn = false;
            _mSelectedVertex = 0;
            _mBlockMove = false;
            UnSelectedRpc(button);
            Destroy(_marker);
            _n = -1;
        }

        [Rpc(SendTo.Server)]
        public void UnSelectedRpc(SelectionType button){
            transform.parent.SendMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
            _mSelectOn = false;
            _mBlockMove = false;
            _n = -1;
        }

        public override void Changed()
        {
            base.Changed();
            _mChanged = true;
        }

        /// <summary>
        /// This is called by the Avatar on the client to move the current vertex
        /// </summary>
        /// <param name="args"></param>
        public override void MoveTo(MoveArgs args) {
            if (!_mSelectOn) return;
            Changed();
            if (_mBlockMove)
            {
                if (args.translate != Vector3.zero)
                {
                    transform.Translate(args.translate, Space.World);
                    transform.parent.SendMessage("Translate", args);
                }
            }
            else
            {
                Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                if (_marker) _marker.transform.localPosition += localTranslate;
                if (args.translate != Vector3.zero && _mSelectOn)
                {
                    Vector3d target;
                    if (Umesh.DMesh3.IsVertex(_mSelectedVertex))
                    {
                        target = Umesh.DMesh3.GetVertex(_mSelectedVertex) + localTranslate;
                    } else 
                    {
                        Debug.Log("Selected Vertex is not a Vertex");
                        return;
                    }

                    // check the current submesh is still correct
                    if (_n != (int)args.scale)
                    {
                        _n = (int)args.scale;

                        //
                        // create an n-ring Sub Mesh
                        //
                        Umesh.SubMesh = new(Umesh.DMesh3);
                        Umesh.SubMesh.Compute(_mSelectedVertex, _n);
                    }
                    //
                    // create the deformer
                    // set the constraint that the selected vertex is moved to position
                    // set the contraint that the n-ring remains stationary
                    //
                    LaplacianMeshDeformer deform = new LaplacianMeshDeformer(Umesh.SubMesh);
                    deform.SetConstraint(_mSelectedVertex, target, 1, true);
                    foreach (int v in MeshIterators.BoundaryVertices(Umesh.SubMesh))
                    {
                        if (v == _mSelectedVertex) continue;
                        if (Umesh.SubMesh.IsSubmeshInternalBoundaryVertex(v))
                        {
                            deform.SetConstraint(v, Umesh.SubMesh.GetVertex(v), 1, true);
                        } else
                        {
                            deform.SetConstraint(v, Umesh.SubMesh.GetVertex(v), 3);
                        }
                    }
                    deform.SolveAndUpdateMesh();
                    Mesh sm = meshFilter.sharedMesh;
                    Vector3[] vertices = sm.vertices;

                    foreach (int vert in Umesh.SubMesh.VertexIndices())
                    {
                        int vID = MVertexMap[vert];
                        vertices[vID] = (Vector3)Umesh.SubMesh.GetVertex(vert);
                    }
                    sm.vertices = vertices;
                    sm.UploadMeshData(false);
                    if (!NetworkManager.Singleton.IsHost)
                    {
                        MoveToRpc(
                            Umesh.SubMesh.VertexIndices().ToArray(),
                            Umesh.SubMesh.VertexValues().ToArray(),
                            Umesh.SubMesh.axisOrder.ToArray()
                            );
                    }
                }
            }
            UpdateUnityMesh();
        }

        [Rpc(SendTo.Server)]
        private void MoveToRpc(int[] vIDs, double[] values, byte[] axisOrder)
        {
            _mChanged = true;
            foreach (int t in vIDs)
            {
                int pointer = 0;
                Vector3d val = new Vector3d(values[pointer++], values[pointer++], values[pointer++]) { axisOrder = new AxisOrder(axisOrder) };
                Umesh.DMesh3.SetVertex(t, val);
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
                Vector3 a = transform.localPosition;
                Vector3 b = transform.InverseTransformPoint(args.pos);
                Vector3 c = a - b;
                float rs = args.scale;
                Vector3 fp = b + c * rs;
                if (fp.magnitude < float.MaxValue)
                {
                    transform.localScale = transform.localScale * rs;
                    transform.localPosition = fp;
                }
            //}
        }

        /// <summary>
        /// Draws the mesh represented by dmeshin - which should be in local space coordinates
        /// i.e. Map Space coordinates without CRs or projection but that could be affected by 
        /// layer level scaling
        /// </summary>
        /// <param name="dmeshin"></param>
        /// <param name="bodySymbology">Symbology</param>
        /// <returns></returns>
        public Transform Draw(DMesh3 dmeshin, UnitPrototype bodySymbology){
            SerializableMaterialHash hash = new()
            {
                Name = "body",
                Color = bodySymbology.Color,
            };
            int hVc;
            switch (bodySymbology.ColorMode)
            {
                case ColorMode.SingleColor:
                    hVc = 0;
                    break;
                case ColorMode.MultibandColor:
                    hVc = dmeshin.HasVertexColors ? 1 : 0;
                    break;
                case ColorMode.SinglebandColor:
                    hVc = 1;
                    break;
                default:
                    hVc = 0;
                    break;
            }
            hash.properties = new[]
            {
            new SerializableProperty()
            {
                Key = "_hasVertexColor",
                Value = hVc
            }
            };

            Spawn(transform.parent);
            SetMaterial(hash);
            Umesh.SetMesh(dmeshin);
            StartCoroutine(Umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
            {
                Umesh.Mesh.uv4 = DataMesh.ToUV(colors, Umesh.DMesh3.VertexMap);
                Umesh.MeshSerialize();
            }
            ));
            return transform;
        }


        public override void OnEdit(bool inSession)
        {
            if (inSession)
            {
                meshRenderer.material.SetFloat(Wireframe, 1);
                if (! _mChanged)
                {
                    _mOldDMesh = new (Umesh.DMesh3);
                    _mOldTransform = Matrix4x4.TRS(
                        transform.position,
                        transform.rotation,
                        transform.localScale
                    );
                    Debug.LogWarning($"Checkpoint saved for Object {GetId()}");
                }
            }
            else
            {
                meshRenderer.material.SetFloat(Wireframe, 0);
            }
        }


        public override void OnEditEnd(bool save)
        {
            if (! save && _mChanged )
            {
                Umesh.SetMesh(_mOldDMesh);
                transform.position = _mOldTransform.GetColumn(3);
                transform.rotation = Quaternion.LookRotation(
                    _mOldTransform.GetColumn(2),
                    _mOldTransform.GetColumn(1)
                    );
                transform.localScale = new Vector3(
                    _mOldTransform.GetColumn(0).magnitude,
                    _mOldTransform.GetColumn(1).magnitude,
                    _mOldTransform.GetColumn(2).magnitude
                );
                StartCoroutine(Umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
                    {
                        Umesh.Mesh.uv4 = DataMesh.ToUV(colors, Umesh.DMesh3.VertexMap);
                        Umesh.OnMeshChanged.Invoke(Umesh.Mesh);
                        Debug.LogWarning($"Checkpoint restored for Object {GetId()}");
                    }
                ));

            }
            _mChanged = false;
        }

        public override void AddVertex(Vector3 position)
        {
            Changed();
            if (!Umesh.DMesh3.CheckValidity(out MeshResult res1))
            {
                Debug.Log("Add Vertex - Add Vertex given a defective mesh " + res1.ToString());
            }

            Vector3d localPosition = transform.InverseTransformPoint(position);

            // get the hit triangle
            RaycastHit lastHit = State.Instance.LastHit;
            int triangle = lastHit.triangleIndex;

            // get the collision mesh and find the DMesh vertices for the hit triangle
            Mesh cmesh = ((MeshCollider)State.Instance.LastHit.collider).sharedMesh;
            Vector2[] uv = cmesh.uv4;

            int vIDa = (int)uv[cmesh.triangles[3 * triangle]].y;
            int vIDb = (int)uv[cmesh.triangles[3 * triangle + 1]].y;
            int vIDc = (int)uv[cmesh.triangles[3 * triangle + 2]].y;

            int currentHitTri = Umesh.DMesh3.FindTriangle(vIDa, vIDb, vIDc);
            if (!Umesh.DMesh3.IsTriangle(currentHitTri))
            {
                Debug.Log("Bad Triangle when adding vertex to mesh");
                return;
            }
            Index3i tri = Umesh.DMesh3.GetTriangle(currentHitTri);
            Vector3d v0 = Umesh.DMesh3.GetVertex(vIDa);
            Vector3d v1 = Umesh.DMesh3.GetVertex(vIDb);
            Vector3d v2 = Umesh.DMesh3.GetVertex(vIDc);

            Vector3d currentBari = MathUtil.BarycentricCoords(ref localPosition, ref v0 , ref v1, ref v2);

            if (Math.Abs((currentBari.x + currentBari.y + currentBari.z) - 1) > 0.0001)
            {
                Debug.Log("invalid barycentric coords" + currentBari.ToString());
                return;
            }
            int edgeId = -1;
            if (currentBari.x > currentBari.y && currentBari.x > currentBari.z)
                edgeId = Umesh.DMesh3.FindEdgeFromTri(tri.a, currentBari.y < currentBari.z ? tri.c : tri.b, currentHitTri);
            if (currentBari.y > currentBari.x && currentBari.y > currentBari.z)
                edgeId = Umesh.DMesh3.FindEdgeFromTri(tri.b, currentBari.x < currentBari.z ? tri.c : tri.a, currentHitTri);
            if (currentBari.z > currentBari.y && currentBari.z > currentBari.x)
                edgeId = Umesh.DMesh3.FindEdgeFromTri(tri.c, currentBari.y < currentBari.x ? tri.a : tri.b, currentHitTri);
            if (!Umesh.DMesh3.IsEdge(edgeId))
            {
                Debug.Log("Could not find the edge when adding vertex to mesh");
                return;
            }
            Debug.Log($"Number of Verteces before edge split {Umesh.DMesh3.VertexCount} ");
            Umesh.DMesh3.SplitEdge(edgeId, out DMesh3.EdgeSplitInfo result);
            Debug.Log($"Number of Verteces after edge split {Umesh.DMesh3.VertexCount} ");
            Umesh.DMesh3.SetVertex(result.vNew, localPosition);
            Umesh.MeshSerialize();
            StartCoroutine(Umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
            {
                Umesh.Mesh.uv4 = DataMesh.ToUV(colors, Umesh.DMesh3.VertexMap);
                Umesh.OnMeshChanged.Invoke(Umesh.Mesh);
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
            Mesh cmesh = ((MeshCollider)State.Instance.LastHit.collider).sharedMesh;
            int v1 = (int)cmesh.uv4[cmesh.triangles[_mSelectedTriangle * 3]].y;
            int v2 = (int)cmesh.uv4[cmesh.triangles[_mSelectedTriangle * 3 + 1]].y;
            int v3 = (int)cmesh.uv4[cmesh.triangles[_mSelectedTriangle * 3 + 2]].y;

            // get the DMesh3 triangle
            int triangle = Umesh.DMesh3.FindTriangle(v1, v2, v3);

            MeshResult res = Umesh.DMesh3.RemoveTriangle(triangle);
            if (res != MeshResult.Ok)
            {
                Debug.Log(res.ToString());
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
            Umesh.MeshSerialize();
            StartCoroutine(Umesh.DMesh3.ColorisationCoroutine(20, (colors) =>
            {
                Umesh.Mesh.uv4 = DataMesh.ToUV(colors, Umesh.DMesh3.VertexMap);
                Umesh.OnMeshChanged.Invoke(Umesh.Mesh);
            }
            ));
            UnSelected(SelectionType.SELECT);
        }
    }
}