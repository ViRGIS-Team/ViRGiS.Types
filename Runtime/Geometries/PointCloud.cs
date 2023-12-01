using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Virgis
{

    public class PointCloud : VirgisFeature
    {
        public SerializableBakedPointCloud Bpc = new();

        public new void Start(){
            base.Start();

        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Bpc.OnValueChanged += SetBpc;
            if (Bpc.width != 0 ) SetBpc( Bpc.PositionMap, Bpc.ColorMap, Bpc.PointCount);
        }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkSpawn();
        Bpc.OnValueChanged -= SetBpc;
    }
        
        public void SetBpc( Texture2D positions, Texture2D colors, int PointCount) {
            VisualEffect vfx = GetComponent<VisualEffect>();
            // Sort out the point size
            RecordSetPrototype layer = GetLayer()?.GetMetadata();
            Dictionary<string,UnitPrototype> Symbology = layer.Units;

            // load the VFX and fire
            vfx.SetTexture("_Positions", positions);
            vfx.SetTexture("_Colors", colors);
            vfx.SetInt("_pointCount", PointCount);
            if ( ! Symbology.TryGetValue("point", out UnitPrototype pointSymbology)) 
                vfx.SetVector3("_size", pointSymbology.Transform.Scale);
            vfx.Play();
        }
        
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
