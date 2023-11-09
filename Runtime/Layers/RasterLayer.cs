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

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace Virgis
{
    public class RasterLayer : VirgisLayer
    {
        // The prefab for the data points to be instantiated

        public GameObject pointCloud;
        public List<GameObject> meshes;

        private VisualEffect m_vfx;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.RASTER;
        }

        public async override Task Init(RecordSetPrototype layer)
        {
            await base.Init(layer);
            m_vfx = gameObject.GetComponentInChildren<VisualEffect>();
        }

        public override void _set_visible() {
            base._set_visible();
            m_loader._set_visible();
        }


        public override void Translate(MoveArgs args)
        {

            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            changed = true;
        }


        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
        {
            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
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
                    if (T.GetComponent<Datapoint>() != null)
                    {
                        T.localScale /= RS;
                    }
                }
                m_vfx.SetVector3("_scale", transform.localScale);
            }
            changed = true;
        }
    }
}
