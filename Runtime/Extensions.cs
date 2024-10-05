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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Unity.Netcode;
using VirgisGeometry;
using Unity.Mathematics;

namespace Virgis
{

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

    public static class VirgisSerializationExtensions
    {
        public static void WriteValueSafe(this FastBufferWriter writer, in double3 v)
        {
            writer.WriteValueSafe(v.x);
            writer.WriteValueSafe(v.y);
            writer.WriteValueSafe(v.z);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out double3 v)
        {
            reader.ReadValueSafe(out v.x);
            reader.ReadValueSafe(out v.y);
            reader.ReadValueSafe(out v.z);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in double2 v)
        {
            writer.WriteValueSafe(v.x);
            writer.WriteValueSafe(v.y);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out double2 v)
        {
            reader.ReadValueSafe(out v.x);
            reader.ReadValueSafe(out v.y);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in int3 i)
        {
            writer.WriteValueSafe(i.x);
            writer.WriteValueSafe(i.y);
            writer.WriteValueSafe(i.z);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out int3 i)
        {
            reader.ReadValueSafe(out i.x);
            reader.ReadValueSafe(out i.y);
            reader.ReadValueSafe(out i.z);
        }


        public static void WriteValueSafe(this FastBufferWriter writer, in double3[] varray)
        {
            writer.WriteValueSafe(varray.Length);
            foreach (double3 v in varray)
            {
                writer.WriteValueSafe(v);
            }
        }

        public static void ReadValueSafe(this FastBufferReader reader, out double3[] varray)
        {
            reader.ReadValueSafe(out int arrayLength);
            varray = new double3[arrayLength];
            double3 value;
            for (int i = 0; i < arrayLength; i++)
            {
                reader.ReadValueSafe(out value);
                varray[i] = (value);
            }
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in double2[] varray)
        {
            writer.WriteValueSafe(varray.Length);
            foreach (double2 v in varray)
            {
                writer.WriteValueSafe(v);
            }
        }

        public static void ReadValueSafe(this FastBufferReader reader, out double2[] varray)
        {
            reader.ReadValueSafe(out int arrayLength);
            varray = new double2[arrayLength];
            double2 value;
            for (int i = 0; i < arrayLength; i++)
            {
                reader.ReadValueSafe(out value);
                varray[i] = (value);
            }
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in int3[] iarray)
        {
            writer.WriteValueSafe(iarray.Length);
            foreach (int3 i in iarray)
            {
                writer.WriteValueSafe(i);
            }
        }

        public static void ReadValueSafe(this FastBufferReader reader, out int3[] iarray)
        {
            reader.ReadValueSafe(out int arrayLength);
            iarray = new int3[arrayLength];
            int3 value;
            for (int i = 0; i < arrayLength; i++)
            {
                reader.ReadValueSafe(out value);
                iarray[i] = (value);
            }
        }
    }
}
