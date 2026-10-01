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
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Serialization;

namespace Virgis {


    public abstract class VirgisFeature : NetworkBehaviour, IVirgisFeature
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int TextureSwitch = Shader.PropertyToID("_TextureSwitch");

        /// <summary>
        /// The Symbology for this Feature
        /// </summary>
        public Dictionary<string, UnitPrototype> Symbology = new();
        /// <summary>
        /// The Label object for this feature
        /// </summary>
        [FormerlySerializedAs("Label")] public Transform label;
        [FormerlySerializedAs("Texture")] public SerializableTexture texture = new();
        [FormerlySerializedAs("MeshRenderer")] public MeshRenderer meshRenderer;

        protected Material MMaterial;
        protected readonly List<IDisposable> MSubs = new();
        protected readonly NetworkVariable<SerializableMaterialHash> MCol = new();
        protected VirgisFeatureState MState = new VirgisFeatureState() {
            FirstHitPosition = Vector3.zero,
            NullifyHitPos = true,
            BlockMove = false
        };
        private object _mFid;
        
        protected bool IsListening => NetworkManager.Singleton is not null && NetworkManager.Singleton.IsListening;

        public virtual void Start()
        {

        }

        public override void OnNetworkSpawn()
        {
            if (meshRenderer)
            {
                MMaterial = meshRenderer.material;
            }
            base.OnNetworkSpawn();
            if (texture.tex) SetTexture(texture.tex);
            UpdateMaterial(new(), MCol.Value);
            MCol.OnValueChanged += UpdateMaterial;
            texture.OnValueChanged += SetTexture;
        }

        public override void OnNetworkDespawn()
        {
            MCol.OnValueChanged -= UpdateMaterial;
            texture.OnValueChanged -= SetTexture;
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            Destroy(MMaterial);
            MSubs.ForEach(item => item.Dispose());
            base.OnDestroy();
        }

        public virtual void UpdateMaterial(SerializableMaterialHash previousValue, SerializableMaterialHash newValue)
        {
            if (newValue.Equals(previousValue)) return;
            MMaterial.SetColor(BaseColor, newValue.Color);
            if (newValue.properties == null) return;
            foreach (SerializableProperty prop in newValue.properties)
            {
                MMaterial.SetFloat(prop.Key.ToString(), prop.Value);
            }
        }

        public virtual void SetMaterial(SerializableMaterialHash hash)
        {
            MCol.Value = hash;
        }

        public virtual void SetTexture(Texture2D tex)
        {
            MMaterial.SetColor(BaseColor, Color.white);
            MMaterial.SetTexture(BaseMap, tex);
            MMaterial.SetFloat(TextureSwitch, 1f);
        }

        /// <summary>
        /// 
        /// Burns the entire object tree of which this is the trunk
        /// starting from the leaves first. Only safe way to destroy 
        /// the ViRGiS tree on a networked version
        /// </summary>
        public void Destroy()
        {
            for (int i = transform.childCount -1; i>=0;  i--)
            {
                if(transform.GetChild(i).TryGetComponent(out VirgisFeature com )){
                    com.Destroy();
                }
            }
            DeSpawn(transform);
            Destroy(gameObject);
        }

        public bool Spawn(Transform parent)
        {
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


        /// <summary>
        /// Use to tell the Component that it is selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void Selected(SelectionType button) {
            MState.NullifyHitPos = true;
            if (button != SelectionType.BROADCAST)
                transform.parent.GetComponent<IVirgisEntity>().Selected(button);
            if (button == SelectionType.SELECTALL) {
                m_SetBlockMove(true);
            }
        }

        /// <summary>
        /// Use to tell the Component that it is un selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void UnSelected(SelectionType button) {
            if (button != SelectionType.BROADCAST)
                transform.parent.GetComponent<IVirgisEntity>().UnSelected(button);
            m_SetBlockMove(false);
        }

        /// <summary>
        /// Used to signal to the hierarchy that a substantive change has been made
        /// </summary>
        public virtual void Changed()
        {
            transform.parent.GetComponent<IVirgisEntity>().Changed();
        }

        /// <summary>
        /// Called to Set the Feature State of the feature:
        /// - Sets the BlockMove State from the VirgisFeatureState object
        /// </summary>
        /// <param name="state"></param>
        public virtual void SetFeatureState(VirgisFeatureState state)
        {
            m_SetBlockMove(state.BlockMove);
            transform.parent.SendMessageUpwards("SetFeatureState",MState,SendMessageOptions.DontRequireReceiver);
        }

        protected void m_SetBlockMove(bool state) {
            MState.BlockMove = state;
        }


        /// <summary>
        /// Sent by the UI to request this component to move.
        /// </summary>
        /// <param name="args">MoveArgs : Either a translation vector OR a Vector position to move to, both in World space coordinates</param>
        public virtual void MoveTo(MoveArgs args)
        {
            MoveToRpc(args, MState, ! IsServer);
        }

        [Rpc(SendTo.Server)]
        protected void MoveToRpc(MoveArgs args, VirgisFeatureState state, bool fromClient)
        {
            Changed();
            MState = state;
            if (fromClient)
            {
                SetFeatureState(state);
            }
            _move(args);
        }

        protected virtual void _move(MoveArgs args)
        {
            //do nothing
        }

        /// <summary>
        /// received when a Move Axis request is made by the user
        /// </summary>
        /// <param name="args">The move argumants structure holding the new position</param>
        public void MoveAxis(MoveArgs args)
        {
            if (MState.NullifyHitPos)
            {
                MState.FirstHitPosition = args.pos;
                MState.NullifyHitPos = false;
            } else
            {
                args.pos = MState.FirstHitPosition;
            }
            MoveAxisRpc(args, MState);
        }

        [Rpc(SendTo.Server)]
        protected void MoveAxisRpc(MoveArgs args, VirgisFeatureState state) {
            MState = state;
            _moveAxis(args);
            Changed();
        }

        protected virtual void _moveAxis(MoveArgs args) { 
            args.id = GetId();
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
        }

        /// <summary>
        /// Called when a child component is translated by User action
        /// </summary>
        /// <param name="args">MoveArgs</param>
        public virtual void Translate(MoveArgs args) {
            //do nothing
        }

        /// <summary>
        /// Called when a child Vertex moves to the point in the MoveArgs - which is in World Coordinates
        /// </summary>
        /// <param name="args">MoveArgs</param> m>
        public virtual void VertexMove(MoveArgs args) {
            transform.parent.SendMessage("VertexMove", args, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Gets the closest point of the feature geometry to the coordinates
        /// </summary>
        /// <param name="coords"> Vector3 Target Coordinates </param>
        /// <param name="exclude"> exclude list based on GetId()</param>
        /// <returns> Vector3 in world space coordinates </returns>
        public virtual VirgisFeature GetClosest(Vector3 coords, Guid[] exclude) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// call this to add a vertex to a feature.
        /// </summary>
        /// <param name="position">Vector3</param>
        /// <returns>VirgisComponent The new vertex</returns>
        /// 
        public virtual void AddVertex(Vector3 position)
        {
            //do nothing
        }

        /// <summary>
        /// call this to remove this vertex from a feature
        /// </summary>
        public virtual void RemoveVertex(Transform vertex = null)
        {
            if (vertex == null) vertex = transform;
            GetParent(out IVirgisEntity parent);
            parent.RemoveVertex(vertex);
        }

        [Rpc(SendTo.Server)]
        public virtual void RemoveFeatureRpc() {
            Destroy();
            Changed();
        }


        /// <summary>
        /// Get Geometry from the Feature
        /// </summary>
        /// <typeparam name="T">The Type of the geometry</typeparam>
        /// <returns> Gemoetry of type T </returns>
        public virtual T GetGeometry<T>() {
            throw new NotImplementedException();
        }

        public ulong GetId() {
            return NetworkObject.NetworkObjectId;
        }

        public virtual Dictionary<string, string> GetInfo()
        {
            if (GetParent(out IVirgisEntity parent))
            {
                return parent.GetInfo();
            }
            return default;
        }

        public virtual void SetInfo(Dictionary<string, object> meta)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            VirgisFeature com = obj as VirgisFeature;
            if (com == null)
                return false;
            else
                return Equals(com);
        }
        public override int GetHashCode() {
            return (int)GetId();
        }
        public bool Equals(VirgisFeature other) {
            if (!other)
                return false;
            return (this.GetId().Equals(other.GetId()));
        }

        /// <summary>
        /// Called when the pointer hovers on this feature
        /// </summary>
        public void Hover() {
            MState.LastHit = State.instance.lastHit.point;
            Dictionary<string, string> meta = GetInfo();
            if (meta != null && meta.Count > 0) {
                string output = string.Join("\n", meta.Select(x => $"{x.Key}:\t{x.Value}"));
                State.instance.Info.Set(output);
            }
        }

        /// <summary>
        /// called when the pointer stops hovering on this feature
        /// </summary>
        public void UnHover() {
            if ( ! State.instance.ButtonStatus.isRhGrip ) {
                State.instance.Info.UnSet();
            }
        }

        public IVirgisLayer GetLayer() {
            if (GetParent(out IVirgisEntity parent))
            {
                return parent.GetLayer();
            }
            return null;
        }

        public bool GetParent( out IVirgisEntity parent)
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

        public virtual void OnEditStart(bool save)
        {
            // do nothing
        }

        public virtual void OnEdit(bool inSession) {
            // do nothing
        }

        public virtual void OnEditEnd(bool save)
        {
            // do nothing
        }

        public virtual Dictionary<string, object> GetInfo(VirgisFeature feat)
        {
            return default;
        }

        public void SetFID<T>(T fid)
        {
            _mFid = fid;
        }

        public T GetFID<T>()
        {
            return (T)_mFid;
        }
    }
}
