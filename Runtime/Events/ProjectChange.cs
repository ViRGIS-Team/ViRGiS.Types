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

using R3;
using System;

namespace Virgis {

    public enum ProjectEventType
    {
        Started,
        Complete
    }

    public class ProjectChange {

        

        private GisProjectPrototype _project;

        private readonly Subject<ProjectEventType> _projectEvent = new Subject<ProjectEventType>();

        public void Set(GisProjectPrototype project) {
            _project = project;
            _projectEvent.OnNext(ProjectEventType.Started);
        }

        public void Complete() {
            _projectEvent.OnNext(ProjectEventType.Complete);
        }

        public GisProjectPrototype Get() {
            return _project;
        }

        public Observable<ProjectEventType> Event {
            get {
                return _projectEvent.AsObservable();
            }
        }

    }

}
