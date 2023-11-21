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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;
using System.Collections;

namespace Virgis {


    /// <summary>
    /// This script initialises the project and loads the Project and Layer data.
    /// 
    /// It is run once at Startup
    /// </summary>
    public abstract class MapInitializePrototype : MonoBehaviour, IVirgisLayer
    {

        protected string m_loadOnStartup;

        public FeatureType featureType => throw new NotImplementedException();

        public string sourceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<IVirgisLayer> subLayers => throw new NotImplementedException();

        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed
        {
            get
            {
                return m_changed;
            }
            set
            {
                m_changed = value;
                IVirgisLayer parent = transform.parent?.GetComponent<IVirgisLayer>();
                if (parent != null) parent.changed = value;
            }
        }
        public bool isContainer { get; protected set; }  // if this is a container layer - do not Draw
        public bool isWriteable
        { // only allow edit and save for layers that can be written
            get;
            set;
        }
        protected Guid m_id;
        protected bool m_editable;
        private bool m_changed;
        private readonly List<IDisposable> m_subs = new List<IDisposable>();

        protected void Start()
        {
            m_subs.Add(State.instance.editSession.StartEvent.Subscribe(_onEditStart));
            m_subs.Add(State.instance.editSession.EndEvent.Subscribe(_onEditStop));
        }

        protected abstract bool _load(string file);

        public abstract bool Load(string file);

        /// <summary>
        /// Override this call to add functionality after the Project has loaded
        /// </summary>
        public abstract void OnLoad();


        public abstract void Add(MoveArgs args);


        /// <summary>
        /// This cll initiates the drawing of the virtual space and calls `Draw ` on each layer in turn.
        /// </summary>
        public async virtual Task Draw()
        {
            foreach (IVirgisLayer layer in State.instance.layers)
            {
                try {
                    await layer.Draw();
                } catch(Exception e) {
                    Debug.LogError($"Project Layer {layer.sourceName} hasfailed to draw :" + e.ToString());
                }
            }
            return;
        }

        protected abstract Task _draw();

        protected abstract void _checkpoint();

        /// <summary>
        /// this call initiates the saving of the whole project and calls `Save` on each layer in turn
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public abstract Task<RecordSetPrototype> Save(bool all = true);

        protected abstract Task _save();


        protected virtual void _onEditStart(bool ignore)
        {
            CheckPoint();
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="save">true if stop and save, false if stop and discard</param>
        protected async virtual void _onEditStop(bool save)
        {
            if (!save) {
                await Draw();
            }
            await Save(save);
    }

        public virtual GameObject GetFeatureShape()
        {
            return null;
        }

        public abstract VirgisFeature AddFeature<T>(T geometry);

        public virtual IEnumerator Init(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task AsyncInit(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public abstract Task SubInit(RecordSetPrototype layer);


        public virtual void CheckPoint()
        {
            //do nothing
        }

        public VirgisFeature GetFeature(Guid id)
        {
            throw new NotImplementedException();
        }

        public RecordSetPrototype GetMetadata()
        {
            throw new NotImplementedException();
        }

        public void SetMetadata(RecordSetPrototype meta)
        {
            throw new NotImplementedException();
        }

        public void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }

        public bool IsVisible()
        {
            throw new NotImplementedException();
        }

        public void SetEditable(bool inSession)
        {
            throw new NotImplementedException();
        }

        public bool IsEditable()
        {
            throw new NotImplementedException();
        }

        public void MessageUpwards(string method, object args)
        {
            throw new NotImplementedException();
        }

        public void Selected(SelectionType button)
        {
            throw new NotImplementedException();
        }

        public void UnSelected(SelectionType button)
        {
            throw new NotImplementedException();
        }

        public Guid GetId()
        {
            throw new NotImplementedException();
        }

        public VirgisFeature GetClosest(Vector3 coords, Guid[] exclude)
        {
            throw new NotImplementedException();
        }

        public void MoveAxis(MoveArgs args)
        {
            throw new NotImplementedException();
        }

        public void Translate(MoveArgs args)
        {
            throw new NotImplementedException();
        }

        public void MoveTo(MoveArgs args)
        {
            throw new NotImplementedException();
        }

        public void VertexMove(MoveArgs args)
        {
            throw new NotImplementedException();
        }

        public IVirgisLayer GetLayer()
        {
            throw new NotImplementedException();
        }

        public void OnEdit(bool inSession)
        {
            throw new NotImplementedException();
        }

        public void Destroy()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetInfo()
        {
            throw new NotImplementedException();
        }

        public void SetInfo(Dictionary<string, object> meta)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetInfo(VirgisFeature feat)
        {
            throw new NotImplementedException();
        }

        public void SetMaterial(Color color, Dictionary<string, float> properties = null)
        {
            throw new NotImplementedException();
        }

        public Material GetMaterial(int idx)
        {
            throw new NotImplementedException();
        }
    }
}
