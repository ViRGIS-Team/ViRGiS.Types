using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

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
            get { return m_parent?.subLayers; }
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

        public bool isWriteable
        {
            get
            { return m_parent?.isWriteable ?? false; }
            set 
            { if (m_parent != null) m_parent.isWriteable= value; }
        }

        public FeatureType featureType => throw new NotImplementedException();

        bool IVirgisLayer.isWriteable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

        public virtual Task Init(RecordSetPrototype layer)
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

        public virtual Task<RecordSetPrototype> Save(bool flag)
        {
            throw new NotImplementedException();
        }

        public virtual VirgisFeature GetFeature(Guid id)
        {
            throw new NotImplementedException();
        }

        public virtual GameObject GetFeatureShape()
        {
            return default;
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

        public virtual bool IsEditable()
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
            return m_parent?.GetId() ?? Guid.Empty;
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

        public void SetMaterial(Color color)
        {
            throw new NotImplementedException();
        }

        public Material GetMaterial(int idx)
        {
            throw new NotImplementedException();
        }
    }
}
