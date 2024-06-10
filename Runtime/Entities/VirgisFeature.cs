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

namespace Virgis {


    public abstract class VirgisFeature : NetworkBehaviour, IVirgisFeature
    {
        /// <summary>
        /// The Symbology for this Feature
        /// </summary>
        public Dictionary<string, UnitPrototype> Symbology = new();
        /// <summary>
        /// The Label object for this feature
        /// </summary>
        public Transform Label;

        protected MeshRenderer m_Mr;
        protected Material m_Material;
        protected readonly List<IDisposable> m_Subs = new();
        protected NetworkVariable<SerializableMaterialHash> m_Col = new();
        protected VirgisFeatureState m_State = new VirgisFeatureState() {
            FirstHitPosition = Vector3.zero,
            NullifyHitPos = true,
            BlockMove = false
        };

        private Guid m_Id; // internal ID for this component - used when it is part of a larger structure
        private object m_FID;

        void Awake()
        {
            m_Id = Guid.NewGuid();
        }

        public void Start()
        {

        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (TryGetComponent<MeshRenderer>(out m_Mr))
            {
                m_Material = m_Mr.material;
                m_Col.OnValueChanged += UpdateMaterial;
                UpdateMaterial(new(), m_Col.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            m_Col.OnValueChanged -= UpdateMaterial;
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            m_Subs.ForEach(item => item.Dispose());
            base.OnDestroy();
        }

        public void UpdateMaterial(SerializableMaterialHash previousValue, SerializableMaterialHash newValue)
        {
            if (newValue.Equals(previousValue)) return;
            m_Material.SetColor("_BaseColor", newValue.Color);
            if (newValue.properties == null) return;
            foreach (SerializableProperty prop in newValue.properties)
            {
                m_Material.SetFloat(prop.Key.ToString(), prop.Value);
            }
        }

        public void SetMaterial(SerializableMaterialHash hash)
        {
            m_Col.Value = hash;
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
                if(transform.GetChild(i).TryGetComponent(out VirgisFeature com )){
                    com.Destroy();
                    DeSpawn(com.transform);
                }
            }
            DeSpawn(transform);
        }

        public bool Spawn(Transform parent)
        {
            NetworkObject no = gameObject.GetComponent<NetworkObject>();
            try
            {
                no.Spawn();
            }
            catch (Exception e)
            {
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


        /// <summary>
        /// Use to tell the Component that it is selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void Selected(SelectionType button) {
            m_State.NullifyHitPos = true;
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
        /// Called to Set the Feature State of the feature:
        /// - Sets the BlockMove State from the VirgisFeatureState object
        /// </summary>
        /// <param name="state"></param>
        public virtual void SetFeatureState(VirgisFeatureState state)
        {
            m_SetBlockMove(state.BlockMove);
            transform.parent.SendMessageUpwards("SetFeatureState",m_State,SendMessageOptions.DontRequireReceiver);
        }

        protected void m_SetBlockMove(bool state) {
            m_State.BlockMove = state;
        }


        /// <summary>
        /// Sent by the UI to request this component to move.
        /// </summary>
        /// <param name="args">MoveArgs : Either a translation vector OR a Vector position to move to, both in World space coordinates</param>
        public void MoveTo(MoveArgs args)
        {
            MoveToRpc(args, m_State, ! IsServer);
        }

        [Rpc(SendTo.Server)]
        protected void MoveToRpc(MoveArgs args, VirgisFeatureState state, bool fromClient)
        {
            m_State = state;
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
            if (m_State.NullifyHitPos)
            {
                m_State.FirstHitPosition = args.pos;
                m_State.NullifyHitPos = false;
            } else
            {
                args.pos = m_State.FirstHitPosition;
            }
            MoveAxisRpc(args, m_State);
        }

        [Rpc(SendTo.Server)]
        protected void MoveAxisRpc(MoveArgs args, VirgisFeatureState state) {
            m_State = state;
            _moveAxis(args);
        }

        protected virtual void _moveAxis(MoveArgs args) { 
            args.id = GetId();
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
        }

        /// <summary>
        /// Called when a child component is translated by User action
        /// </summary>
        /// <param name="args">MoveArgs</param>
        /// <param name="state">The state structure for the client feature object</param>
        public virtual void Translate(MoveArgs args) {
            //do nothing
        }

        /// <summary>
        /// Called when a child Vertex moves to the point in the MoveArgs - which is in World Coordinates
        /// </summary>
        /// <param name="data">MoveArgs</param>
        /// <param name="state">The state structure for the client feature object</param>
        public virtual void VertexMove(MoveArgs args) {
            transform.parent.SendMessage("VertexMove", args, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Gets the closest point of the feature geometry to the coordinates
        /// </summary>
        /// <param name="coords"> Vector3 Target Coordinates </param>
        /// <returns> Vector3 in world space coordinates </returns>
        public virtual VirgisFeature GetClosest(Vector3 coords, Guid[] exclude) {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// call this to add a vertex to a feature.
        /// </summary>
        /// <param name="position">Vector3</param>
        /// <returns>VirgisComponent The new vertex</returns>
        [Rpc(SendTo.Server)]
        public virtual void AddVertexRpc(Vector3 position) {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// call this to remove a vertxe from a feature
        /// </summary>
        /// <param name="vertex">Vertex to remove</param>
        //[Rpc(SendTo.Server)]
        public virtual void RemoveVertexRpc(VirgisFeature vertex) {
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// Get Geometry from the Feature
        /// </summary>
        /// <typeparam name="T">The Type of the geometry</typeparam>
        /// <returns> Gemoetry of type T </returns>
        public virtual T GetGeometry<T>() {
            throw new System.NotImplementedException();
        }

        public Guid GetId() {
            return m_Id;
        }

        public abstract Dictionary<string, object> GetInfo();

        public abstract void SetInfo(Dictionary<string, object> meta);

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
            return m_Id.GetHashCode();
        }
        public bool Equals(VirgisFeature other) {
            if (other == null)
                return false;
            return (this.m_Id.Equals(other.GetId()));
        }

        /// <summary>
        /// Called when the pointer hovers on this feature
        /// </summary>
        public void Hover(Vector3 hit) {
            m_State.LastHit = hit;
            Dictionary<string, object> meta = GetInfo();
            if (meta != null && meta.Count > 0) {
                string output = string.Join("\n", meta.Select(x => $"{x.Key}:\t{x.Value}"));
                State.instance.Info.Set(output);
            }
        }

        /// <summary>
        /// called when the pointer stops hoveringon this feature
        /// </summary>
        public void UnHover() {
            State.instance.Info.Set("");
        }

        public IVirgisLayer GetLayer() {
            Transform parent = transform.parent; 
            if (parent != null)
            {
                return transform.parent.GetComponent<IVirgisEntity>()?.GetLayer();
            }
            return null;
        }

        public virtual void OnEdit(bool inSession) {
            // do nothing
        }

        public virtual Dictionary<string, object> GetInfo(VirgisFeature feat)
        {
            return default;
        }

        public void SetFID<T>(T FID)
        {
            m_FID = FID;
        }

        public T GetFID<T>()
        {
            return (T)m_FID;
        }
    }
}
