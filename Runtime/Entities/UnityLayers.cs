using UnityEngine;

namespace Virgis
{
    /// <summary>
    /// mapping from Virgis types to Unity Layer Mask
    /// </summary>
    public static class UnityLayers
    {
        public static LayerMask POINT
        {
            get
            {
                return LayerMask.GetMask("Pointlike Entities");
            }
        }
        public static LayerMask LINE
        {
            get
            {
                return LayerMask.GetMask("Linelike Entities");
            }
        }
        public static LayerMask SHAPE
        {
            get
            {
                return LayerMask.GetMask("Shapelike Entities");
            }
        }
        public static LayerMask MESH
        {
            get
            {
                return LayerMask.GetMask("Meshlike Entities");
            }
        }
    }
}
