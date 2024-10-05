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
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;
using System.Collections;
using Unity.Netcode;
using Unity.Mathematics;

namespace Virgis {


    /// <summary>
    /// This script initialises the project and loads the Project and Layer data.
    /// 
    /// It is run once at Startup
    /// </summary>
    public abstract class MapInitializePrototype : MonoBehaviour, IVirgisLayer
    {

        public GameObject appState;

        public string LoadOnStartup;

        public FeatureType featureType => throw new NotImplementedException();

        public string sourceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<IVirgisLayer> subLayers => throw new NotImplementedException();

        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed
        {
            get
            {
                return m_changed;
            }
            set
            {
                m_changed = value;
                IVirgisLayer parent = transform.parent?.GetComponent<IVirgisLayer>();
                if (parent != null) parent.changed = value;
            }
        }
        public bool isContainer { get; protected set; }  // if this is a container layer - do not Draw
        public bool IsEditable { get => false; }
        public bool IsWriteable { get => false; set { } }

        protected Guid m_id;
        private bool m_changed;
        private readonly List<IDisposable> m_subs = new List<IDisposable>();

        protected void Start()
        {
            UserNetworkVariableSerialization<double3[]>.WriteValue = VirgisSerializationExtensions.WriteValueSafe;
            UserNetworkVariableSerialization<double3[]>.ReadValue = VirgisSerializationExtensions.ReadValueSafe;
            UserNetworkVariableSerialization<double2[]>.WriteValue = VirgisSerializationExtensions.WriteValueSafe;
            UserNetworkVariableSerialization<double2[]>.ReadValue = VirgisSerializationExtensions.ReadValueSafe;
            UserNetworkVariableSerialization<int3[]>.WriteValue = VirgisSerializationExtensions.WriteValueSafe;
            UserNetworkVariableSerialization<int3[]>.ReadValue = VirgisSerializationExtensions.ReadValueSafe;

            m_subs.Add(State.instance.EditSession.StartEvent.Subscribe(_onEditStart));
            m_subs.Add(State.instance.EditSession.EndEvent.Subscribe(_onEditStop));
        }

        /// 
        /// This is the initialisation script.
        /// 
        /// It loads the Project file, reads it for the layers and calls Draw to render each layer
        /// </summary>
        public bool Load(string file)
        {
            return _load(file);
        }

        protected abstract bool _load(string file);


        /// <summary>
        /// override this call in the consuming project to process the individual layers.
        /// This allows the consuming project to define the layer types
        /// </summary>
        /// <param name="thisLayer"> the layer that ws pulled from the project file</param>
        /// <returns></returns>
        public abstract VirgisLayer CreateLayer(RecordSetPrototype thisLayer);

        protected async Task initLayers(List<RecordSetPrototype> layers, Action callback)
        {
            try
            {
                List<Task> tasks = new();
                foreach (RecordSetPrototype thisLayer in layers)
                {
                    VirgisLayer temp = null;
                    Debug.Log("Loading Layer : " + thisLayer.DisplayName);
                    temp = CreateLayer(thisLayer);
                    if (temp == null) continue;
                    if (!temp.Spawn(State.instance.Map.transform)) Debug.Log("reparent failed");
                    tasks.Add(temp.AsyncInit(thisLayer));
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Debug.LogError($"Project load failed :" + e.ToString());
                callback();
            }
            OnLoad();
            Debug.Log("Completed load Project File");
            callback();
        }

        /// <summary>
        /// This call initiates the drawing of the virtual space and calls `Draw ` on each layer in turn.
        /// </summary>
        public void Draw()
        {
            foreach (IVirgisLayer layer in State.instance.Layers)
            {
                try
                {
                    layer.Draw();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Project Layer {layer.sourceName} has failed to draw :" + e.ToString());
                }
            }
        }

        /// <summary>
        /// Override this call to add functionality after the Project has loaded
        /// </summary>
        public abstract void OnLoad();


        public abstract void Add(MoveArgs args);

        protected Task _draw()
        {
            throw new System.NotImplementedException();
        }

        protected void _checkpoint()
        {
        }

        /// <summary>
        /// this call initiates the saving of the whole project and calls `Save` on each layer in turn
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public virtual async Task<RecordSetPrototype> Save()
        {
            try
            {
                Debug.Log("Save starts");
                if (State.instance.project != null)
                {
                    foreach (IVirgisLayer com in State.instance.Layers)
                    {
                        RecordSetPrototype alayer = await (com as VirgisLayer).Save();
                    }
                }
                Debug.Log("Save Completed");
                return default;
            }
            catch (Exception e)
            {
                Debug.Log("Save failed : " + e.ToString());
                return default;
            }
        }

        protected Task _save()
        {
            throw new System.NotImplementedException();
        }


        protected void _onEditStart(bool ignore)
        {
            CheckPoint();
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        protected async void _onEditStop(bool saved)
        {
            if (!saved)
            {
                foreach (VirgisLayer layer in State.instance.Layers)
                {
                    layer.SetEditable(false);
                }
            } else
            {
                await Save();
            }
        }

        public virtual Shapes GetFeatureShape()
        {
            return Shapes.None;
        }

        public abstract VirgisFeature AddFeature<T>(T geometry);

        public virtual IEnumerator Init(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task AsyncInit(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public abstract Task SubInit(RecordSetPrototype layer);


        public virtual void CheckPoint()
        {
            //do nothing
        }

        public VirgisFeature GetFeature(Guid id)
        {
            throw new NotImplementedException();
        }

        public RecordSetPrototype GetMetadata()
        {
            throw new NotImplementedException();
        }

        public void SetMetadata(RecordSetPrototype meta)
        {
            throw new NotImplementedException();
        }

        public void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }

        public bool IsVisible()
        {
            throw new NotImplementedException();
        }

        public void SetEditable(bool checkout)
        {
            throw new NotImplementedException();
        }

        public void MessageUpwards(string method, object args)
        {
            throw new NotImplementedException();
        }

        public void Selected(SelectionType button)
        {
            throw new NotImplementedException();
        }

        public void UnSelected(SelectionType button)
        {
            throw new NotImplementedException();
        }

        public Guid GetId()
        {
            throw new NotImplementedException();
        }

        public VirgisFeature GetClosest(Vector3 coords, Guid[] exclude)
        {
            throw new NotImplementedException();
        }

        public void MoveAxis(MoveArgs args)
        {
            // do nothing
        }

        public void Translate(MoveArgs args)
        {
            // do nothing
        }

        public void MoveTo(MoveArgs args)
        {
            // do nothing
        }

        public void VertexMove(MoveArgs args)
        {
            // do nothing
        }

        public IVirgisLayer GetLayer()
        {
            throw new NotImplementedException();
        }

        public void OnEdit(bool inSession)
        {
            throw new NotImplementedException();
        }

        public void Destroy()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetInfo()
        {
            throw new NotImplementedException();
        }

        public void SetInfo(Dictionary<string, object> meta)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetInfo(VirgisFeature feat)
        {
            throw new NotImplementedException();
        }

        public void SetMaterial(string idx, Color color, Dictionary<string, float> properties = null)
        {
            throw new NotImplementedException();
        }

        public Material GetMaterial(string idx)
        {
            throw new NotImplementedException();
        }

        Task IVirgisLayer.Draw()
        {
            throw new NotImplementedException();
        }

        public virtual void Loaded(VirgisLayer layer)
        {
            throw new NotImplementedException();
        }
    }
}
