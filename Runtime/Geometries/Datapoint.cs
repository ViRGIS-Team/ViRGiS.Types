
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
        /// <summary>
        /// sets the label reference
        /// </summary>
        public void Start() {
            base.Start();
            if (transform.childCount > 0)
                Label = transform.GetChild(0);
        }


        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        void Update()
        {
            if (Label) Label.LookAt(State.instance.mainCamera.transform);
        }

        public override void Selected(SelectionType button){
            base.Selected(button);
            m_Mr.material.SetInt("_Selected", 1);
        }


        public override void UnSelected(SelectionType button){
            base.UnSelected(button);
            m_Mr.material.SetInt("_Selected", 0);
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
                            args.translate = args.pos - args.oldPos;
                            MoveTo(args);
                        }
                        break;
                    case EditSession.EditMode.SnapGrid:
                        args.oldPos = transform.position;
                        args.pos = transform.position.Round(State.instance.Map.transform.TransformVector(Vector3.one * (State.instance.GridScale.Get() != 0 ? State.instance.GridScale.Get() :  1f)).magnitude);;
                        args.translate = args.pos - transform.position;
                        MoveTo(args);
                        break;
                }
            }
        }

        protected override void _move(MoveArgs args) {
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
                VertexMove(argsout);
            }
        }


        protected override void _moveAxis(MoveArgs args) {
            args.pos = transform.position;
            base._moveAxis(args);
        }


        public override VirgisFeature GetClosest(Vector3 coords, Guid[] excludes) {
            return this;
        }

        public void Delete() {
            transform.parent.SendMessage("RemoveVertexRpc", this, SendMessageOptions.DontRequireReceiver);
        }


        public override Dictionary<string, object> GetInfo() {
            return GetLayer().GetInfo(this);
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}
