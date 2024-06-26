﻿/* MIT License

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

using g3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class VirgisLayer : NetworkBehaviour, IVirgisLayer {

        public NetworkVariable<RecordSetPrototype> _layer;

        public NetworkVariable<bool> m_Editable;


        public NetworkVariable<int> m_SubLayersCount;

        public FeatureType featureType { get; protected set; }

        public string sourceName { get; set; }

        public List<IVirgisLayer> subLayers
        { get; } = new List<IVirgisLayer>();


        public void AddSubLayer(IVirgisLayer layer)
        {
            subLayers.Add(layer);
            m_SubLayersCount.Value++;
        }

        private IVirgisLayer m_Parent;

        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed {
            get {
                return m_changed;
            }
            set {
                m_changed = value;
                if (m_Parent != null) m_Parent.changed = value;
            }
        }
        public bool isContainer { get; protected set; }  // if this is a container layer - do not Draw
        public bool isWriteable { // only allow edit and save for layers that can be written
            get;
            set;
        }

        protected int m_SubLayersLoaded;

        protected Guid m_id;
        protected IVirgisLoader m_loader;

        protected Task m_loaderTask;
        protected IEnumerator m_loaderItr;
        private bool m_changed;

        private readonly List<IDisposable> m_subs = new();

        protected void Awake() {
            m_id = Guid.NewGuid();
            changed = true;
            isContainer = false;
            isWriteable = false;
        }

        public void Start() {
            State appState = State.instance;
            m_subs.Add(appState.EditSession.StartEvent.Subscribe(_onEditStart));
            m_subs.Add(appState.EditSession.EndEvent.Subscribe(_onEditStop));
            if (! IsServer)
            {
                m_Parent = transform.parent?.GetComponent<IVirgisLayer>();
                if (! isContainer) Loaded(this);
                else if (m_SubLayersLoaded >= m_SubLayersCount.Value)
                {
                    if (m_Parent != null) m_Parent.Loaded(this);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_Editable.Value = false;
            } 
        }

        protected new void OnDestroy() {
            State.instance.DelLayer(this);
            // kill any active loader process
            if (m_loaderTask != null) {
                StopCoroutine(m_loaderItr);
                if(m_loaderTask.IsCompleted){
                    m_loaderTask.Dispose();
                } else {
                    Debug.Log("loader not finished");
                }
            }
            m_subs.ForEach(item => item.Dispose());
            base.OnDestroy();
        }

        /// <summary>
        /// Only Run this on a Server
        /// Burns the entire object tree of which this is the trunk
        /// starting from the leaves first. Only safe way to destroy 
        /// the ViRGiS tree on a networked version
        /// </summary>
        public void Destroy()
        {
            for (int i = transform.childCount -1; i>=0;  i--)
            {
                if ( transform.GetChild(i).TryGetComponent(out VirgisFeature com)) {
                    com.Destroy();
                    DeSpawn(com.transform);
                } else if ( transform.GetChild(i).TryGetComponent(out VirgisLayer sublayer)) {
                    sublayer.Destroy();
                    DeSpawn(sublayer.transform);
                }
            }
        }

        public bool Spawn(Transform parent){
            NetworkObject no = gameObject.GetComponent<NetworkObject>();
            try
            {
                no.Spawn();
            } catch (Exception e){
                _ = e;
                return false;
            }
            return no.TrySetParent(parent);
        }

        public void DeSpawn(Transform t = null) {
            if (t==null) t = transform;
            NetworkObject no = t.GetComponent<NetworkObject>();
            try
            {
                no.Despawn();
            }
            catch (Exception e)
            {
                _ = e;
            }
        }

        public virtual bool Load(string file) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called to initialise this layer
        /// 
        /// If the data cannot be read, fails quitely and creates an empty layer
        /// </summary>
        /// <param name="layer"> The RecordSet object that defines this layer</param>
        /// 
        public virtual IEnumerator Init(RecordSetPrototype layer) {
            m_loaderTask = AsyncInit(layer);
            m_loaderItr = m_loaderTask.AsIEnumerator();
            return m_loaderItr;
        }

        public virtual void Loaded(VirgisLayer layer) {
            if (! IsServer) subLayers.Add(layer);
            if (isContainer) {
                m_SubLayersLoaded++;
                if (m_SubLayersLoaded >= m_SubLayersCount.Value)
                {
                    if (m_Parent != null) m_Parent.Loaded(this);
                }
            } else {
                m_Parent.Loaded(this);
            }
        }

        public async virtual Task AsyncInit(RecordSetPrototype layer) {
            await SubInit(layer);
            await Draw();
        }

        public Task Awaiter(){
            return m_loaderTask;
        }

        /// <summary>
        /// Called to Initialise a sublayer
        /// </summary>
        /// <param name="layer"> The RecordSet object that defines this layer</param>
        /// 
        public async virtual Task SubInit(RecordSetPrototype layer) {
            try {
                m_Parent = transform.parent?.GetComponent<IVirgisLayer>();
                m_SubLayersLoaded = 0;
                m_loader = GetComponent<IVirgisLoader>();
                SetMetadata(layer);
                if (m_loader != null)
                    await m_loader._init();
                gameObject.SetActive(layer.Visible);
            } catch (Exception e) {
                Debug.LogError($"Layer : { layer.DisplayName} :  {e}");
            }
        }

        /// <summary>
        /// Call this to create a new feature
        /// </summary>
        /// <param name="position">Vector3 or DMesh3</param>
        public IVirgisFeature AddFeature<T>(T geometry) {
            if (State.instance.InEditSession() && IsEditable()) {
                if (m_loader != null)
                {
                    changed = true;
                    switch (geometry)
                    {
                        case Vector3[] v:
                            return m_loader._addFeature(v);
                        case DMesh3 d:
                            return m_loader._addFeature(d);
                        default: return null;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Draw the layer based upon the features in the features RecordSet
        /// </summary>
        public virtual async Task Draw() {
            // if not a server - do ntohing
            if(!IsServer) return;
            
            //change nothing if there are no changes
            if (changed) {
                if (!isContainer) {
                    //make sure the layer is empty
                    for (int i = transform.childCount - 1; i >= 0; i--) {
                        Transform child = transform.GetChild(i);
                        Destroy(child.gameObject);
                    }

                    transform.rotation = Quaternion.identity;
                    transform.localPosition = Vector3.zero;
                    transform.localScale = Vector3.one;
                }
                if (m_loader != null)
                    await m_loader._draw();
                changed = false;
            }
            if (! isContainer) Loaded(this);
            return;
        }

        /// <summary>
        /// Call this to tell the layers to create a checkpoint. 
        /// 
        /// Only valid outside of an Edit Session. Inside an Edit Session use Save() as CheckPoint() will do nothing
        /// </summary>
        public virtual void CheckPoint() {
            if (!State.instance.InEditSession()) {
                m_loader?._checkpoint();
            }

        }

        /// <summary>
        /// Called to save the current layer data to source
        /// </summary>
        /// <returns>A copy of the data save dot the source</returns>
        public virtual async Task<RecordSetPrototype> Save(bool flag = false) {
            if (!IsServer) return GetMetadata();
            if (changed) {
                if (m_loader != null)
                    await m_loader._save();
            }
            return GetMetadata();
        }

        /// <summary>
        /// Called Whenever a member entity is asked to Translate
        /// </summary>
        /// <param name="args">MoveArge Object</param>
        public virtual void Translate(MoveArgs args) {
            //do nothing
        }

        /// <summary>
        /// Called whenever a member entity is asked to Change Axis
        /// </summary>
        /// <param name="args">MoveArgs Object</param>
        public virtual void MoveAxis(MoveArgs args) {
            // do nothing 
        }

        public virtual void MoveTo(MoveArgs args) {
            //do nothing
        }

        public virtual void VertexMove(MoveArgs args) {
            //do nothing 
        }

        /// <summary>
        /// called when a daughter IVirgisEntity is selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void Selected(SelectionType button) {
            changed = true;
        }

        /// <summary>
        /// Called when a daughter IVirgisEntity is UnSelected
        /// </summary>
        /// <param name="button">SelectionType</param>
        public virtual void UnSelected(SelectionType button) {
            // do nothing
        }

        /// <summary>
        ///  Get the Closest Feature to the coordinates. Exclude any Component Ids in the Exclude Array. The exclude lis  is primarily used to avoid a GetClosest to a Faeture picking up the feature itself
        /// </summary>
        /// <param name="coords"> coordinates </param>
        /// <returns>returns the featue contained in an enitity of type S</returns>
        public IVirgisFeature GetClosest(Vector3 coords, Guid[] exclude) {
            List<VirgisFeature> list = transform.GetComponentsInChildren<VirgisFeature>().ToList();
            list = list.FindAll(item => !exclude.Contains(item.GetId()));
            KdTree<VirgisFeature> tree = new();
            tree.AddAll(list);
            return tree.FindClosest(transform.position);
        }

        /// <summary>
        /// Get the feature that matches the ID provided 
        /// </summary>
        /// <param name="id"> ID</param>
        /// <returns>returns the featue contained in an enitity of type S</returns>
        public IVirgisFeature GetFeature(Guid id) {
            return GetComponents<VirgisFeature>().ToList().Find(item => item.GetId() == id);
        }

        /// <summary>
        /// Fecth the layer GUID
        /// </summary>
        /// <returns>GUID</returns>
        public Guid GetId() {
            if (m_id == Guid.Empty)
                m_id = Guid.NewGuid();
            return m_id;
        }

        /// <summary>
        /// Get the metadata for this Layer
        /// </summary>
        /// <returns></returns>
        public RecordSetPrototype GetMetadata() {
            return _layer.Value;
        }

        /// <summary>
        /// Sets the layer Metadata
        /// </summary>
        /// <param name="layer">Data tyoe that inherits form RecordSet</param>
        public void SetMetadata(RecordSetPrototype layer) {
            _layer.Value = layer;
        }

        /// <summary>
        /// Fetches the feature shape to be used to create new features
        /// </summary>
        /// <returns></returns>
        public virtual GameObject GetFeatureShape()
        {
            if (m_loader == null)
                return default;
            return m_loader.GetFeatureShape();
        }

        /// <summary>
        /// Change the layer visibility
        /// </summary>
        /// <param name="visible"></param>
        public virtual void SetVisible(bool visible) {
            if (GetMetadata().Visible != visible) {
                _layer.Value.Visible = visible;
                gameObject.SetActive(visible);
                _set_visible();
            }
        }

        public virtual void _set_visible() {
        }

        /// <summary>
        /// Test if this layer is currently visible
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsVisible() {
            return GetMetadata().Visible;
        }

        /// <summary>
        /// Sets a marker that this particular layer is being edited.
        /// </summary>
        /// 
        /// There can be only one layer being edited during an edit session on the client
        /// 
        /// <param name="inSession"></param> true to indicate that this layer is in edit session,
        /// or false if otherwise.
        [Rpc(SendTo.Server)]
        public void SetEditableRpc(bool inSession) {
            if (isWriteable) {
                m_Editable.Value = inSession;
                _set_editable();
            }
        }

        protected virtual void _set_editable() {
        }

        /// <summary>
        /// Test to see if this layer is currently being edited
        /// </summary>
        /// <returns>Boolean</returns>
        
        public bool IsEditable() {
            return m_Editable.Value;
        }

        protected virtual void _onEditStart(bool test) {
            if (IsEditable()) {
                VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                foreach (VirgisFeature com in coms) {
                    com.OnEdit(true);
                }
            }
        }

        protected virtual void _onEditStop(bool test) {
            if (IsEditable()) {
                VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                foreach (VirgisFeature com in coms) {
                    com.OnEdit(false);
                }
            }
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            VirgisLayer com = obj as VirgisLayer;
            if (com == null)
                return false;
            else
                return Equals(com);
        }

        public override int GetHashCode() {
            return m_id.GetHashCode();
        }
        public bool Equals(VirgisLayer other) {
            if (other == null)
                return false;
            return m_id.Equals(other.GetId());
        }

        public IVirgisLayer GetLayer() {
            return this;
        }

        public void OnEdit(bool inSession) {
            // do nothing
        }

        public virtual Dictionary<string, object> GetInfo(VirgisFeature feat) {
            return default;
        }

        public void MessageUpwards(string method, object args) {
            transform.SendMessageUpwards(method, args, SendMessageOptions.DontRequireReceiver);
        }

        VirgisFeature IVirgisLayer.AddFeature<T>(T geometry) {
            throw new NotImplementedException();
        }

        VirgisFeature IVirgisLayer.GetFeature(Guid id) {
            throw new NotImplementedException();
        }

        VirgisFeature IVirgisEntity.GetClosest(Vector3 coords, Guid[] exclude) {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetInfo() {
            throw new NotImplementedException();
        }

        public void SetInfo(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }

        
    }
}