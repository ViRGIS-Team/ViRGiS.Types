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

using UniRx;
using System;

namespace Virgis {

    public class LayerChange {

        private readonly Subject<VirgisLayer> _AddEvent = new Subject<VirgisLayer>();
        private readonly Subject<VirgisLayer> _DelEvent = new Subject<VirgisLayer>();

        public void AddLayer(VirgisLayer layer) {
            _AddEvent.OnNext(layer);
        }

        public void DelLayer(VirgisLayer layer) {
            _DelEvent.OnNext(layer);
        }

        public IObservable<VirgisLayer> AddEvents {
            get {
                return _AddEvent.AsObservable();
            }
        }

        public IObservable<VirgisLayer> DelEvents {
            get {
                return _DelEvent.AsObservable();
            }
        }

    }

}
