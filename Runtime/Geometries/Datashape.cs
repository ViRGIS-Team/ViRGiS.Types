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
        protected List<Dataline> lines = new();
        protected List<DCurve3> Polygon = new();
        protected float scaleX;
        protected float scaleY;

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
            if (lines.Count > 0)
            {
                Polygon = new List<DCurve3>();
                foreach (Dataline ring in lines)
                {
                    Polygon.Add(ring.Curve); // Note that Polygon is in World Coordinates
                }
            }


            //
            // Map 3d Polygon to the bext fit 2d polygon and also return the frame used for the mapping
            //
            Frame3f frame;
            IEnumerable<Vector3d> VerticesItr;
            GeneralPolygon2d polygon2d = new(Polygon, out frame, out VerticesItr );

            Index3i[] triangles = polygon2d.GetMesh();

            //
            // for each vertex in the dalaunay triangulation - map back to a 3d point and also populate the vertex table
            //

            //List<Vector3d> vertices = VerticesItr.Select(vertex => Shape.transform.InverseTransformPoint(vertex)).ToList();

            DMesh3 dmesh = new();
            foreach (Vector3d vertex in VerticesItr) { dmesh.AppendVertex(vertex); };
            foreach (Index3i tri in triangles) { dmesh.AppendTriangle(tri);  };

            // Note that DMesh is in World Coordinates since the DCurve3's were in World Coordinates
            // However, the Unity Mesh must by in object local coordinates
            dmesh.ToLocal(transform);

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

        /// <summary>
        /// Builds the UV values for thw mesh represented by the vertices 
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        protected Vector2[] BuildUVs(Vector3[] vertices) {
            List<Vector2> ret = new();
            List<Vector3d> vertices3d = vertices.ToList<Vector3>().ConvertAll(item => (Vector3d)item);

            //
            // create a UV mapping plane
            // to make image planes work  - we assume that the origin of UV plane is the last vertex
            //
            OrthogonalPlaneFit3 orth = new OrthogonalPlaneFit3(vertices3d);
            Frame3f frame = new Frame3f( vertices[vertices.Length - 1], -1 * orth.Normal);

            //
            // check the orientation of the plane in UV space.
            // for image planes  - we assume that the x direction from the first point to the second point should always be positive
            // if not - reverse the frame
            //
            if (Math.Sign(
                frame.ToPlaneUV((Vector3f) vertices3d[0], 2).x -
                frame.ToPlaneUV((Vector3f) vertices3d[1], 2).x
                ) > -1) {
                frame = new Frame3f(vertices[vertices.Length - 1], orth.Normal);
            }

            //
            // map all of the points to UV space
            //
            foreach (Vector3d v in vertices3d) {
                ret.Add(frame.ToPlaneUV((Vector3f)v,2));
            }

            //
            // normalize UVs to [0..1, 0..1]
            //
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;

            for (int i = 0; i < ret.Count; i++) {
                Vector2 v = ret[i];
                maxX = maxX > v.x ? maxX : v.x;
                minX = minX < v.x ? minX : v.x;
                maxY = maxY > v.y ? maxY : v.y;
                minY = minY < v.y ? minY : v.y;
            }

            scaleX = maxX - minX;
            scaleY = maxY - minY;


            for (int i = 0; i < ret.Count; i++) {
                ret[i] = new Vector2( (ret[i].x - minX) / scaleX, (ret[i].y - minY) / scaleY);
            }
            return ret.ToArray();
        }

        public override Dictionary<string, object> GetInfo() {
            return default;
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}