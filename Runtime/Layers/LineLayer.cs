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

using System.Linq;
using UnityEngine;

namespace Virgis
{
    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLayer : VirgisLayer
    {
        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject LabelPrefab;
        public Material PointBaseMaterial;
        public Material LineBaseMaterial;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.LINE;
        }

        public override void Translate(MoveArgs args)
        {
            changed = true;
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            dataFeatures.ToList<Dataline>().Find(item => args.id == item.GetId())?.transform.Translate(args.translate, Space.World);
        }

        public override void MoveAxis(MoveArgs args)
        {
            changed = true;
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            dataFeatures.ToList<Dataline>().Find(item => args.id == item.GetId()).MoveAxisAction(args);
        }

        protected override Material MapMaterial(Color color, int idx)
        {
            Material m;
            switch (idx)
            {
                case var _ when idx < 2:
                    m = Instantiate(PointBaseMaterial);
                    break;
                case var _ when idx < 4:
                    m = Instantiate(LineBaseMaterial);
                    break;
                default:
                    m = Instantiate(LineBaseMaterial);
                    break;
            }
            m.SetColor("_BaseColor", color);
            return m;
        }

    }
}
