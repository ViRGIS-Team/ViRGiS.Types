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

using UnityEngine;
using System.Threading.Tasks;

namespace Virgis
{

    public class MapInitialize : MapInitializePrototype
    {

        protected void Start()
        {
            base.Start();
            State.instance.Map = gameObject;
        }

        public override void Add(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        public override VirgisFeature AddFeature<T>(T geometry)
        {
            throw new System.NotImplementedException();
        }


        public override void OnLoad()
        {
            throw new System.NotImplementedException();
        }

        public override void Loaded(VirgisLayer layer)
        {
            if (layer._layer.Value != null) Debug.Log($"Loaded Layer : {layer._layer.Value.DisplayName}");
            State.instance.AddLayer(layer);
        }

        public override Task SubInit(RecordSetPrototype layer)
        {
            throw new System.NotImplementedException();
        }

        protected override bool _load(string file)
        {
            throw new System.NotImplementedException();
        }

        public override VirgisLayer CreateLayer(RecordSetPrototype thisLayer)
        {
            throw new System.NotImplementedException();
        }
    }
}

