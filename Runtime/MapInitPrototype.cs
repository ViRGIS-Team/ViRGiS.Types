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
using R3;
using System.Collections;
using UnityEngine.Serialization;

namespace Virgis {


    /// <summary>
    /// This script initialises the project and loads the Project and Layer data.
    /// 
    /// It is run once at Startup
    /// </summary>
    public abstract class MapInitializePrototype : MonoBehaviour, IVirgisLayer
    {

        public GameObject appState;

        [FormerlySerializedAs("LoadOnStartup")] public string loadOnStartup;

        public FeatureType FeatureType => throw new NotImplementedException();

        public string SourceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<IVirgisLayer> SubLayers => throw new NotImplementedException();

        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed
        {
            get => _bChanged;
            protected set
            {
                _bChanged = value;
                IVirgisLayer parent = transform.parent?.GetComponent<IVirgisLayer>();
                if (value && parent != null) parent.Changed();
            }
        }
        public bool IsContainer { get; protected set; }  // if this is a container layer - do not Draw
        public bool IsEditable => false;
        public bool IsCheckedOut => false;
        public bool IsWriteable { get => false; set { } }

        protected Guid MId;
        private bool _bChanged;
        private readonly List<IDisposable> _mSubs = new List<IDisposable>();

        protected void Start()
        {
            _mSubs.Add(State.Instance.EditSession.StartEvent.Subscribe(OnEditStart));
            _mSubs.Add(State.Instance.EditSession.EndEvent.Subscribe(OnEditStop));
            _mSubs.Add(State.Instance.Client.Event.Subscribe(OnClientConnected));
        }

        protected void OnDestroy()
        {
            _mSubs.ForEach(x => x.Dispose());
        }

        /// <summary>
        /// This is the initialisation script.
        /// 
        /// It loads the Project file, reads it for the layers and calls Draw to render each layer
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool Load(string file)
        {
            return _load(file);
        }

        public abstract bool Load();

        protected abstract bool _load(string file);
        
        


        /// <summary>
        /// override this call in the consuming project to process the individual layers.
        /// This allows the consuming project to define the layer types
        /// </summary>
        /// <param name="thisLayer"> the layer that ws pulled from the project file</param>
        /// <returns></returns>
        public abstract VirgisLayer CreateLayer(RecordSetPrototype thisLayer);

        protected async Task InitLayers(List<RecordSetPrototype> layers, Action callback)
        {
            try
            {
                List<Task> tasks = new();
                foreach (RecordSetPrototype thisLayer in layers)
                {
                    Debug.Log("Loading Layer : " + thisLayer.DisplayName);
                    VirgisLayer temp = CreateLayer(thisLayer);
                    if (temp == null) continue;
                    if (!temp.Spawn(State.Instance.Map.transform)) Debug.Log("reparent failed");
                    tasks.Add(temp.AsyncInit(thisLayer));
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Debug.LogError($"Project load failed :" + e);
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
            foreach (IVirgisLayer layer in State.Instance.Layers)
            {
                try
                {
                    layer.Draw();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Project Layer {layer.SourceName} has failed to draw :" + e);
                }
            }
        }

        /// <summary>
        /// Override this call to add functionality after the Project has loaded
        /// </summary>
        protected abstract void OnLoad();
        
        /// <summary>
        /// Override this to add functionality after the Client has loaded
        /// </summary>
        protected abstract void OnClientConnected(ClientEventType thisEvent);


        public abstract void Add(MoveArgs args);

        protected Task _draw()
        {
            throw new NotImplementedException();
        }

        protected void _checkpoint()
        {
        }

        /// <summary>
        /// this call initiates the saving of the whole project and calls `Save` on each layer in turn
        /// </summary>
        /// <returns>RecordSetPrototype</returns>
        public abstract RecordSetPrototype Save();

        /// <summary>
        /// Overload this to actually save the project file
        /// </summary>
        /// <param name="clientId"></param>
        public abstract Task SaveProjectAsync(ulong clientId);


        protected Task _save()
        {
            throw new NotImplementedException();
        }


        protected void OnEditStart(bool ignore)
        {

        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        protected void OnEditStop(bool saved)
        {
            if (!saved)
            {
                foreach (VirgisLayer layer in State.Instance.Layers)
                {
                    layer?.SetEditable(false);
                }
            } else
            {
                Save();
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

        public void CheckPoint()
        {
            throw new NotImplementedException();
        }

        public void UnCheckPoint()
        {
            throw new NotImplementedException();
        }

        public ulong GetId()
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

        public Dictionary<string, string> GetInfo()
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

        public SerializableMaterialHash GetFeatureDefaultColor()
        {
            throw new NotImplementedException();
        }

        public void AddFeatureRpc(Vector3[] verteces)
        {
            throw new NotImplementedException();
        }

        public bool GetParent(out IVirgisEntity parent)
        {
            throw new NotImplementedException();
        }

        public void RemoveVertex(Transform vertex)
        {
            throw new NotImplementedException();
        }

        public void AddVertex(Vector3 pos)
        {
            throw new NotImplementedException();
        }

        public void Changed()
        {
            throw new NotImplementedException();
        }

        public virtual void New()
        {
            throw new NotImplementedException();
        }
    }
}
