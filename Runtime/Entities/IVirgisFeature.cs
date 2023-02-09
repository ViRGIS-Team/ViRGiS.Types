/* MIT License

Copyright (c) 2020 - 21 Runette Software

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
using System.Collections.Generic;
using UnityEngine;

namespace Virgis {

    /// <summary>
    /// Abstract parent for all in game entities
    /// </summary>
    public interface IVirgisEntity
    {
        void Selected(SelectionType button);
        void UnSelected(SelectionType button);
        Guid GetId();
        VirgisFeature GetClosest(Vector3 coords, Guid[] exclude);
        void MoveAxis(MoveArgs args);
        void Translate(MoveArgs args);
        void MoveTo(MoveArgs args);
        void VertexMove(MoveArgs args);
        T GetLayer<T>();
        void OnEdit(bool inSession);
        void Destroy();
        Dictionary<string, object> GetInfo();
        void SetInfo(Dictionary<string, object> meta);
        Dictionary<string, object> GetInfo(VirgisFeature feat);
    }

    /// <summary>
    /// Abstract Parent for all symbology relevant in game entities
    /// </summary>
    public interface IVirgisFeature : IVirgisEntity
    {
        void SetMaterial(Material mainMat, Material selectedMat);
        //void MoveTo(Vector3 newPos);
        VirgisFeature AddVertex(Vector3 position);
        void RemoveVertex(VirgisFeature vertex);
        T GetGeometry<T>();

        void Hover(Vector3 hit);
        void UnHover();
    }
}
