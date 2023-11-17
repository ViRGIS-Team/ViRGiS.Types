using System;
using Unity.Netcode;
using UnityEngine;

namespace Virgis
{
    [Serializable]
    public class SerializeableMesh : NetworkVariableBase
    {
        public Mesh mesh;

        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate( Mesh newValue);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// Sets the <see cref="Value"/>, marks the <see cref="NetworkVariable{T}"/> dirty, and invokes the <see cref="OnValueChanged"/> callback
        /// if there are subscribers to that event.
        /// </summary>
        /// <param name="value">the new value of type `T` to be set/></param>
        public void Set(Mesh value)
        {
            SetDirty(true);
            mesh = value;
            OnValueChanged?.Invoke( mesh);
        }

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            if (mesh == null)
            {
                writer.WriteValueSafe<int>(0);
                return;
            }
            // Serialize the data we need to synchronize
            writer.WriteValueSafe(mesh.vertexCount);
            int[] tris = mesh.triangles;
            writer.WriteValueSafe(tris.Length);
            writer.WriteValueSafe(mesh.vertices);
            writer.WriteValueSafe(mesh.normals);
            writer.WriteValueSafe(mesh.colors);
            writer.WriteValueSafe(mesh.uv);
            writer.WriteValueSafe(tris);
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            mesh = new();
            reader.ReadValueSafe(out int vertexCount);
            if (vertexCount == 0) return;
            reader.ReadValueSafe(out int triCount);
            Vector3[] vertices = new Vector3[vertexCount];
            reader.ReadValueSafe(out vertices);
            Vector3[] normals = new Vector3[vertexCount];
            reader.ReadValueSafe(out normals);
            Color[] colors = new Color[vertexCount];
            reader.ReadValueSafe(out colors);
            Vector2[] uvs = new Vector2[vertexCount];
            reader.ReadValueSafe(out uvs);
            int[] tris = new int[triCount];
            reader.ReadValueSafe(out tris);
            Mesh tmesh = new();
            tmesh.SetVertices(vertices);
            tmesh.SetNormals(normals);
            tmesh.SetColors(colors);
            tmesh.SetUVs(0,uvs);
            tmesh.SetTriangles(tris, 0);
            mesh = tmesh;
            OnValueChanged?.Invoke(mesh);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // Don'thing for this example
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            // Don'thing for this example
        }
    }
}
