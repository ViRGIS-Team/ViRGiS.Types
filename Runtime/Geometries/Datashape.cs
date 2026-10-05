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
using System.Diagnostics.CodeAnalysis;

namespace Virgis
{
    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component. The DataShape component is not editable and assumes that the MLines perimeter is correctly setup. This type is an abstract base class
    /// </summary>
    public abstract class Datashape : VirgisFeature {

        public GameObject shapePrefab;
        protected GameObject Shape; // gameObject to be used for the shape
        public List<Dataline> Lines { get; protected set; } = new();
        public List<DCurve3> Polygon { get; protected set; } = new();


        public override void Selected(SelectionType button) {
            if (button == SelectionType.SELECTALL) {
                gameObject.BroadcastMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_SetBlockMove(true);
                GetComponentsInChildren<Dataline>().ToList().ForEach(item => item.Selected(SelectionType.SELECTALL));
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
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        protected void _redraw()
        {
            if (Lines.Count > 0)
            {
                Polygon = new List<DCurve3>();
                foreach (Dataline ring in Lines)
                {
                    Polygon.Add(ring.Curve); // Note that Polygon is in World Coordinates
                }
            }

            //
            // Map 3d Polygon to the bext fit 2d polygon and also return the frame used for the mapping
            //
            GeneralPolygon2d polygon2D = new(Polygon, out Frame3f _, out var verticesItr );

            //Triangulate The Polygon
            Index3i[] trianglesItr = polygon2D.GetMesh();

            //Build a DMesh3 from the result
            DMesh3 dmesh = DMesh3Builder.Build<Vector3d, Index3i, Vector3d>(verticesItr, trianglesItr, null, null, Polygon[0].axisOrder);
            dmesh.CalculateUVs();

            //Add DMesh to the component
            DataMesh mesh = Shape.GetComponent<DataMesh>();
            mesh.Umesh.DMesh3 = dmesh;
            mesh.Umesh.MeshFinalize();
        }

        public override void AddVertex(Vector3 position) {
            _redraw();
            base.AddVertex(position);
        }

        public override void RemoveVertex(Transform vertex = null) {
            if (MState.BlockMove) {
                RemoveFeatureRpc();
            } else {
                _redraw();
            }
            base.RemoveVertex(vertex);
        }

        public override void UpdateMaterial(SerializableMaterialHash previousValue, SerializableMaterialHash newValue)
        {
            if (Shape)
            {
                Shape.SendMessage("SetMaterial", newValue);
            }
        }
    }
}