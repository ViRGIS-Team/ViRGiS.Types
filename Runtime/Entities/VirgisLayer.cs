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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using R3;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Virgis
{

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class VirgisLayer : NetworkBehaviour, IVirgisLayer {

        public FeatureType featureType { get; protected set; }
        public string sourceName { get; set; }
        private bool IsListening => NetworkManager.Singleton is not null && NetworkManager.Singleton.IsListening;

        [HideInInspector]
        public List<IVirgisLayer> subLayers
        { get; } = new List<IVirgisLayer>();
        [HideInInspector]
        public NetworkVariable<Shapes> FeatureShape = new();
        [HideInInspector]
        public readonly NetworkVariable<SerializableMaterialHash> DefaultCol = new();
        public readonly SerializableSymbology MSymbology = new();

        protected readonly NetworkVariable<RecordSetPrototype> Layer = new();
        protected readonly NetworkVariable<ulong> MCheckedOut = new();
        protected readonly NetworkVariable<bool> MWriteable = new();
        protected readonly NetworkVariable<int> MSubLayersCount = new();

        public void AddSubLayer(IVirgisLayer layer)
        {
            subLayers.Add(layer);
            MSubLayersCount.Value++;
        }

        protected bool MEditing;
        private IVirgisLayer _mParent;

        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed {
            get => mChanged;
            set {
                mChanged = value;
                if (_mParent != null) _mParent.changed = value;
            }
        }
        public bool isContainer { get; protected set; }  // if this is a container layer - do not Draw


        protected int MSubLayersLoaded;

        protected IVirgisLoader MLoader;

        protected Task MLoaderTask;
        protected IEnumerator MLoaderItr;
        [FormerlySerializedAs("m_changed")] public bool mChanged;

        private readonly List<IDisposable> _mSubs = new();

        protected void Awake() {
            changed = true;
            isContainer = false;
        }

        public virtual void Start() {
            State appState = State.instance;
            _mSubs.Add(appState.EditSession.StartEvent.Subscribe(_onEditStart));
            _mSubs.Add(appState.EditSession.EndEvent.Subscribe(_onEditStop));
            _mSubs.Add(appState.EditSession.ChangeLayerEvent.Subscribe(_onEditLayerChange));
            if ( IsListening && ! IsServer)
            {
                _mParent = transform.parent?.GetComponent<IVirgisLayer>();
                if (! isContainer) Loaded(this);
                else if (MSubLayersLoaded >= MSubLayersCount.Value)
                {
                    _mParent?.Loaded(this);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (! IsListening || IsServer)
            {
                MCheckedOut.Value = 0;
            } 
        }

        protected new void OnDestroy() {
            State.instance.DelLayer(this);
            // kill any active loader process
            if (MLoaderTask != null) {
                StopCoroutine(MLoaderItr);
                if(MLoaderTask.IsCompleted){
                    MLoaderTask.Dispose();
                } else {
                    Debug.Log("loader not finished");
                }
            }
            _mSubs.ForEach(item => item.Dispose());
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
                } else if ( transform.GetChild(i).TryGetComponent(out VirgisLayer sublayer)) {
                    sublayer.Destroy();
                }
            }
            DeSpawn();
        }

        public bool Spawn(Transform parent){
            NetworkObject no = gameObject.GetComponent<NetworkObject>();
            if (IsListening)
            {
                try
                {
                    no.Spawn();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    return false;
                }

                return no.TrySetParent(parent);
            }
            else
            {
                no.transform.SetParent(transform);
                OnNetworkSpawn();
                return true;
            }
        }

        public void DeSpawn(Transform t = null) {
            if (!t) t = transform;
            NetworkObject no = t.GetComponent<NetworkObject>();
            if (IsListening)
            {

                try
                {
                    if (no.IsSpawned) no.Despawn();
                }
                catch (Exception e)
                {
                    _ = e;
                }
            }
            else
            {
                OnNetworkDespawn();
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
            MLoaderTask = AsyncInit(layer);
            MLoaderItr = MLoaderTask.AsIEnumerator();
            return MLoaderItr;
        }

        public virtual void Loaded(VirgisLayer layer) {
            if (IsListening && ! IsServer) subLayers.Add(layer);
            if (isContainer) {
                MSubLayersLoaded++;
                if (MSubLayersLoaded >= MSubLayersCount.Value)
                {
                    if (_mParent != null) _mParent.Loaded(this);
                }
            } else {
                _mParent.Loaded(this);
            }
        }

        public virtual async Task AsyncInit(RecordSetPrototype layer) {
            await SubInit(layer);
            await Draw();
        }

        public Task Awaiter(){
            return MLoaderTask;
        }

        /// <summary>
        /// Called to Initialise a sublayer
        /// </summary>
        /// <param name="layer"> The RecordSet object that defines this layer</param>
        /// 
        public virtual async Task SubInit(RecordSetPrototype layer) {
            try {
                _mParent = transform.parent?.GetComponent<IVirgisLayer>();
                MSubLayersLoaded = 0;
                MLoader = GetComponent<IVirgisLoader>();
                SetMetadata(layer);
                if (MLoader != null) {
                    await MLoader._init();
                    FeatureShape.Value = MLoader.GetFeatureShape();
                }
                else
                {
                    FeatureShape.Value = Shapes.None;
                }
                gameObject.SetActive(layer.Visible);
            } catch (Exception e) {
                Debug.LogError($"Layer : { layer.DisplayName} :  {e}");
            }
        }

        /// <summary>
        /// Draw the layer based upon the features in the features RecordSet
        /// </summary>
        public virtual async Task Draw() {
            // if not a server - do nothing
            if(IsListening && !IsServer) return;
            
            //change nothing if there are no changes
            if (changed) {
                if (!isContainer) {
                    //make sure the layer is empty
                    for (int i = transform.childCount - 1; i >= 0; i--) {
                        Transform child = transform.GetChild(i);
                        VirgisFeature com = child.GetComponent<VirgisFeature>();
                        if (com != null)
                        {
                            com.Destroy();
                            Destroy(com);
                        }

                    }

                    transform.rotation = Quaternion.identity;
                    transform.localPosition = Vector3.zero;
                    transform.localScale = Vector3.one;
                }
                if (MLoader != null)
                    await MLoader._draw();
                changed = false;
            }
            if (! isContainer) Loaded(this);
            return;
        }

        /// <summary>
        /// Called to save the current layer data to source
        /// </summary>
        /// <returns>A copy of the data save dot the source</returns>
        public virtual RecordSetPrototype Save() {
            if (changed) {
                SaveRpc(State.instance.Hash);
                changed = false;
                MEditing = false;
            }
            return GetMetadata();
        }

        [Rpc(SendTo.Server)]
        private void SaveRpc(ulong clientId)
        {
            if (MCheckedOut.Value == clientId)
            {
                State.instance.NetworkState.LogMessageRpc($"Check-in layer {GetId()} by client {clientId}");
                State.instance.NetworkState.LogMessageRpc($"Save requested on layer {GetId()} by client {clientId}");
                if (MLoader != null)
                    _ = MLoader._save();
                MCheckedOut.Value = 0;
            }
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
        public void MoveAxis(MoveArgs args)
        {
            _moveAxis(args);
        }

        protected virtual void _moveAxis(MoveArgs args)
        {
            //do nothing
        }

        public void MoveTo(MoveArgs args)
        {
            _move(args);
        }

        protected virtual void _move(MoveArgs args)
        {
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
            //do nothing
        }

        /// <summary>
        /// Called when a daughter IVirgisEntity is UnSelected
        /// </summary>
        /// <param name="button">SelectionType</param>
        public virtual void UnSelected(SelectionType button) {
            // do nothing
        }

        /// <summary>
        /// Used to signal to the hierarchy that a substantive change has been made
        /// </summary>
        public virtual void Changed()
        {
            changed = true;
        }

        /// <summary>
        ///  Get the Closest Feature to the coordinates. Exclude any Component Ids in the Exclude Array. The exclude lis  is primarily used to avoid a GetClosest to a Faeture picking up the feature itself
        /// </summary>
        /// <param name="coords"> coordinates </param>
        /// <returns>returns the featue contained in an enitity of type S</returns>
        public IVirgisFeature GetClosest(Vector3 coords, ulong[] exclude) {
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
        public IVirgisFeature GetFeature(ulong id) {
            return GetComponents<VirgisFeature>().ToList().Find(item => item.GetId() == id);
        }

        /// <summary>
        /// Fecth the layer GUID
        /// </summary>
        /// <returns>GUID</returns>
        public ulong GetId() {
            return NetworkObject.NetworkObjectId;
        }

        /// <summary>
        /// Get the metadata for this Layer
        /// </summary>
        /// <returns></returns>
        public RecordSetPrototype GetMetadata() {
            return Layer.Value;
        }

        /// <summary>
        /// Sets the layer Metadata
        /// </summary>
        /// <param name="layer">Data tyoe that inherits form RecordSet</param>
        public void SetMetadata(RecordSetPrototype layer) {
            Layer.Value = layer;
        }

        /// <summary>
        /// Fetches the feature shape to be used to create new features
        /// </summary>
        /// <returns></returns>
        public virtual Shapes GetFeatureShape()
        {
            return FeatureShape.Value;
        }

        public virtual SerializableMaterialHash GetFeatureDefaultColor()
        {
            return DefaultCol.Value;
        }

        /// <summary>
        /// Change the layer visibility
        /// </summary>
        /// <param name="visible"></param>
        public virtual void SetVisible(bool visible) {
            if (GetMetadata().Visible != visible) {
                Layer.Value.Visible = visible;
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

        public bool IsWriteable
        {
            get { return MWriteable.Value; }
            set { MWriteable.Value = value; }
        }

        public bool IsEditable
        {
            get { return MCheckedOut.Value == 0 || MCheckedOut.Value == State.instance.Hash; }
        }

        public void SetEditable(bool checkout)
        {
            if (isContainer)
            {
                foreach (VirgisLayer sublayer in subLayers)
                {
                    sublayer.SetEditable(checkout);
                }
            } else
            {
                SetEditableRpc(checkout, State.instance.Hash);
            }
        }

        [Rpc(SendTo.Server)]
        private void SetEditableRpc(bool checkout, ulong clientID) {
            if (checkout)
            {
                if (MCheckedOut.Value == 0)
                {
                    MCheckedOut.Value = clientID;
                    _set_editable();
                    MLoader.CheckpointSymbology();
                    State.instance.NetworkState.LogMessageRpc($"Check-out layer {GetId()} by client {clientID}");
                }
            }
            else
            {
                if (MCheckedOut.Value == clientID)
                {
                    MCheckedOut.Value = 0;
                    MLoader.RevertSymbology();
                    State.instance.NetworkState.LogMessageRpc($"Check-in layer {GetId()} by client {clientID}");
                    RequestRedrawRpc();
                    //_ = AsyncInit(GetMetadata());
                }
            }
        }

        protected virtual void _set_editable() {
        }

        protected virtual void _onEditStart(bool test) {
            if (IsWriteable)
            {
                VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                foreach (VirgisFeature com in coms)
                {
                    com.OnEditStart(test);
                }
            }
        }

        protected virtual void _onEditLayerChange((IVirgisLayer, IVirgisLayer) args)
        {
            if (IsWriteable)
            {
                if (args.Item2 as VirgisLayer == this )
                {
                    if (IsWriteable)
                    {
                        MEditing = true;
                        VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                        foreach (VirgisFeature com in coms)
                        {
                            com.OnEdit(true);
                        }
                    }
                } else if (MEditing)
                {
                    if (IsWriteable)
                    {
                        VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                        foreach (VirgisFeature com in coms)
                        {
                            com.OnEdit(false);
                        }
                        MEditing = false;
                    }
                }
            }
        }

        protected virtual void _onEditStop(bool save) {
            MEditing = false;
            Draw();
            if (IsWriteable)
            {
                VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                foreach (VirgisFeature com in coms)
                {
                    com.OnEditEnd(save);
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
            return (int)GetId();
        }
        public bool Equals(VirgisLayer other) {
            if (other == null)
                return false;
            return GetId().Equals(other.GetId());
        }

        public IVirgisLayer GetLayer() {
            return this;
        }

        public bool GetParent(out IVirgisEntity parent)
        {
            Transform temp = transform.parent;
            if (temp != null)
            {
                parent = transform.parent.GetComponent<IVirgisEntity>();
                return true;
            }
            parent = default;
            return false;
        }

        public IVirgisLoader GetLoader()
        {
            return MLoader;
        }

        public void OnEdit(bool inSession) {
            // do nothing
        }

        public void MessageUpwards(string method, object args) {
            transform.SendMessageUpwards(method, args, SendMessageOptions.DontRequireReceiver);
        }

        VirgisFeature IVirgisLayer.GetFeature(Guid id) {
            throw new NotImplementedException();
        }

        VirgisFeature IVirgisEntity.GetClosest(Vector3 coords, Guid[] exclude) {
            throw new NotImplementedException();
        }

        public virtual Dictionary<string, string> GetInfo() {
            Dictionary<string, string> ret = new();
            RecordSetPrototype recordSet = GetMetadata();
            ret.Add("Name", recordSet.DisplayName);
            ret.Add("Source", Path.GetFileName(recordSet.Source));
            ret.Add("Editable?", IsWriteable? "Yes" : "No");
            return ret;
        }

        [Rpc(SendTo.Server)]
        public virtual void AddFeatureRpc(Vector3[] verteces)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveVertex(Transform vertex) 
        {
            vertex.GetComponent<VirgisFeature>().RemoveFeatureRpc();
        }

        public virtual void AddVertex(Vector3 position) 
        {
            //do nothing
        }

        [Rpc(SendTo.Server)]
        public void RequestRedrawRpc()
        {
            changed = true;
            MLoader.ReadSymbology();
            _ = Draw();
        }

        [Rpc(SendTo.Server)]
        public void UpdateSymbologyRpc(string unitName, UnitPrototype unit)
        {
            MLoader.ChangeSymbology(unitName, unit);
        }
    }
}