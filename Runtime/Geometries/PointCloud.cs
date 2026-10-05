using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace Virgis
{

    public class PointCloud : VirgisFeature
    {
        [FormerlySerializedAs("Bpc")] public SerializableBakedPointCloud bpc = new();
        [FormerlySerializedAs("VFX")] public VisualEffect vfx;

        public new void Start(){
            base.Start();

        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            bpc.OnValueChanged += SetBpc;
            if (bpc.PointCount != 0 ) SetBpc( bpc.PositionMap, bpc.ColorMap, bpc.PointCount, bpc.PixelSize);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkSpawn();
            bpc.OnValueChanged -= SetBpc;
        }

        private void SetBpc( Texture2D positions, Texture2D colors, int pointCount, float pixelSize) {

            // load the VFX and fire
            vfx.SetTexture("_Positions", positions);
            vfx.SetTexture("_Colors", colors);
            vfx.SetInt("_pointCount", pointCount);
            vfx.SetVector3("_size", Vector3.one * pixelSize);
            vfx.Play();
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
