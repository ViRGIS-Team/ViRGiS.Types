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
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Mapbox.Json;
using UnityEngine.Serialization;

namespace Virgis
{
    public interface IVirgisLoader : IVirgisLayer
    {
        /// <summary>
        /// Called to add feature to the layer dataset
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="geometry"></param>
        /// <returns></returns>
        IVirgisFeature _addFeature<T1>(T1 geometry);
        

        /// <summary>
        /// Called to Draw the dataset
        /// </summary>
        /// <returns></returns>
        Task _draw();

        /// <summary>
        /// Implement the layer specific init code in this method
        /// </summary>
        /// <returns></returns>
        Task _init();

        /// <summary>
        /// Implement the layer specific save code in this method
        /// </summary>
        /// <returns></returns>
        Task _save();

        void _set_visible();

        /// <summary>
        /// Change a Unit of Symbology in the Loader
        /// </summary>
        /// <param name="name"> string name of the unit</param>
        /// <param name="unit"> UnitPrototype</param>
        public void ChangeSymbology(string name, UnitPrototype unit);

        /// <summary>
        /// Tells the Loader to read and implement the symbology
        /// </summary>
        public void ReadSymbology();

        /// <summary>
        /// Takes a checkpoint of the Symbology as a JSON string
        /// </summary>
        public void CheckpointSymbology();
        
        /// <summary>
        /// Revert to the symbology checkpoint
        /// </summary>
        public void RevertSymbology();
    }
    
    public class VirgisLoader<S> : NetworkBehaviour, IVirgisLoader
    {
        protected enum EColorInterp
        {
            Interpolate,
            CategoryValue,
            CategoryList,
            None
        }

        public S features; // holds the feature data for this layer
        [FormerlySerializedAs("Grad")] public Gradient grad;

        protected VirgisLayer MParent; // holds the parent VirgisLayer
        protected object MCrs;
        protected Dictionary<string, SerializableMaterialHash> MMaterials = new();
        protected float MDisplacement;
        protected EColorInterp MColorInterp = EColorInterp.None;
        protected Dictionary<string, UnitPrototype> MSymbology;
        
        private string _sSymbologyCheckpoint;

        public RecordSetPrototype _layer
        {
            get => MParent?.GetMetadata();
            set
            { if (MParent != null) MParent.SetMetadata(value); }
        }

        public string sourceName { get => MParent?.sourceName;
            set 
            { if ( MParent != null) MParent.sourceName = value;} 
        }

        public List<IVirgisLayer> subLayers
        {
            get { return MParent?.subLayers;}
            }


        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed
        {
            get => MParent?.changed ?? false;
            set
            {
                if (MParent != null) MParent.changed = value;
            }
        }

        public bool isContainer => MParent?.isContainer ?? false;

        public FeatureType featureType => throw new NotImplementedException();

        public bool IsEditable => MParent?.IsEditable ?? false;

        public bool IsWriteable { 
            get => MParent?.IsWriteable ?? false;
            set {
                if (MParent != null) MParent.IsWriteable = value;
            } }

        protected IVirgisLoader MLoader;

        protected void Awake()
        {
            MParent = GetComponent<VirgisLayer>();
        }

        public virtual IVirgisFeature _addFeature<T>(T geometry)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task _draw()
        {
            throw new System.NotImplementedException();
        }

        public virtual Task _init()
        {
            throw new System.NotImplementedException();
        }

        public virtual Task _save()
        {
            throw new System.NotImplementedException();
        }

        public void SetFeatures(S theseFeatures)
        {
            this.features = theseFeatures;
        }

        /// <summary>
        /// Set the Layer CRS
        /// </summary>
        /// <param name="crs">SpatialReference</param>
        public void SetCrs(object crs)
        {
            MCrs = crs;
        }

        public object GetCrsRaw()
        {
            return MCrs;
        }

        public virtual void _set_visible()
        {
            throw new NotImplementedException();
        }

        public virtual VirgisFeature AddFeature<T>(T geometry)
        {
            throw new NotImplementedException();
        }

        public virtual bool Load(string file)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerator Init(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task AsyncInit(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task SubInit(RecordSetPrototype layer)
        {
            throw new NotImplementedException();
        }

        public virtual Task Draw()
        {
            throw new NotImplementedException();
        }

        public virtual RecordSetPrototype Save()
        {
            throw new NotImplementedException();
        }

        public virtual VirgisFeature GetFeature(Guid id)
        {
            throw new NotImplementedException();
        }

        public virtual Shapes GetFeatureShape()
        {
            return Shapes.None;
        }

        public virtual RecordSetPrototype GetMetadata()
        {
            return _layer;
        }

        public virtual void SetMetadata(RecordSetPrototype meta)
        {
            _layer = meta;
        }

        public virtual void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsVisible()
        {
            throw new NotImplementedException();
        }

        public virtual void SetEditable(bool inSession)
        {
            throw new NotImplementedException();
        }


        public void MessageUpwards(string method, object args)
        {
            throw new NotImplementedException();
        }

        public void Selected(SelectionType button)
        {
            //Do Nothing
        }

        public void UnSelected(SelectionType button)
        {
            // Do Nothing
        }

        public ulong GetId()
        {
            return MParent.GetId();
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
            return MParent;
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

        public virtual void SetMaterial(string idx, Color color, Dictionary<string, float> properties = null)
        {
            throw new NotImplementedException();
        }

        public Material GetMaterial(string idx)
        {
            throw new NotImplementedException();
        }

        public void Loaded(VirgisLayer layer)
        {
            throw new NotImplementedException();
        }

        public void SetupColormap(UnitPrototype unit)
        {
            if (unit.ColorMap != null)
            {
                if (unit.ColorMode == ColorMode.SinglebandColor)
                {
                    if (unit.ColorMap.Type == ColorMapType.Interpolate)
                    {
                        grad = unit.ColorMap.GetGradient();
                        MColorInterp = EColorInterp.Interpolate;
                    }
                    else
                    {
                        MColorInterp = EColorInterp.CategoryValue;
                    }
                }
                else if (unit.ColorMode == ColorMode.Category)
                {
                    if (unit.ColorMap.Type == ColorMapType.Categorize)
                    {
                        grad = unit.ColorMap.GetGradient();
                        MColorInterp = EColorInterp.CategoryList;
                    }
                }
            }
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
            // do nothing
        }

        public void AddVertex(Vector3 pos)
        {
            // do nothing
        }

        public void Changed()
        {
            throw new NotImplementedException();
        }

        public void ChangeSymbology(string unitName, UnitPrototype unit)
        {
            MSymbology[unitName] = unit;
        }

        public virtual void ReadSymbology()
        {
            throw new NotImplementedException();
        }

        public void CheckpointSymbology()
        {
            _sSymbologyCheckpoint = JsonConvert.SerializeObject(MSymbology);
        }

        public void RevertSymbology()
        {
            MSymbology = JsonConvert.DeserializeObject<Dictionary<string, UnitPrototype>>(_sSymbologyCheckpoint);
            GetMetadata().Units = MSymbology;
        }
    }
}
