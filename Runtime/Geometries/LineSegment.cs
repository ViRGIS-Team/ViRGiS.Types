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
using UnityEngine.Serialization;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a line segment
    /// </summary>
    public class LineSegment : VirgisFeature
    {

        private Vector3 _mStart; // coords of the start of the line in Map.local space coordinates
        private Vector3 _mEnd;  // coords of the start of the line in Map.local space coordinates
        private float _mDiameter; // Diameter of the vertex in Map.local units
        [FormerlySerializedAs("m_vStart")] public int mVStart; // Vertex ID of the start of the line
        [FormerlySerializedAs("m_vEnd")] public int mVEnd; // Vertex ID of the end of the line
        private Transform _mShape;
        private Datapoint _mSelectedVertex;


        public new void Start()
        {
            _mShape = transform.GetChild(0);
            if (_mShape.TryGetComponent(out meshRenderer)) MMaterial = meshRenderer.material;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _mShape = transform.GetChild(0);
            if (_mShape.TryGetComponent(out meshRenderer)) MMaterial = meshRenderer.material;
        }

        /// <summary>
        /// Called to draw the line Segment 
        /// </summary>
        /// <param name="from">starting point of the line segment in worldspace coords</param>
        /// <param name="to"> end point for the line segment in worldspace coordinates</param>
        /// <param name="vertStart">vertex ID for the vertex at the start of the line segment</param>
        /// <param name="vertEnd"> vertex ID for the vertex at the end of the line segment </param>
        /// <param name="dia">Diameter of the line segement in Map.local units</param>
        public void Draw(Vector3 from, Vector3 to, int vertStart, int vertEnd, float dia)
        {
            _mStart = transform.parent.InverseTransformPoint(from);
            _mEnd = transform.parent.InverseTransformPoint(to);
            _mDiameter = dia;
            mVStart = vertStart;
            mVEnd = vertEnd;
            _draw();
        }

        // Move the start of line to newStart point in World Coords
        public void MoveStart(Vector3 newStart)
        {
            _mStart = transform.parent.InverseTransformPoint(newStart);
            _draw();
        }

        // Move the start of line to newStart point in World Coords
        public void MoveEnd(Vector3 newEnd)
        {
            _mEnd = transform.parent.InverseTransformPoint(newEnd);
            _draw();
        }

        private void _draw()
        {

            transform.localPosition = _mStart;
            transform.LookAt(transform.parent.TransformPoint(_mEnd));
            float length = Vector3.Distance(_mStart, _mEnd) / 2.0f;
            Vector3 linescale = transform.parent.localScale;
            transform.localScale = new Vector3(_mDiameter / linescale.x, _mDiameter / linescale.y, length);
        }

        protected override void _moveAxis(MoveArgs args){
            args.pos = transform.position;
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
        }

        protected override void _move(MoveArgs args){
            if (MState.BlockMove)
                SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
        }

        public override void AddVertex(Vector3 position) {
            GetComponentInParent<Dataline>().AddVertexRpc( GetId(), position);
        }

        public override void Selected(SelectionType button)
        {
            base.Selected(button);
            float dist1 = (MState.LastHit - _mStart).sqrMagnitude;
            float dist2 = (MState.LastHit - _mEnd).sqrMagnitude;
            int selected = -1;
            if (dist1 < dist2 * .5f) selected = mVStart;
            if (dist2 < dist1 * .5f) selected = mVEnd;
            if (selected == -1) return;
            if (!GetParent(out IVirgisEntity parent)) return;
            _mSelectedVertex = (parent as Dataline)?.GetVertexById(selected) as Datapoint;
            _mSelectedVertex?.Selected(button);
        }

        public override void MoveTo(MoveArgs args)
        {
            if (_mSelectedVertex)
            {
                _mSelectedVertex.MoveTo(args);
            }
        }

        public override void RemoveVertex(Transform vertex = null)
        {
            if (_mSelectedVertex != null)
            {
                _mSelectedVertex.RemoveVertex(_mSelectedVertex.transform);
            }
        }
    }
}
