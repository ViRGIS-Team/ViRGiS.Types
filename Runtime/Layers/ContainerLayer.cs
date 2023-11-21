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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Virgis {

    public class ContainerLayer : VirgisLayer  {

        protected new void Awake() {
            base.Awake();
            isContainer = true;
        }

        public async override Task SubInit(RecordSetPrototype layerData)
        {
            foreach (VirgisLayer layer in subLayers.Cast<VirgisLayer>())
            {
                layer.Init(layerData);
                await layer.Awaiter();
            }
            await base.SubInit(layerData);
            return;
        }

        public override void CheckPoint() {
            foreach (VirgisLayer layer in subLayers.Cast<VirgisLayer>()) {
                layer.CheckPoint();
            }
            base.CheckPoint();
        }

        public override async Task Draw() {
            foreach (VirgisLayer layer in subLayers.Cast<VirgisLayer>()) {
                await layer.Draw();
            }
            await base.Draw();
            return;
        }

        public override async Task<RecordSetPrototype> Save(bool flag = false) {
            foreach (VirgisLayer layer in subLayers.Cast<VirgisLayer>()) {
                await layer.Save();
            }
            await base.Save();
            return GetMetadata();
        }

        public new void OnDestroy()
        {
            foreach (VirgisLayer layer in subLayers.Cast<VirgisLayer>()) {
                Destroy(layer);
            }
            base.OnDestroy();
        }
    }
}

