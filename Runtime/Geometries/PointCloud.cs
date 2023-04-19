using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Virgis
{

    public class PointCloud : VirgisFeature
    {
        
        
        
        public override Dictionary<string, object> GetInfo()
        {
            throw new System.NotImplementedException();
        }

        public override void SetInfo(Dictionary<string, object> meta)
        {
            throw new System.NotImplementedException();
        }

        public override T GetGeometry<T>()
        {
            if (typeof(T) != typeof(VisualEffect)) {
                throw new System.NotImplementedException();
            }

            return GetComponent<T>();
        }
    }
}
