using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Virgis
{

    public class PointCloud : VirgisFeature
    {
        public SerializableBakedPointCloud Bpc = new();
        public VisualEffect VFX;

        public new void Start(){
            base.Start();

        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Bpc.OnValueChanged += SetBpc;
            if (Bpc.PointCount != 0 ) SetBpc( Bpc.PositionMap, Bpc.ColorMap, Bpc.PointCount, Bpc.PixelSize);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkSpawn();
            Bpc.OnValueChanged -= SetBpc;
        }
        
        public void SetBpc( Texture2D positions, Texture2D colors, int PointCount, float PixelSize) {

            // load the VFX and fire
            VFX.SetTexture("_Positions", positions);
            VFX.SetTexture("_Colors", colors);
            VFX.SetInt("_pointCount", PointCount);
            VFX.SetVector3("_size", Vector3.one * PixelSize);
            VFX.Play();
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
