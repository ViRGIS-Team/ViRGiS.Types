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
    /// Controls an instance of a Polygon ViRGIS component for creating non-editable planar images from two DCurve imstances (the tope and the bottom of the plane - which do not need to be vertically aligned) and a texture
    /// </summary>
    public class Dataplane : Datashape
    {
        public string gisId;
        public Dictionary<string, object> GisProperties;
        
        // This is to set the texture on the client side - Draw is only called on the server side
        public override void Start() 
        { 
            base.Start();
            DataMesh com = GetComponentInChildren<DataMesh>();
            if (texture.tex != null) com.SetTexture(texture.tex);
        }

        /// <summary>
        /// Called to draw the Polygon based upon the 
        /// </summary>
        /// <param name="top">DCurev3 for the tope of the plabe</param>
        /// <param name="bottom">DCurev3 for the bottom of the plane></param>
        /// <param name="tex">The texture to be used</param>
        /// <returns></returns>
        public GameObject Draw( DCurve3 top, DCurve3 bottom, Texture2D tex)
        {
            Polygon = new List<DCurve3>();
            Lines = new List<Dataline>();
            Polygon.Add(top);
            for (int i = bottom.VertexCount - 1; i >=0; i--) {
                Polygon[0].AppendVertex(bottom[i]);
            }
            Shape = Instantiate(shapePrefab, transform);
            DataMesh com = Shape.GetComponent<DataMesh>();
            if (!com.Spawn(transform)) throw new Exception("reparenting failed");
            Polygon[0].Closed = true;

            // call the generic polygon draw function from DataShape
            try
            {
                _redraw();
            }
            catch (Exception e)
            {
                RecordSetPrototype temp = GetLayer().GetMetadata();
                Debug.LogError($"Triangulation Error for Layer {temp.DisplayName} in Object {gisId} : {e.Message}");
            }

            com.SetMaterial(MCol.Value);
            com.texture.Set(tex);


            return gameObject;
        }
    }
}