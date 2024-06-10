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
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace Virgis
{
    public interface IVirgisLoader : IVirgisLayer
    {
        /// <summary>
        /// Called to add feature to the layer dataset
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="geometry"></param>
        /// <returns></returns>
        IVirgisFeature _addFeature<T1>(T1 geometry);

        /// <summary>
        /// Called at the Checkpoint Lifecycle Hook
        /// </summary>
        void _checkpoint();

        /// <summary>
        /// Called to Draw the dataset
        /// </summary>
        /// <returns></returns>
        Task _draw();

        /// <summary>
        /// Implement the layer specific init code in this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        Task _init();

        /// <summary>
        /// Implement the layer specific save code in this method
        /// </summary>
        /// <returns></returns>
        Task _save();

        void _set_visible();
    }
    
    public class VirgisLoader<S> : NetworkBehaviour, IVirgisLoader
    {
        public S features; // holds the feature data for this layer
        protected VirgisLayer m_parent; // holds the parent VirgisLayer
        protected object m_crs;
        protected Dictionary<string, SerializableMaterialHash> m_materials = new();
        protected float m_displacement;

        public RecordSetPrototype _layer
        {
            get
            { return m_parent?.GetMetadata(); }
            set
            { if (m_parent != null) m_parent.SetMetadata(value); }
        }

        public string sourceName { get 
            { return m_parent?.sourceName;}
            set 
            { if ( m_parent != null) m_parent.sourceName = value;} 
        }

        public List<IVirgisLayer> subLayers
        {
            get { return m_parent?.subLayers;}
            }

        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed
        {
            get
            {
                return m_parent?.changed ?? false;
            }
            set
            {
                if (m_parent != null) m_parent.changed = value;
            }
        }

        public bool isContainer
        {
            get
            { return m_parent?.isContainer ?? false; }
        }

        public FeatureType featureType => throw new NotImplementedException();

        public bool IsEditable { get => m_parent?.IsEditable ?? false; }

        public bool IsWriteable { 
            get {
                return m_parent?.IsWriteable ?? false;
            } 
            set {
                if (m_parent != null) m_parent.IsWriteable = value;
            } }

        protected IVirgisLoader m_loader;

        protected void Awake()
        {
            m_parent = GetComponent<VirgisLayer>();
        }

        public virtual IVirgisFeature _addFeature<T>(T geometry)
        {
            throw new System.NotImplementedException();
        }

        public virtual void _checkpoint()
        {
            throw new System.NotImplementedException();
        }

        public virtual Task _draw()
        {
            throw new System.NotImplementedException();
        }

        public virtual Task _init()
        {
            throw new System.NotImplementedException();
        }

        public virtual Task _save()
        {
            throw new System.NotImplementedException();
        }

        public void SetFeatures(S features)
        {
            this.features = features;
        }

        /// <summary>
        /// Set the Layer CRS
        /// </summary>
        /// <param name="crs">SpatialReference</param>
        public void SetCrs(object crs)
        {
            m_crs = crs;
        }

        public object GetCrsRaw()
        {
            return m_crs;
        }

        public virtual void _set_visible()
        {
            throw new NotImplementedException();
        }

        public virtual VirgisFeature AddFeature<T>(T geometry)
        {
            throw new NotImplementedException();
        }

        public virtual bool Load(string file)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerator Init(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task AsyncInit(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task SubInit(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task Draw()
        {
            throw new NotImplementedException();
        }

        public virtual void CheckPoint()
        {
            throw new NotImplementedException();
        }

        public virtual Task<RecordSetPrototype> Save()
        {
            throw new NotImplementedException();
        }

        public virtual VirgisFeature GetFeature(Guid id)
        {
            throw new NotImplementedException();
        }

        public virtual Shapes GetFeatureShape()
        {
            return Shapes.None;
        }

        public virtual RecordSetPrototype GetMetadata()
        {
            return _layer;
        }

        public virtual void SetMetadata(RecordSetPrototype meta)
        {
            _layer = meta;
        }

        public virtual void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsVisible()
        {
            throw new NotImplementedException();
        }

        public virtual void SetEditable(bool inSession)
        {
            throw new NotImplementedException();
        }


        public void MessageUpwards(string method, object args)
        {
            throw new NotImplementedException();
        }

        public void Selected(SelectionType button)
        {
            //Do Nothing
        }

        public void UnSelected(SelectionType button)
        {
            // Do Nothing
        }

        public Guid GetId()
        {
            return m_parent?.GetId() ?? Guid.Empty;
        }

        public VirgisFeature GetClosest(Vector3 coords, Guid[] exclude)
        {
            throw new NotImplementedException();
        }

        public void MoveAxis(MoveArgs args)
        {
            // do nothing
        }

        public void Translate(MoveArgs args)
        {
            // do nothing
        }

        public void MoveTo(MoveArgs args)
        {
            // do nothing
        }

        public void VertexMove(MoveArgs args)
        {
            // do nothing
        }

        public IVirgisLayer GetLayer()
        {
            return m_parent;
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
            m_parent?.SetInfo(meta);
        }

        public Dictionary<string, object> GetInfo(VirgisFeature feat)
        {
            throw new NotImplementedException();
        }

        public virtual void SetMaterial(string idx, Color color, Dictionary<string, float> properties = null)
        {
            throw new NotImplementedException();
        }

        public Material GetMaterial(string idx)
        {
            throw new NotImplementedException();
        }

        public void Loaded(VirgisLayer layer)
        {
            throw new NotImplementedException();
        }
    }
}
