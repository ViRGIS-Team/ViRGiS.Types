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

using System.Collections.Generic;
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a line segment
    /// </summary>
    public class LineSegment : VirgisFeature
    {

        private Vector3 m_Start; // coords of the start of the line in Map.local space coordinates
        private Vector3 m_End;  // coords of the start of the line in Map.local space coordinates
        private float m_Diameter; // Diameter of the vertex in Map.local units
        public int m_vStart; // Vertex ID of the start of the line
        public int m_vEnd; // Vertex ID of the end of the line
        private Transform m_Shape;

        public new void Start()
        {
            m_Shape = transform.GetChild(0);
            if (m_Shape.TryGetComponent<MeshRenderer>(out m_Mr)) m_Material = m_Mr.material;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_Shape = transform.GetChild(0);
            if (m_Shape.TryGetComponent<MeshRenderer>(out m_Mr)) m_Material = m_Mr.material;
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
            m_Start = transform.parent.InverseTransformPoint(from);
            m_End = transform.parent.InverseTransformPoint(to);
            m_Diameter = dia;
            m_vStart = vertStart;
            m_vEnd = vertEnd;
            _draw();
        }

        // Move the start of line to newStart point in World Coords
        public void MoveStart(Vector3 newStart)
        {
            m_Start = transform.parent.InverseTransformPoint(newStart);
            _draw();
        }

        // Move the start of line to newStart point in World Coords
        public void MoveEnd(Vector3 newEnd)
        {
            m_End = transform.parent.InverseTransformPoint(newEnd);
            _draw();
        }

        private void _draw()
        {

            transform.localPosition = m_Start;
            transform.LookAt(transform.parent.TransformPoint(m_End));
            float length = Vector3.Distance(m_Start, m_End) / 2.0f;
            Vector3 linescale = transform.parent.localScale;
            transform.localScale = new Vector3(m_Diameter / linescale.x, m_Diameter / linescale.y, length);
        }

        protected override void _moveAxis(MoveArgs args){
            args.pos = transform.position;
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
        }

        protected override void _move(MoveArgs args){
            if (m_State.BlockMove)
                SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
        }

        public override void AddVertexRpc(Vector3 position) {
            GetComponentInParent<Dataline>().AddVertex( this, position);
        }

        public void Delete() {
            transform.parent.SendMessage("RemoveVertex", this, SendMessageOptions.DontRequireReceiver);
        }

        public override Dictionary<string, object> GetInfo() {
            return GetComponentInParent<Dataline>().GetInfo(this);
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new System.NotImplementedException();
        }
    }
}
