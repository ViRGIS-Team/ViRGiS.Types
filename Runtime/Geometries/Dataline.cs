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
using UnityEngine.Serialization;

namespace Virgis
{

    /// <summary>
    /// Controls and Instance of a Line Component
    /// </summary>
    public class Dataline : VirgisFeature
    {
        [FormerlySerializedAs("CylinderObject")] public GameObject cylinderObject;
        public DCurve3 Curve; // The DCurve3 of the line. Is in world coordinates and kept update


        private bool _mLr; // is this line a Linear Ring - i.e. used to define a polygon
        private readonly List<VertexLookup> _vertexTable = new ();
        private readonly List<VertexLookup> _segmentTable = new();
        private GameObject _mHandlePrefab;
        private SerializableMaterialHash _mPointHash;
        private SerializableMaterialHash _mLineHash;

        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        public void Update()
        {
            if (label) label.LookAt(State.Instance.MainCamera.transform);
        }


        public override void VertexMove(MoveArgs data)
        {
            if (_vertexTable.Contains(new VertexLookup() { Id = data.id})) {
                VertexLookup vdata = _vertexTable.Find(item => item.Id == data.id);
                foreach (VertexLookup vLookup in _vertexTable) {
                    if (vLookup.LineComp && vLookup.LineComp.mVStart == vdata.Vertex)
                        vLookup.LineComp.MoveStart(data.pos);
                    if (vLookup.LineComp && vLookup.LineComp.mVEnd == vdata.Vertex)
                        vLookup.LineComp.MoveEnd(data.pos);
                }
                if (label) label.position = _labelPosition();
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
            transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            transform.RotateAround(args.pos, axis, angle);
            Vector3 a = transform.localPosition;
            Vector3 b = transform.parent.InverseTransformPoint(args.pos);
            Vector3 c = a - b;
            float rs = args.scale;
            Vector3 fp = b + c * rs;
            if (fp.magnitude < float.MaxValue)
            {
                transform.localScale = transform.localScale * rs;
                transform.localPosition = fp;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform t = transform.GetChild(i);
                    if (t.GetComponent<LineSegment>() != null)
                    {
                        Vector3 local = t.localScale;
                        local /= rs;
                        local.z = t.localScale.z;
                        t.localScale = local;
                    }
                    else
                    {
                        t.localScale /= rs;
                    }
                }
            }
        }

        /// <summary>
        /// Called to draw the line
        /// </summary>
        /// <param name="curve"> A LineString in DCurve3 format in world space coordinates</param>
        /// <param name="symbology">The symbology to be applied to the line</param>
        /// <param name="handlePrefab"> The prefab to be used for the handle</param>
        /// <param name="labelPrefab"> the prefab to used for the label</param>
        public void Draw(DCurve3 curve, Dictionary<string, SerializableMaterialHash> symbology,  GameObject handlePrefab, GameObject labelPrefab)
        {
            Curve = curve;
            _mLr = curve.Closed;
            if (!symbology.TryGetValue("point", out _mPointHash)) _mPointHash = new();
            if (!symbology.TryGetValue("line", out _mLineHash)) _mLineHash = new();
            _mHandlePrefab = handlePrefab;

            List<Vector3d> line = curve.VertexItr().ToList();
            int i = 0;
            foreach (Vector3d vertex3D in line)
            {
                Vector3 vertex = (Vector3)vertex3D;
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
            // Make the Line not a Linear ring
            if (!_mLr) {
                VertexLookup first = _vertexTable.Find(item => item.Vertex == 0);
                VertexLookup last = _vertexTable.Find(item => item.Vertex == _vertexTable.Count - 1);
                if (first.VertexComp.transform.position == last.VertexComp.transform.position) {
                    Destroy(last.VertexComp.gameObject);
                    _vertexTable.Remove(last);
                    last = _vertexTable.Find(item => item.Vertex == last.Vertex - 1);
                    last.LineComp.MoveEnd(first.VertexComp.transform.position);
                    last.LineComp.mVEnd = 0;
                } else {
                    _vertexTable.Last().LineComp = _createSegment(_vertexTable.Last().VertexComp.transform.position, _vertexTable.First().VertexComp.transform.position, _vertexTable.Count -1, true);
                }

                _mLr = true;
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
            if (!MState.BlockMove)
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
            ulong segment = _vertexTable.Find(item => item.Vertex == seg).Id;
            AddVertexRpc(segment, position);
            Curve.InsertVertex(position, seg);
            Curve.InsertData((long)Curve.VertexCount, seg);
        }

        /// <summary>
        /// Add a vertx to the Line when you know the segment to add the vertex to
        /// </summary>
        /// <param name="segmentId"> Line segment to add the vertex to </param>
        /// <param name="position"> Vertex Position in World Space coordinates</param>
        /// <returns></returns>
        [Rpc(SendTo.Server)]
        public void AddVertexRpc(ulong segmentId, Vector3 position) {
            VertexLookup segmentLookup = _segmentTable.Find(seg => seg.Id == segmentId);
            if (segmentLookup == null) return;
            LineSegment segment = segmentLookup.LineComp;
            int start = segment.mVStart;
            int next = segment.mVEnd;
            _vertexTable.ForEach(item => {
                if (item.Vertex > start) {
                    item.Vertex++;
                    if (item.LineComp != null) {
                        item.LineComp.mVStart++;
                        if (item.LineComp.mVEnd != 0) {
                            item.LineComp.mVEnd++;
                        }
                    }
                }
                if (_mLr && item.LineComp.mVStart == start) {
                    item.LineComp.mVEnd = start + 1;
                }
                if (_mLr && item.LineComp.mVEnd > _vertexTable.Count)
                    item.LineComp.mVEnd = 0;
            });
            start++;
            int end = next;
            if (end != 0)
                end++;
            segment.MoveEnd(position);
            Datapoint vertex = _createVertex(position, start);
            Curve.InsertVertex(position, start);
            _createSegment(position, _vertexTable.Find(item => item.Vertex == end).VertexComp.transform.position, start, end == 0);
            transform.parent.SendMessage("AddVertex", position, SendMessageOptions.DontRequireReceiver);
            vertex.UnSelected(SelectionType.SELECT);
        }

        public override void RemoveVertex(Transform vertex = null)
        {
            if (vertex == null) return;
            ulong vID = vertex.GetComponent<VirgisFeature>().GetId();
            if (MState.BlockMove)
            {
                RemoveFeatureRpc();
            }
            else
            {
                RemoveLineVertexRpc(vID);
            }
        }

        [Rpc(SendTo.Server)]
        private void RemoveLineVertexRpc(ulong vertex)
        {
            VertexLookup vLookup = _vertexTable.Find(item => item.Id == vertex);
            if (vLookup == null) return;
            if (GetParent(out IVirgisEntity parent)) parent.RemoveVertex(vLookup.VertexComp.transform);
            int thisVertex = vLookup.Vertex;
            if (vLookup.LineComp != null)
            {
                vLookup.LineComp.RemoveFeatureRpc();
            }
            else
            {
                _vertexTable.Find(item => item.Vertex == vLookup.Vertex - 1).LineComp.RemoveFeatureRpc();
            }
            vLookup.VertexComp.RemoveFeatureRpc();
            _vertexTable.Remove(vLookup);
            Curve.RemoveVertex(thisVertex);
            _vertexTable.ForEach(item =>
            {
                if (item.Vertex >= thisVertex)
                {
                    item.Vertex--;
                    if (item.LineComp != null)
                    {
                        item.LineComp.mVStart--;
                        if (item.LineComp.mVEnd != 0)
                        {
                            item.LineComp.mVEnd--;
                        }
                    }
                }
                if (_mLr && item.LineComp.mVEnd >= _vertexTable.Count)
                {
                    item.LineComp.mVEnd = 0;
                }
            });
            int end = thisVertex;
            int start = thisVertex - 1;
            if (_mLr && thisVertex >= _vertexTable.Count)
                end = 0;
            if (_mLr && thisVertex == 0)
                start = _vertexTable.Count - 1;
            Debug.Log($"start : {start}, End : {end}");
            if (_vertexTable.Count > 1)
            {
                _vertexTable.Find(item => item.Vertex == start).LineComp.MoveEnd(_vertexTable.Find(item => item.Vertex == end).VertexComp.transform.position);
            }
            else
            {
                RemoveFeatureRpc();
            }
        }

        private Datapoint _createVertex(Vector3 vertex, int i) {
            GameObject handle = Instantiate(_mHandlePrefab, vertex, Quaternion.identity, transform );
            Datapoint com = handle.GetComponent<Datapoint>();
            com.Spawn(transform);
            com.SetMaterial(_mPointHash);
            _vertexTable.Add(new VertexLookup() { Id = com.GetId(), Vertex = i, VertexComp = com });
            handle.transform.localScale = Symbology.TryGetValue("point", out UnitPrototype value) ? value.Transform.Scale : Vector3.one;
            return com;
        }

        private LineSegment _createSegment(Vector3 start, Vector3 end, int i, bool close) {
            GameObject lineSegment = Instantiate(cylinderObject, start, Quaternion.identity, transform);
            LineSegment com = lineSegment.GetComponent<LineSegment>();
            com.Spawn(transform);
            com.SetMaterial(_mLineHash);
            com.Draw(start, end, i, i + 1, Symbology["line"].Transform.Scale.Magnitude);
            if (close)
                com.mVEnd = 0;
            _vertexTable.Find(item => item.Vertex == i).LineComp = com;
            _segmentTable.Add(new VertexLookup() { Id = com.GetId(), LineComp = com });
            return com;
        }

        /// <summary>
        /// get the center of the line
        /// 
        /// <returns></returns>
        /// </summary>
        private Vector3 Center() {
            return (Vector3) Curve.CenterMark();
        }

        private Vector3 _labelPosition() {
            return Center() + transform.TransformVector(Vector3.up) * Symbology["line"].Transform.Scale.Magnitude;
        }

        public VirgisFeature GetVertexById(int vID) 
        {
            VertexLookup vertex = _vertexTable.Find(vertex => vertex.Vertex == vID);
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
