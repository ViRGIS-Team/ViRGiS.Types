/* MIT License

Copyright (c) 2020 - 23 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using VirgisGeometry;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;

namespace Virgis
{
    public static class DCurveExtensions
    {

        /// <summary>
        /// Converts a DCurve3 in map space coordinates to a List of Vector3 to world space
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static List<Vector3> ToVector3(this DCurve3 curve)
        {
            List<Vector3d> verteces = curve.Vertices.ToList();
            List<Vector3> result = new();
            verteces.ForEach(v =>
            {
                result.Add(
                    State.instance.Map.transform.TransformPoint((Vector3)v)
                    ) ;
            });
            return result;
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
