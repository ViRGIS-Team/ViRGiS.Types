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

using Project;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace Virgis {

    // AppState is a global singleton object that stores
    // app states, such as EditSession, etc.
    //
    // Singleton pattern taken from https://learn.unity.com/tutorial/level-generation
    public interface IState  {
        static IState instance = null;
        Vector3 lastHitPosition { get; set; }
        List<Coroutine> tasks { get; set; }
        int editScale { get; set; } 
        int currentView { get; set; }

        /// <summary>
        /// Shows if there is interaction with the gui
        /// </summary>
        bool guiActive {
            get {
                return lhguiActive || rhguiActive;
            }
        }
        bool lhguiActive { get; set; }
        bool rhguiActive { get; set; }

        /// <summary>
        /// Use this to get and change the view orientation
        /// </summary>
        OrientEvent Orientation {
            get;
        }

        /// <summary>
        /// Use this to Show text in the Infoi Panel
        /// </summary>
        InfoEvent Info {
            get;
        }

        /// <summary>
        /// Use this to change the Zoom Level
        /// </summary>
        ZoomEvent Zoom {
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
        ProjectChange Project {
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
        /// Init is called after a project has been fully loaded.
        /// </summary>
        /// 
        /// Call this method everytime a new project has been loaded,
        /// e.g. New Project, Open Project
        void Init() { }

        /// <summary>
        /// Event for if the Map is current in an edit session
        /// </summary>
        EditSession editSession { get; }

        /// <summary>
        /// Use this to change or get the project
        /// </summary>
        GisProject project {
            get {
                return Project.Get();
            } 
            set {
                Project.Set(value);
            } 
        }

        /// <summary>
        /// List of all of the layers of the model
        /// </summary>
        List<IVirgisLayer> layers {
            get;
        }

        /// <summary>
        /// Add a layer to the model
        /// </summary>
        /// <param name="layer"></param>
        void addLayer(IVirgisLayer layer);

        /// <summary>
        /// remove all layers from the model
        /// </summary>
        public void clearLayers();

        /// <summary>
        /// Get and set the main camera
        /// </summary>
        Camera mainCamera {
            get; set;
        }

        /// <summary>
        /// Get and Set the tracking space for this user
        /// </summary>
        Transform trackingSpace {
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
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns> a number representing the scale set</returns>
        float SetScale(float zoom);

    }

    public class State : MonoBehaviour, IState
    {

        public static State instance = null;
        public Vector3 lastHitPosition
        {
            get; set;
        }
        public List<Coroutine> tasks
        {
            get; set;
        }
        public int editScale
        {
            get; set;
        }
        public int currentView
        {
            get; set;
        }

        public bool guiActive
        {
            get
            {
                return lhguiActive || rhguiActive;
            }
        }
        public bool lhguiActive { get; set; } = false;
        public bool rhguiActive { get; set; } = false;

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

        public ZoomEvent Zoom
        {
            get;
            protected set;
        }

        public ButtonStatus ButtonStatus
        {
            get;
            protected set;
        }

        public ProjectChange Project
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
        
        protected EditSession _editSession
        {
            get; set;
        }
        protected List<IVirgisLayer> _layers;

        public virtual void Init() { }

        public GisProject project
        {
            get
            {
                return Project.Get();
            }
            set
            {
                Project.Set(value);
            }
        }

        public EditSession editSession
        {
            get => _editSession;
        }


        public GameObject map
        {
            get; set;
        }

        public List<IVirgisLayer> layers
        {
            get => _layers;
        }

        public void addLayer(IVirgisLayer layer)
        {
            _layers.Add(layer);
            if (_layers.Count == 1)
                _editSession.editableLayer = _layers[0];
            if (_layers.Count == 2 && _layers[0].GetMetadata().DataType == RecordSetDataType.MapBox)
                _editSession.editableLayer = _layers[1];
            LayerUpdate.AddLayer(layer);
        }

        public void clearLayers()
        {
            _layers.Clear();
        }

        public Camera mainCamera
        {
            get; set;
        }

        public Transform trackingSpace
        {
            get; set;
        }

        public bool InEditSession()
        {
            return _editSession.IsActive();
        }

        public void StartEditSession()
        {
            _editSession.Start();
            editScale = 5;
        }

        public void StopSaveEditSession()
        {
            _editSession.StopAndSave();
        }

        public void StopDiscardEditSession()
        {
            _editSession.StopAndDiscard();
        }

        public virtual void SetConfig(string key, object value)
        {
            throw new NotImplementedException();
        }

        public virtual object GetConfig(string key)
        {
            throw new NotImplementedException();
        }

        public virtual float SetScale(float zoom)
        {
            throw new NotImplementedException();
        }
    }
}