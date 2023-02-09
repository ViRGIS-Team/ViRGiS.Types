using System;


namespace Virgis
{
    /// <summary>
    /// Structure used to hold avertex for an arbitrary shape and to calculate equality
    /// </summary>
    public class VertexLookup
    {
        public Guid Id;
        public int Vertex;
        public bool isVertex;
        public VirgisFeature Com;
        public LineSegment Line;
        public int pVertex;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            VertexLookup com = obj as VertexLookup;
            if (com == null) return false;
            else return Equals(com);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public bool Equals(VertexLookup other)
        {
            if (other == null) return false;
            return (this.Id.Equals(other.Id));
        }

        public int CompareTo(VertexLookup other)
        {
            if (other == null)
                return 1;

            else
                return Vertex.CompareTo(other.Vertex);
        }
    }
}