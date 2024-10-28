/* MIT License

Copyright (c) 2020 - 21 Runette Software

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
using System.Collections.Generic;
using UnityEngine;
using VirgisGeometry;

namespace Virgis
{
    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Dataplane : Datashape
    {
        public string gisId;
        public Dictionary<string, object> gisProperties;

        public override void Start() 
        { 
            base.Start();
            DataMesh com = GetComponentInChildren<DataMesh>();
            if (Texture.tex != null) com.SetTexture(Texture.tex);
        }

        /// <summary>
        /// Called to draw the Polygon based upon the 
        /// </summary>
        /// <param name="perimeter">LineString defining the perimter of the polygon</param>
        /// <returns></returns>
        public GameObject Draw( DCurve3 top, DCurve3 bottom, Texture2D tex)
        {
            m_Polygon = new List<DCurve3>();
            m_Lines = new List<Dataline>();
            m_Polygon.Add(top);
            for (int i = bottom.VertexCount - 1; i >=0; i--) {
                m_Polygon[0].AppendVertex(bottom[i]);
            }
            Shape = Instantiate(shapePrefab, transform);
            if (!Shape.GetComponent<VirgisFeature>().Spawn(transform)) throw new Exception("reparenting failed");
            m_Polygon[0].Closed = true;

            // call the generic polygon draw function from DataShape
            _redraw();

            GetComponentInChildren<DataMesh>().Texture.Set(tex);

            return gameObject;
        }

        public override Dictionary<string, object> GetInfo() {
            Dictionary<string, object> temp = new Dictionary<string, object>(gisProperties);
            temp.Add("ID", gisId);
            return temp;
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new System.NotImplementedException();
        }
    }
}