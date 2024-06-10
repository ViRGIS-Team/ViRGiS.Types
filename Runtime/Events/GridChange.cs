/* MIT License

Copyright (c) 2020 - 24 Runette Software

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

namespace Virgis
{

    /// <summary>
    /// Event type for Grid Change Events
    /// </summary>
    public class GridEvent
    {

        private readonly BehaviorSubject<float> _gridEvent = new BehaviorSubject<float>(0);

        public GridEvent()
        {
            OnNext(1);
        }

        public IObservable<float> Event
        {
            get
            {
                return _gridEvent.AsObservable();
            }
        }

        public void OnNext(float scale)
        {
            _gridEvent.OnNext(scale);
        }

        public float Get()
        {
            return _gridEvent.Value;
        }
    }
}
