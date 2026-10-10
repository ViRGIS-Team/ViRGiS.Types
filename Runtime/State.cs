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

using System.Collections.Generic;
using UnityEngine;
using R3;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Serialization;

namespace Virgis {

    // AppState is a global singleton object that stores
    // app states, such as EditSession, etc.
    //
    // Singleton pattern taken from https://learn.unity.com/tutorial/level-generation
    public interface IState  {
        static IState Instance;
        int EditScale { get; set; } // holds the current Edit Svcal
        int CurrentView { get; set; } // holds the current view number
        string UserID { get; set; } // holds a user identity
        object Token { get; set; } // allows the storing of an arbitrary licence token object

        /// <summary>
        /// Shows if there is interaction with the gui
        /// </summary>
        bool GUIActive => LhguiActive || RhguiActive;

        bool LhguiActive { get; set; }
        bool RhguiActive { get; set; }

        /// <summary>
        /// Use this to get and change the view orientation
        /// </summary>
        OrientEvent Orientation {
            get;
        }

        /// <summary>
        /// usde this to change the apparent scale of the model
        /// </summary>
        public ZoomEvent MapScale
        {
            get;
        }

        /// <summary>
        /// Use this to Show text in the Info Panel
        /// </summary>
        InfoEvent Info {
            get;
        }

        /// <summary>
        /// Use this to change the Button Status
        /// </summary>
        ButtonStatus ButtonStatus
        {
            get;
        }

        /// <summary>
        /// Use this to get the project change event
        /// </summary>
        ProjectChange ProjectChange {
            get;
        }

        /// <summary>
        /// Event that is triggered when a layer is added
        /// </summary>
        LayerChange LayerUpdate {
            get;
        }

        /// <summary>
        /// UniRx Subject that is triggered when a new configuration is loaded.
        /// </summary>
       BehaviorSubject<bool> ConfigEvent { get; }

        /// <summary>
        /// Holds the list of servers currently resgistered with the client
        /// </summary>
        List<VirgisServerDetails> Servers { get; }

        /// <summary>
        /// Register a server qwith this client
        /// </summary>
        /// <param name="details">Server Details as VirgisServerDetails</param>
        void RegisterServer(VirgisServerDetails details) { }

        /// <summary>
        /// Init is called after a project has been fully loaded.
        /// </summary>
        /// 
        /// Call this method everytime a new project has been loaded,
        /// e.g. New Project, Open Project
        void Init() { }

        /// <summary>
        /// Event for if the Map is current in an edit session
        /// </summary>
        EditSession EditSession { get; }

        /// <summary>
        /// Use this to change or get the project
        /// </summary>
        GisProjectPrototype Project {
            get => ProjectChange.Get();
            set => ProjectChange.Set(value);
        }

        /// <summary>
        /// List of all the layers of the model
        /// </summary>
        List<VirgisLayer> Layers {
            get;
        }

        /// <summary>
        /// Add a layer to the model
        /// </summary>
        /// <param name="layer"></param>
        void AddLayer(VirgisLayer layer);

        /// <summary>
        /// remove a layer from the model
        /// </summary>
        void DelLayer(VirgisLayer layer);

        /// <summary>
        /// Get and set the main camera
        /// </summary>
        Camera MainCamera {
            get; set;
        }

        /// <summary>
        /// Get and Set the tracking space for this user
        /// </summary>
        Transform TrackingSpace {
            get; set;
        }

        /// <summary>
        /// Flag for if the map is in an edit session
        /// </summary>
        /// <returns></returns>
        bool InEditSession();

        /// <summary>
        /// Start an edit session
        /// </summary>
        void StartEditSession();

        /// <summary>
        /// Stop an edit session and svae the results
        /// </summary>
        void StopSaveEditSession();

        /// <summary>
        /// Stop an edit session and discard the reulst
        /// </summary>
        void StopDiscardEditSession();

        /// <summary>
        /// Courtesy function to return a configuration object
        /// </summary>
        /// <returns></returns>
        object ConfigObject();

        /// <summary>
        /// Courtesy function to allow the creation of logic to set configuration items
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetConfig(string key, object value);

        /// <summary>
        /// Courtesy Function to allow the retrieval of Configuration items
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object GetConfig(string key);

        /// <summary>
        /// Sets the map scale
        /// </summary>
        /// <param name="scale"></param>
        /// <returns> a number representing the scale set</returns>
        void SetScale(float scale);

        bool LoadProject(string path);

        void UnloadProject(Action callback);

        /// <summary>
        /// Get a unique session ID for this client
        /// </summary>
        /// <returns>Guid</returns>
        public Guid Guid
        {
            get;
        }

    }

    public abstract class State : MonoBehaviour, IState
    {
        public static State Instance { get; protected set; }

        public RaycastHit LastHit = new();
        
        /// <summary>
        /// Create custom ex ception handler
        /// </summary>
        private void Start()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                OnUnhandledException;

            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            AppDomain.CurrentDomain.UnhandledException -=
                OnUnhandledException;

            Application.logMessageReceived -= OnLogMessageReceived;
        }

        protected abstract void OnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs args);

        protected abstract void OnLogMessageReceived(
            string condition,
            string stackTrace,
            LogType type);

        public int EditScale
        {
            get; set;
        }
        public int CurrentView
        {
            get; set;
        }

        public string UserID {
            get; set;
        }

        public object Token
        {
            get; set;
        }

        public bool GUIActive => LhguiActive || RhguiActive;
        public bool LhguiActive { get; set; }
        public bool RhguiActive { get; set; } 

        public OrientEvent Orientation
        {
            get;
            protected set;
        }

        public InfoEvent Info
        {
            get;
            protected set;
        }

        public ZoomEvent MapScale
        {
            get;
            protected set;
        }

        public GridEvent GridScale
        {
            get;
            protected set;
        }

        public ButtonStatus ButtonStatus
        {
            get;
            protected set;
        }

        public ProjectChange ProjectChange
        {
            get;
            protected set;
        }

        public LayerChange LayerUpdate
        {
            get;
            protected set;
        }

        public BehaviorSubject<bool> ConfigEvent { get; private set; } = new BehaviorSubject<bool>(false);


        
        public virtual GisProjectPrototype Project
        {
            get => ProjectChange.Get();
            set => ProjectChange.Set(value);
        }

        public EditSession EditSession { get; protected set; }


        public GameObject Map
        {
            get; set;
        }

        [FormerlySerializedAs("MapInitialize")] public MapInitializePrototype mapInitialize;

        [FormerlySerializedAs("NetworkState")] public VirgisNetworkState networkState;

        public List<VirgisLayer> Layers
        {
            get;
            private set;
        } = new List<VirgisLayer>();

        /// <summary>
        /// Project startup tasks
        /// </summary>
        public abstract void InitProj();

        public virtual void AddLayer(VirgisLayer layer)
        {
            Layers.Add(layer);
            LayerUpdate.AddLayer(layer);
        }

        public virtual void DelLayer(VirgisLayer layer)
        {
            LayerUpdate.DelLayer(layer);
        }

        public Camera MainCamera
        {
            get; set;
        }

        public Transform TrackingSpace
        {
            get; set;
        }

        public List<VirgisServerDetails> Servers { get; private set; } = new();

        public BehaviorSubject<VirgisServerDetails> ServerEvent { get; private set; } = new BehaviorSubject<VirgisServerDetails>(new());

        public void RegisterServer(VirgisServerDetails details) 
        {
            Servers.Add(details);
            ServerEvent.OnNext(details);
        }

        public ClientConnect Client { get; private set; } = new();

        public virtual void ConnectClient(VirgisServerDetails details)
        {
            Client.Start();
            NetworkManager nm = NetworkManager.Singleton;
            UnityTransport unityTransport = nm.GetComponent<UnityTransport>();
            if (!nm.IsConnectedClient)
            {
                unityTransport.ConnectionData.Address = details.Endpoint.Address.ToString();
                unityTransport.ConnectionData.Port = (ushort)details.Endpoint.Port;
                nm.NetworkConfig.ClientConnectionBufferTimeout = 120;
                if (!nm.StartClient())
                {
                    Client.Failed();
                } 
            }
        }

        public void ClearServers()
        {
            Servers = new();
            ServerEvent.OnNext(new());
        }

        public bool InEditSession()
        {
            return EditSession.IsActive();
        }

        public void StartEditSession()
        {
            EditSession.Start();
            EditScale = 5;
        }

        public void StopSaveEditSession()
        {
            EditSession.StopAndSave();
        }

        public void StopDiscardEditSession()
        {
            EditSession.StopAndDiscard();
        }

        public virtual object ConfigObject()
        {
            throw new NotImplementedException();
        }

        public virtual void SetConfig(string key, object value)
        {
            throw new NotImplementedException();
        }

        public virtual object GetConfig(string key)
        {
            throw new NotImplementedException();
        }

        public virtual void SetScale(float scale)
        {
            MapScale.OnNext(scale);
        }

        public bool LoadProject(string path)
        {
            return mapInitialize.Load(path);
        }

        public void UnloadProject(Action callback = null)
        {
            //If Server ...Kill all map entities
            if ( ! ( NetworkManager.Singleton.IsListening && ! NetworkManager.Singleton.IsServer ) )
            {
                if (Map)
                {
                    for (int i = Map.transform.childCount - 1; i >= 0; i--)
                    {
                        if (Map.transform.GetChild(i).TryGetComponent(out VirgisLayer sublayer))
                        {
                            sublayer.Destroy();
                        }
                    }
                    Map.GetComponent<NetworkObject>().Despawn();
                }
            }
            callback?.Invoke();
        }

        public ulong Hash
        {
            get
            {
                Span<byte> bytes = stackalloc byte[16];
                Guid.TryWriteBytes(bytes);
                return BitConverter.ToUInt64(bytes);
            }
        }

        public Guid Guid { get; } = Guid.NewGuid();

        public Task Exit()
        {
            Debug.Log("QuitButton.OnClick now quit");
            UnloadProject();
            NetworkManager.Singleton.Shutdown();
            Application.Quit();
            return Task.CompletedTask;
        }
    }
}
