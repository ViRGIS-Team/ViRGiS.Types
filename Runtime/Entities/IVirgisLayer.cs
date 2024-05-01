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
using UnityEngine;

namespace Virgis
{

    public interface IVirgisLayer : IVirgisEntity
    {

        FeatureType featureType
        {
            get;
        }

        string sourceName
        {
            get; set;
        }

        public bool isContainer
        {
            get;
        }

        List<IVirgisLayer> subLayers
        {
            get;
        }

        public bool isWriteable
        {
            get;
            set;
        }

        public bool changed
        {
            get;
            set;
        }

        VirgisFeature AddFeature<T>(T geometry);
        bool Load(string file);
        IEnumerator Init(RecordSetPrototype layer);
        Task AsyncInit(RecordSetPrototype layer);
        Task SubInit(RecordSetPrototype layer);
        Task Draw();
        void Loaded(VirgisLayer layer);
        void CheckPoint();
        Task<RecordSetPrototype> Save(bool flag);
        VirgisFeature GetFeature(Guid id);
        GameObject GetFeatureShape();
        RecordSetPrototype GetMetadata();
        void SetMetadata(RecordSetPrototype meta);
        void SetVisible(bool visible);
        bool IsVisible();
        bool IsEditable();
        void SetEditableRpc(bool inSession);
        void MessageUpwards(string method, object args);
    }
}