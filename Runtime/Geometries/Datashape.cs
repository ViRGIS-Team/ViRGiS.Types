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
using VirgisGeometry;
using System.Linq;
using System;

namespace Virgis
{
    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Datashape : VirgisFeature {

        public GameObject shapePrefab;
        protected GameObject Shape; // gameObject to be used for the shape
        protected List<Dataline> m_Lines = new();
        protected List<DCurve3> m_Polygon = new();
        protected float m_ScaleX;
        protected float m_ScaleY;

        public override void Start()
        {
            base.Start();
        }

        public override void Selected(SelectionType button) {
            if (button == SelectionType.SELECTALL) {
                gameObject.BroadcastMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_SetBlockMove(true);
                GetComponentsInChildren<Dataline>().ToList<Dataline>().ForEach(item => item.Selected(SelectionType.SELECTALL));
            }
        }

        public override void UnSelected(SelectionType button) {
            if (button != SelectionType.BROADCAST) {
                gameObject.BroadcastMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_SetBlockMove(false);
            }
        }

        protected override void _move(MoveArgs args) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Makes the actual mesh
        /// </summary>
        protected void _redraw()
        {
            if (m_Lines.Count > 0)
            {
                m_Polygon = new List<DCurve3>();
                foreach (Dataline ring in m_Lines)
                {
                    m_Polygon.Add(ring.Curve); // Note that Polygon is in World Coordinates
                }
            }

            //
            // Map 3d Polygon to the bext fit 2d polygon and also return the frame used for the mapping
            //
            Frame3f frame;
            IEnumerable<Vector3d> verticesItr;
            GeneralPolygon2d polygon2d = new(m_Polygon, out frame, out verticesItr );

            Index3i[] trianglesItr = polygon2d.GetMesh();


            DMesh3 dmesh = DMesh3Builder.Build<Vector3d, Index3i, Vector3d>(verticesItr, trianglesItr, null, null, m_Polygon[0].axisOrder);
            dmesh.CalculateUVs();
            Shape.GetComponent<DataMesh>().umesh.Value = dmesh;
        }

        public override void AddVertexRpc(Vector3 position) {
            _redraw();
            base.AddVertexRpc(position);
        }

        public override void RemoveVertexRpc(VirgisFeature vertex) {
            if (m_State.BlockMove) {
                Destroy(gameObject);
            } else {
                _redraw();
            }
        }

        public override Dictionary<string, object> GetInfo() {
            return default;
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}