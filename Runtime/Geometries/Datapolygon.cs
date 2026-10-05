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
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Virgis
{
    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component for editable polygon shapes. Expects to be given a valid set of DataLine perimeters in the Draw Function.
    /// </summary>
    public class Datapolygon : Datashape {

        public override void VertexMove(MoveArgs data) {
            if (!MState.BlockMove) {
                _redraw();
            }
            base.VertexMove(data);
        }

        public override void Translate(MoveArgs args) {
            if (MState.BlockMove) {
                transform.Translate(args.translate, Space.World);
            } else
            {
                BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        protected override void _moveAxis(MoveArgs args) {
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
            transform.GetComponentsInChildren<Dataline>().ToList().ForEach(line => line.MoveAxisAction(args));
            Shape.transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            Shape.transform.RotateAround(args.pos, axis, angle);
            Vector3 a = Shape.transform.localPosition;
            Vector3 b = transform.InverseTransformPoint(args.pos);
            Vector3 c = a - b;
            float rs = args.scale;
            Vector3 fp = b + c * rs;
            if (!(fp.magnitude < float.MaxValue)) return;
            Shape.transform.localScale = Shape.transform.localScale * rs;
            Shape.transform.localPosition = fp;
        }

        /// <summary>
        /// Called to draw the Polygon based upon the 
        /// <param name="polygon">List<Dataline> defining the perimeter of the polygons</param>
        /// <param name="mat"> List of materials</param>
        /// <returns></returns>
        /// </summary>
        [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
        public GameObject Draw(List<Dataline> polygon, Dictionary<string, SerializableMaterialHash> mat) {

            Shape = Instantiate(shapePrefab, transform, false);
            VirgisFeature com = Shape.GetComponent<VirgisFeature>();
            com.Spawn(transform);
            if (!mat.TryGetValue("body", out SerializableMaterialHash bodyHash))
                bodyHash = new();
            com.SetMaterial(bodyHash);
            Lines = polygon;

            // call the generic polygon draw function in DataShape
            try
            {
                _redraw();
            }
            catch (Exception e)
            {
                RecordSetPrototype temp = GetLayer().GetMetadata();
                Debug.LogError($"Triangulation Error for Layer {temp.DisplayName} in Object {GetFid<object>()} : {e.Message}");
            }
            return gameObject;
        }
    }
}

