
/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby gr anted, free of charge, to any person obtaining a copy
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

using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;


namespace Virgis
{
    /// <summary>
    /// Controls an instance of a data point handle
    /// </summary>
    public class Datapoint : VirgisFeature
    {
        private Renderer thisRenderer; // convenience link to the rendere for this marker


        private void Start() {
            mainMat = GetMaterial("point");
            selectedMat = GetMaterial("point_sel");
            if (transform.childCount > 0)
                label = transform.GetChild(0);
        }
        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        void Update()
        {
            if (label) label.LookAt(State.instance.mainCamera.transform);
        }


        public override void Selected(SelectionType button){
            base.Selected(button);
            thisRenderer.material = selectedMat;
        }


        public override void UnSelected(SelectionType button){
            base.Selected(button);
            thisRenderer.material = mainMat;
            if (button != SelectionType.BROADCAST){
                MoveArgs args = new MoveArgs();
                switch (State.instance.EditSession.mode){
                    case EditSession.EditMode.None:
                        break;
                    case EditSession.EditMode.SnapAnchor:
                        LayerMask layerMask = UnityLayers.POINT;
                        List<Collider> hitColliders = Physics.OverlapBox(transform.position, transform.TransformVector(Vector3.one / 2 ), Quaternion.identity, layerMask).ToList().FindAll( item => item.transform.position != transform.position);
                        if (hitColliders.Count > 0)
                        {
                            args.oldPos = transform.position;
                            args.pos = hitColliders.First<Collider>().transform.position;
                            args.id = GetId();
                            args.translate = args.pos - args.oldPos;
                            SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
                        }
                        break;
                    case EditSession.EditMode.SnapGrid:
                        args.oldPos = transform.position;
                        args.pos = transform.position.Round(State.instance.map.transform.TransformVector(Vector3.one * (State.instance.project.ContainsKey("GridScale") && State.instance.project.GridScale != 0 ? State.instance.project.GridScale :  1f)).magnitude);;
                        args.id = GetId();
                        args.translate = args.pos - transform.position;
                        SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
                        break;
                }
            }
        }

        public override void MoveTo(MoveArgs args) {
            if (args.translate != Vector3.zero) {
                args.id = GetId();
                transform.parent.SendMessage("Translate", args, SendMessageOptions.DontRequireReceiver);
            } else if (args.pos != Vector3.zero && args.pos != transform.position) {
                args.id = GetId();
                args.translate = args.pos - transform.position;
                transform.parent.SendMessage("Translate", args, SendMessageOptions.DontRequireReceiver);
            }

        }

        /// <summary>
        ///  Sent by the parent entity to request this marker to move as part of an entity move
        /// </summary>
        /// <param name="argsin">MoveArgs</param>
        void TranslateHandle(MoveArgs argsin) {
            if (argsin.id == GetId()) {
                MoveArgs argsout = new MoveArgs();
                argsout.oldPos = transform.position;
                transform.Translate(argsin.translate, Space.World);
                argsout.id = GetId();
                argsout.pos = transform.position;
                SendMessageUpwards("VertexMove", argsout, SendMessageOptions.DontRequireReceiver);
            }
        }


        public override void MoveAxis(MoveArgs args) {
            args.pos = transform.position;
            base.MoveAxis(args);
        }


        public override VirgisFeature GetClosest(Vector3 coords, Guid[] excludes) {
            return this;
        }

        public void Delete() {
            transform.parent.SendMessage("RemoveVertex", this, SendMessageOptions.DontRequireReceiver);
        }


        public override Dictionary<string, object> GetInfo() {
            return GetLayer().GetInfo(this);
            //if (meta == default) {
            //    meta = feature.GetAll();
            //    Geometry geom = (gameObject.transform.position.ToGeometry());
            //    string wkt;
            //    try {
            //        GetLayer<IVirgisLayer>().GetCrs().ExportToWkt(out wkt, null);
            //        geom.TransformTo(GetLayer<IVirgisLayer>().GetCrs());
            //    } catch { }
            //    double[] coords = new double[3];
            //    geom.GetPoint(0, coords);
            //    meta.Add("X Coordinate", coords[0].ToString());
            //    meta.Add("Y Coordinate", coords[1].ToString());
            //    meta.Add("Z Coordinate", coords[2].ToString());
            //    geom.Dispose();
            //}
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}
