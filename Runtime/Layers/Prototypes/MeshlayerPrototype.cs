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

using System.Linq;
using UnityEngine;

namespace Virgis
{
    public abstract class MeshlayerPrototype : VirgisLayer
    {
        // The prefab for the data points to be instantiated
        public GameObject Mesh;
        public Material MeshMaterial;
        public Material WireframeMaterial;
        public Material ImageMaterial;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.MESH;
        }
       
        public override void Translate(MoveArgs args) {
            changed = true;
        }

        protected override void _moveAxis(MoveArgs args) {
            changed = true;
            EditableMesh[] dataFeatures = gameObject.GetComponentsInChildren<EditableMesh>();
            dataFeatures.ToList<EditableMesh>().Find(item => args.id == item.GetId()).MoveAxisAction(args);
        }

        protected override void _set_editable() {
            base._set_editable();
            if (State.instance.InEditSession()) {
                if (IsWriteable) {
                    EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
                    foreach (EditableMesh mesh in meshes) {
                        mesh.OnEdit(true);
                    }
                } else {
                    EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
                    foreach (EditableMesh mesh in meshes) {
                        mesh.OnEdit(false);
                    }
                }
            }
        }
    }
}