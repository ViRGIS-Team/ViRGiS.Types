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

// parts from  https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html

using System.Collections.Generic;
using UnityEngine;
using System;
using VirgisGeometry;
using System.Linq;
using Unity.Netcode;

namespace Virgis
{

    /// <summary>
    /// Controls and Instance of a Line Component
    /// </summary>
    public class Dataline : VirgisFeature
    {
        public GameObject CylinderObject;


        private bool m_Lr = false; // is this line a Linear Ring - i.e. used to define a polygon
        public List<VertexLookup> VertexTable = new ();
        public List<VertexLookup> SegmentTable = new();
        private GameObject m_handlePrefab;
        private SerializableMaterialHash m_Point_hash;
        private SerializableMaterialHash m_Line_hash;
        public DCurve3 Curve; // The DCurve3 of the line. Is in world coordinates and kept updated

        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        public void Update()
        {
            if (Label) Label.LookAt(State.instance.mainCamera.transform);
        }


        public override void VertexMove(MoveArgs data)
        {
            if (VertexTable.Contains(new VertexLookup() { Id = data.id})) {
                VertexLookup vdata = VertexTable.Find(item => item.Id == data.id);
                foreach (VertexLookup vLookup in VertexTable) {
                    if (vLookup.LineComp && vLookup.LineComp.m_vStart == vdata.Vertex)
                        vLookup.LineComp.MoveStart(data.pos);
                    if (vLookup.LineComp && vLookup.LineComp.m_vEnd == vdata.Vertex)
                        vLookup.LineComp.MoveEnd(data.pos);
                }
                if (Label) Label.position = _labelPosition();
                Curve.SetVertex(vdata.Vertex, data.pos);
            }
            base.VertexMove(data);
        }


        /// <summary>
        /// This is called by the parent to action the move
        /// </summary>
        /// <param name="args"></param>
        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public void MoveAxisAction(MoveArgs args)
        {
            if (args.translate != null) transform.Translate(args.translate, Space.World);
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
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform T = transform.GetChild(i);
                    if (T.GetComponent<LineSegment>() != null)
                    {
                        Vector3 local = T.localScale;
                        local /= RS;
                        local.z = T.localScale.z;
                        T.localScale = local;
                    }
                    else
                    {
                        T.localScale /= RS;
                    }
                }
            }
        }

        /// <summary>
        /// Called to draw the line
        /// </summary>
        /// <param name="curve"> A LineString in DCurve3 format in world space coordinates</param>
        /// <param name="symbology">The symbo,logy to be applied to the line</param>
        /// <param name="handlePrefab"> The prefab to be used for the handle</param>
        /// <param name="labelPrefab"> the prefab to used for the label</param>
        public void Draw(DCurve3 curve, Dictionary<string, SerializableMaterialHash> symbology,  GameObject handlePrefab, GameObject labelPrefab)
        {
            Curve = curve;
            m_Lr = curve.Closed;
            if (!symbology.TryGetValue("point", out m_Point_hash)) m_Point_hash = new();
            if (!symbology.TryGetValue("line", out m_Line_hash)) m_Line_hash = new();
            m_handlePrefab = handlePrefab;

            List<Vector3d> line = curve.VertexItr().ToList();
            int i = 0;
            foreach (Vector3d vertex3d in line)
            {
                Vector3 vertex = (Vector3)vertex3d;
                _createVertex(vertex, i);
                if (i + 1 != line.Count)
                {
                    _createSegment(vertex, (Vector3)line[i + 1],i , false);
                } else {
                    if (curve.Closed)
                        _createSegment(vertex, (Vector3)line[0], i, true);
                }
                i++;
            }

            //Set the label
            //if (labelPrefab != null)
            //{
            //    Dictionary<string, object> meta = transform.parent.GetComponent<IVirgisEntity>().GetInfo(this); 
            //    if (symbology["line"].ContainsKey("Label") && symbology["line"].Label != null && (meta?.ContainsKey(symbology["line"].Label) ?? false))
            //       {
            //        GameObject labelObject = Instantiate(labelPrefab, _labelPosition(), Quaternion.identity, transform);
            //        label = labelObject.transform;
            //        Text labelText = labelObject.GetComponentInChildren<Text>();
            //        labelText.text = (string)meta[symbology["line"].Label];
            //    }
            //}
        }

        /// <summary>
        /// Make the Line into a Linear Ring by setting the Lr flag and creating a LineSegment form the last vertex to the first.
        /// If the last vertex is in the same (exact) position as the first vertex, the last vertex is deleted.
        /// </summary>
        public void MakeLinearRing() {
            // Make the Line inot a Linear ring
            if (!m_Lr) {
                VertexLookup First = VertexTable.Find(item => item.Vertex == 0);
                VertexLookup Last = VertexTable.Find(item => item.Vertex == VertexTable.Count - 1);
                if (First.VertexComp.transform.position == Last.VertexComp.transform.position) {
                    Destroy(Last.VertexComp.gameObject);
                    VertexTable.Remove(Last);
                    Last = VertexTable.Find(item => item.Vertex == Last.Vertex - 1);
                    Last.LineComp.MoveEnd(First.VertexComp.transform.position);
                    Last.LineComp.m_vEnd = 0;
                } else {
                    VertexTable.Last().LineComp = _createSegment(VertexTable.Last().VertexComp.transform.position, VertexTable.First().VertexComp.transform.position, VertexTable.Count -1, true);
                }

                m_Lr = true;
            }
        }



        public override void Selected(SelectionType button)
        {
            if (button == SelectionType.SELECTALL)
            {
                gameObject.BroadcastMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_SetBlockMove(true);
            }
        }

        public override void UnSelected(SelectionType button)
        {
            if (button != SelectionType.BROADCAST)
            {
                gameObject.BroadcastMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_SetBlockMove(false);
            }
        }

        public override void Translate(MoveArgs args)
        {
            if (!m_State.BlockMove)
            {
                BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                args.id = GetId();
                transform.parent.SendMessage("Translate", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        protected override void _move(MoveArgs args)
        {
            throw new NotImplementedException();
        }

        public override void AddVertex(Vector3 position) {
            int seg = Curve.NearestSegment(position);
            ulong segment = VertexTable.Find(item => item.Vertex == seg).Id;
            AddVertexRpc(segment, position);
            Curve.InsertVertex(position, seg);
            Curve.InsertData((long)Curve.VertexCount, seg);
        }

        /// <summary>
        /// Add a vertx to the Line when you know the segment to add the vertex to
        /// </summary>
        /// <param name="segment"> Linesegement to add the vertex to </param>
        /// <param name="position"> Vertex Position in Wordl Space coordinates</param>
        /// <returns></returns>
        [Rpc(SendTo.Server)]
        public void AddVertexRpc(ulong segmentId, Vector3 position) {
            VertexLookup segmentLookup = SegmentTable.Find(seg => seg.Id == segmentId);
            if (segmentLookup == null) return;
            LineSegment segment = segmentLookup.LineComp;
            int start = segment.m_vStart;
            int next = segment.m_vEnd;
            VertexTable.ForEach(item => {
                if (item.Vertex > start) {
                    item.Vertex++;
                    if (item.LineComp != null) {
                        item.LineComp.m_vStart++;
                        if (item.LineComp.m_vEnd != 0) {
                            item.LineComp.m_vEnd++;
                        }
                    }
                }
                if (m_Lr && item.LineComp.m_vStart == start) {
                    item.LineComp.m_vEnd = start + 1;
                }
                if (m_Lr && item.LineComp.m_vEnd > VertexTable.Count)
                    item.LineComp.m_vEnd = 0;
            });
            start++;
            int end = next;
            if (end != 0)
                end++;
            segment.MoveEnd(position);
            Datapoint vertex = _createVertex(position, start);
            Curve.InsertVertex(position, start);
            _createSegment(position, VertexTable.Find(item => item.Vertex == end).VertexComp.transform.position, start, end == 0);
            transform.parent.SendMessage("AddVertex", position, SendMessageOptions.DontRequireReceiver);
            vertex.UnSelected(SelectionType.SELECT);
        }

        public override void RemoveVertex(Transform vertex)
        {
            ulong vID = vertex.GetComponent<VirgisFeature>().GetId();
            if (m_State.BlockMove)
            {
                RemoveFeatureRpc();
            }
            else
            {
                RemoveLineVertexRpc(vID);
            }
        }

        [Rpc(SendTo.Server)]
        public void RemoveLineVertexRpc(ulong vertex)
        {
            VertexLookup vLookup = VertexTable.Find(item => item.Id == vertex);
            if (vLookup == null) return;
            if (GetParent(out IVirgisEntity parent)) parent.RemoveVertex(vLookup.VertexComp.transform);
            int thisVertex = vLookup.Vertex;
            if (vLookup.LineComp != null)
            {
                vLookup.LineComp.RemoveFeatureRpc();
            }
            else
            {
                VertexTable.Find(item => item.Vertex == vLookup.Vertex - 1).LineComp.RemoveFeatureRpc();
            }
            vLookup.VertexComp.RemoveFeatureRpc();
            VertexTable.Remove(vLookup);
            Curve.RemoveVertex(thisVertex);
            VertexTable.ForEach(item =>
            {
                if (item.Vertex >= thisVertex)
                {
                    item.Vertex--;
                    if (item.LineComp != null)
                    {
                        item.LineComp.m_vStart--;
                        if (item.LineComp.m_vEnd != 0)
                        {
                            item.LineComp.m_vEnd--;
                        }
                    }
                };
                if (m_Lr && item.LineComp.m_vEnd >= VertexTable.Count)
                {
                    item.LineComp.m_vEnd = 0;
                };
            });
            int end = thisVertex;
            int start = thisVertex - 1;
            if (m_Lr && thisVertex >= VertexTable.Count)
                end = 0;
            if (m_Lr && thisVertex == 0)
                start = VertexTable.Count - 1;
            Debug.Log($"start : {start}, End : {end}");
            if (VertexTable.Count > 1)
            {
                VertexTable.Find(item => item.Vertex == start).LineComp.MoveEnd(VertexTable.Find(item => item.Vertex == end).VertexComp.transform.position);
            }
            else
            {
                RemoveFeatureRpc();
            }
        }

        private Datapoint _createVertex(Vector3 vertex, int i) {
            GameObject handle = Instantiate(m_handlePrefab, vertex, Quaternion.identity, transform );
            Datapoint com = handle.GetComponent<Datapoint>();
            com.Spawn(transform);
            com.SetMaterial(m_Point_hash);
            VertexTable.Add(new VertexLookup() { Id = com.GetId(), Vertex = i, VertexComp = com });
            handle.transform.localScale = Symbology.ContainsKey("point") ? Symbology["point"].Transform.Scale : Vector3.one;
            return com;
        }

        private LineSegment _createSegment(Vector3 start, Vector3 end, int i, bool close) {
            GameObject lineSegment = Instantiate(CylinderObject, start, Quaternion.identity, transform);
            LineSegment com = lineSegment.GetComponent<LineSegment>();
            com.Spawn(transform);
            com.SetMaterial(m_Line_hash);
            com.Draw(start, end, i, i + 1, Symbology["line"].Transform.Scale.magnitude);
            if (close)
                com.m_vEnd = 0;
            VertexTable.Find(item => item.Vertex == i).LineComp = com;
            SegmentTable.Add(new VertexLookup() { Id = com.GetId(), LineComp = com });
            return com;
        }

        /// <summary>
        /// get the center of the line
        /// 
        /// <returns></returns>
        private Vector3 Center() {
            return (Vector3) Curve.CenterMark();
        }

        private Vector3 _labelPosition() {
            return Center() + transform.TransformVector(Vector3.up) * Symbology["line"].Transform.Scale.magnitude;
        }

        public VirgisFeature GetVertexById(int vID) 
        {
            VertexLookup vertex = VertexTable.Find(vertex => vertex.Vertex == vID);
            if (vertex != null)
                return vertex.VertexComp;
            else
            {
                Debug.LogError($"DataLine GetVertexById : Could not find vertex {vID}");
                return null;
            }
        }

    }
}
