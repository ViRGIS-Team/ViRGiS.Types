using System.Threading.Tasks;
using UnityEngine;

namespace Virgis
{
    public interface IVirgisLoader
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

        GameObject GetFeatureShape();
        void SetMetadata(RecordSetPrototype layer);
    }
    
    public class VirgisLoader<S> : VirgisLayer,  IVirgisLoader
    {
        public S features; // holds the feature data for this layer
        protected VirgisLayer m_parent; // holds the parent VirgisLayer
        protected object m_crs;

        protected new void Awake()
        {
            base.Awake();
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
    }
}
