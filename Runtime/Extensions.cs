
using DelaunatorSharp;
using g3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;

namespace Virgis
{
    public static class DCurveExtensions
    {
        /// <summary>
        /// Creates g3.DCurve from Vector3[]
        /// </summary>
        /// <param name="curve">DCurve</param>
        /// <param name="verteces">Vextor3[]</param>
        /// <param name="bClosed">whether the line is closed</param>
        public static DCurve3 Vector3(this DCurve3 curve, Vector3[] verteces, bool bClosed)
        {
            curve.ClearVertices();
            curve.Closed = bClosed;
            foreach (Vector3 vertex in verteces)
            {
                curve.AppendVertex(vertex);
            }
            return curve;
        }

        /// <summary>
        /// Estimates the 3D centroid of a DCurve 
        /// </summary>
        /// <param name="curve">DCurve</param>
        /// <returns>Vector3[]</returns>
        public static Vector3d Center(this DCurve3 curve)
        {
            Vector3d center = Vector3d.Zero;
            int len = curve.SegmentCount;
            if (!curve.Closed) len++;
            for (int i = 0; i < len; i++)
            {
                center += curve.GetVertex(i);
            }
            center /= len;
            return center;
        }

        /// <summary>
        /// Estimates the nearest point on a DCurve to the centroid of that DCurve
        /// </summary>
        /// <param name="curve">g3.DCurve</param>
        /// <returns>g3.Vector3d Centroid</returns>
        public static Vector3d CenterMark(this DCurve3 curve)
        {
            Vector3d center = curve.Center();
            return curve.GetSegment(curve.NearestSegment(center)).NearestPoint(center);
        }

        /// <summary>
        /// Finds the Segment from the DCurve3 closes to the position
        /// </summary>
        /// <param name="curve">DCurve3</param>
        /// <param name="position">Vector3d</param>
        /// <returns>Integer Sgement index</returns>
        public static int NearestSegment(this DCurve3 curve, Vector3d position)
        {
            _ = curve.DistanceSquared(position, out int iSeg, out double tangent);
            return iSeg;
        }

        /// <summary>
        /// Get a List of Vector3d for all the DCurve3 in the Polygon
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static List<Vector3d> AllVertexItr(this List<DCurve3> poly)
        {
            List<Vector3d> ret = new List<Vector3d>();
            foreach (DCurve3 curve in poly)
            {
                ret.AddRange(curve.Vertices);
            }
            return ret;
        }

        /// <summary>
        /// Converts a DCurve3 in map space coordinates to a List of Vector3 to world space
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="transform">transform to convert the coordinates into</param>
        /// <returns></returns>
        public static List<Vector3> ToVector3(this DCurve3 curve)
        {
            List<Vector3d> verteces = curve.Vertices.ToList();
            List<Vector3> result = new();
            verteces.ForEach(v =>
            {
                result.Add(
                    State.instance.map.transform.TransformPoint((Vector3)v)
                    ) ;
            });
            return result;
        }
    }

    public static class PolygonExtensions
    {

        public static GeneralPolygon2d ToPolygon(this List<DCurve3> list, ref Frame3f frame)
        {
            OrthogonalPlaneFit3 orth = new OrthogonalPlaneFit3(list[0].Vertices);
            frame = new Frame3f(orth.Origin, orth.Normal);
            GeneralPolygon2d poly = new GeneralPolygon2d(new Polygon2d());
            for (int i = 0; i < list.Count; i++)
            {
                List<Vector3d> vertices = list[i].Vertices.ToList();
                List<Vector2d> vertices2d = new List<Vector2d>();
                foreach (Vector3d v in vertices)
                {
                    Vector2f vertex = frame.ToPlaneUV((Vector3f)v, 3);
                    if (i != 0 && !poly.Outer.Contains(vertex)) break;
                    vertices2d.Add(vertex);
                }
                Polygon2d p2d = new Polygon2d(vertices2d);
                if (i == 0)
                {
                    p2d = new Polygon2d(vertices2d);
                    p2d.Reverse();
                    poly.Outer = p2d;
                }
                else
                {
                    try
                    {
                        try
                        {
                            poly.AddHole(p2d, true, true);
                        }
                        catch (Exception e)
                        {
                            p2d.Reverse();
                            poly.AddHole(p2d, true, true);
                        }
                    }
                    catch (Exception e)
                    {
                        // skip this hole
                    }
                }
            }
            return poly;
        }

        public static bool IsOutside(this GeneralPolygon2d poly, Segment2d seg)
        {
            bool isOutside = true;
            if (poly.Outer.IsMember(seg, out isOutside))
            {
                if (isOutside)
                    return true;
                else
                    return false;
            }
            foreach (Polygon2d hole in poly.Holes)
            {
                if (hole.IsMember(seg, out isOutside))
                {
                    if (isOutside)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        public static bool BiContains(this Polygon2d poly, Segment2d seg)
        {
            foreach (Segment2d thisSeg in poly.SegmentItr())
            {
                if (thisSeg.BiEquals(seg))
                    return true;
            }
            return false;
        }

        public static bool IsMember(this Polygon2d poly, Segment2d seg, out bool IsOutside)
        {
            IsOutside = true;
            if (poly.Vertices.Contains(seg.P0) && poly.Vertices.Contains(seg.P1))
            {
                if (poly.BiContains(seg))
                    IsOutside = false;
                return true;
            }
            return false;
        }
    }

    public static class DelaunatorExtensions
    {
        public static IPoint[] ToPoints(this IEnumerable<Vector2d> vertices) => vertices.Select(vertex => new Point(vertex.x, vertex.y)).OfType<IPoint>().ToArray();

        public static Vector2d[] ToVectors2d(this IEnumerable<IPoint> points) => points.Select(point => point.ToVector2d()).ToArray();

        public static Vector2d ToVector2d(this IPoint point) => new Vector2d((float)point.X, (float)point.Y);

        public static Vector2d CetIncenter(this ITriangle tri)
        {
            Vector2d A = tri.Points.ElementAt<IPoint>(0).ToVector2d();
            Vector2d B = tri.Points.ElementAt<IPoint>(1).ToVector2d();
            Vector2d C = tri.Points.ElementAt<IPoint>(2).ToVector2d();
            double a = (B - A).Length;
            double b = (C - B).Length;
            double c = (A - C).Length;
            double x = (a * A.x + b * B.x + c * C.x) / (a + b + c);
            double y = (a * A.y + b * B.y + c * C.y) / (a + b + c);
            return new Vector2d(x, y);
        }

    }

    public static class Segment2dExtensions
    {
        public static bool BiEquals(this Segment2d self, Segment2d seg)
        {
            return seg.Center == self.Center && seg.Extent == self.Extent;
        }
    }

    public static class MeshExtensions
    {
        /// <summary>
        /// Craate a new compact DMesh3 with all of the ViRGiS metadata copied across
        /// </summary>
        /// <param name="dMesh">Source DMesh3</param>
        /// <returns>DMesh3</returns>
        public static DMesh3 Compactify(this DMesh3 dMesh)
        {
            DMesh3 mesh = new DMesh3();
            mesh.CompactCopy(dMesh, true, true, true);

            if (dMesh.HasMetadata)
            {
                string crs = dMesh.FindMetadata("CRS") as string;
                if (crs != null)
                    mesh.AttachMetadata("CRS", crs);
            }
            return mesh;
        }

        /// <summary>
        /// Converts g3.DMesh3 to UnityEngine.Mesh. 
        /// The Dmesh3 must be in Map or Local space coordinates
        /// The DMesh3 must be compact. If neccesary - run Compactify first.
        /// </summary>
        /// <param name="mesh">Dmesh3</param>
        /// <returns>UnityEngine.Mesh</returns>
        public static Mesh ToMesh(this DMesh3 mesh)
        {
            Mesh unityMesh = new Mesh();
            unityMesh.MarkDynamic();
            unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            Vector3[] vertices = new Vector3[mesh.VertexCount];
            Color[] colors = new Color[mesh.VertexCount];
            Vector2[] uvs = new Vector2[mesh.VertexCount];
            Vector3[] normals = new Vector3[mesh.VertexCount];
            NewVertexInfo data;
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                if (mesh.IsVertex(i))
                {
                    data = mesh.GetVertexAll(i);
                    vertices[i] = (Vector3)data.v;
                    if (data.bHaveC)
                        colors[i] = (Color)data.c;
                    if (data.bHaveUV)
                        uvs[i] = (Vector2)data.uv;
                    if (data.bHaveN)
                        normals[i] = (Vector3)data.n;
                }
            }
            unityMesh.vertices = vertices;
            if (mesh.HasVertexColors) unityMesh.SetColors(colors);
            if (mesh.HasVertexUVs) unityMesh.SetUVs(0, uvs);
            if (mesh.HasVertexNormals) unityMesh.SetNormals(normals);
            int[] triangles = new int[mesh.TriangleCount * 3];
            int j = 0;
            foreach (Index3i tri in mesh.Triangles())
            {
                triangles[j * 3] = tri.a;
                triangles[j * 3 + 1] = tri.b;
                triangles[j * 3 + 2] = tri.c;
                j++;
            }
            unityMesh.triangles = triangles;
            return unityMesh;
        }

        /// <summary>
        /// Convert a Unity Mesh to DMesh3 in Local Coordinates taking into account mapscale zoom etc
        /// </summary>
        /// <param name="mesh"> Unity Mesh</param>
        /// <param name="tform"> Transform of the Gameobject the Mesh is attached to </param>
        /// <param name="to">Optional CRS to use for the output DMesh3</param>
        /// <returns>DMesh3</returns>
        public static DMesh3 ToDmesh(this Mesh mesh, Transform tform)
        {
            DMesh3 dmesh = new DMesh3();
            foreach (Vector3 vertex in mesh.vertices)
            {
                dmesh.AppendVertex(tform.TransformPoint(vertex));
            }
            int[] tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3)
            {
                dmesh.AppendTriangle(tris[i], tris[i + 1], tris[i + 2]);
            }
            return dmesh;
        }

        public static void CalculateUVs(this DMesh3 dMesh)
        {
            dMesh.EnableVertexUVs(Vector2f.Zero);
            OrthogonalPlaneFit3 orth = new OrthogonalPlaneFit3(dMesh.Vertices());
            Frame3f frame = new Frame3f(orth.Origin, orth.Normal);
            AxisAlignedBox3d bounds = dMesh.CachedBounds;
            AxisAlignedBox2d boundsInFrame = new AxisAlignedBox2d();
            for (int i = 0; i < 8; i++)
            {
                boundsInFrame.Contain(frame.ToPlaneUV((Vector3f)bounds.Corner(i), 3));
            }
            Vector2f min = (Vector2f)boundsInFrame.Min;
            float width = (float)boundsInFrame.Width;
            float height = (float)boundsInFrame.Height;

            for (int i = 0; i < dMesh.VertexCount; i++)
            {
                Vector2f UV = frame.ToPlaneUV((Vector3f)dMesh.GetVertex(i), 3);
                UV.x = (UV.x - min.x) / width;
                UV.y = (UV.y - min.y) / height;
                dMesh.SetVertexUV(i, UV);
            }
        }

        public static Task<int> CalculateUVsAsync(this DMesh3 dMesh)
        {

            TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
            Task<int> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {
                dMesh.CalculateUVs();
                tcs1.SetResult(1);
            });
            return t1;
        }
    }

    public static class VirgisVectorExtensions
    {
        /// <summary>
        /// Rounds a Vector3 in 3d to the nearest value divisible by roundTo
        /// </summary>
        /// <param name="vector3">Vector 3 value</param>
        /// <param name="roundTo"> rounding size</param>
        /// <returns>Vector3 rounded value</returns>
        public static Vector3 Round(this Vector3 vector3, float roundTo = 0.1f)
        {
            return new Vector3(
                Mathf.Round(vector3.x / roundTo) * roundTo,
                Mathf.Round(vector3.y / roundTo) * roundTo,
                Mathf.Round(vector3.z / roundTo) * roundTo
                );
        }
    }

    /// <summary>
    /// from http://www.stevevermeulen.com/index.php/2017/09/using-async-await-in-unity3d-2017/
    /// </summary>
    public static class TaskExtensions
    {
        public static IEnumerator AsIEnumerator(this Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    }

    public static class JobExtensions
    {
        public static IEnumerator WaitFor(this JobHandle job)
        {
            yield return new WaitUntil(() => job.IsCompleted);
        }
    }
}
