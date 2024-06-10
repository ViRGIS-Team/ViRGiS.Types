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
using System.Threading.Tasks;

namespace Virgis
{

    public interface IVirgisLayer : IVirgisEntity
    {

        /// <summary>
        /// The type of layer this is
        /// </summary>
        FeatureType featureType
        {
            get;
        }

        /// <summary>
        /// The display name of the source
        /// </summary>
        string sourceName
        {
            get; set;
        }

        /// <summary>
        /// Is this layer a container layer or a value layer
        /// </summary>
        public bool isContainer
        {
            get;
        }

        /// <summary>
        /// List of daighter layers to this layer
        /// </summary>
        List<IVirgisLayer> subLayers
        {
            get;
        }

        /// <summary>
        /// Does this layer, or a daughter layer, have changes that have not been saved
        /// </summary>
        public bool changed
        {
            get;
            set;
        }

        /// <summary>
        /// Add a new feature to the layer
        /// </summary>
        /// <typeparam name="T"> The type of the geometry - must match the type expected by the source loader</typeparam>
        /// <param name="geometry">The geometry of the new feature</param>
        /// <returns></returns>
        VirgisFeature AddFeature<T>(T geometry);

        /// <summary>
        /// Life cycle hook that is called on all layers to load the source
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        bool Load(string file);

        /// <summary>
        /// Lifecycle hook that is called to In initialise the layer
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        IEnumerator Init(RecordSetPrototype layer);
        Task AsyncInit(RecordSetPrototype layer);
        Task SubInit(RecordSetPrototype layer);

        /// <summary>
        /// Lifecycle hook that is called to draw the layer from the source data
        /// </summary>
        /// <returns></returns>
        Task Draw();

        /// <summary>
        /// Hook that is called when a sublayer has loaded succesfully
        /// </summary>
        /// <param name="layer"></param>
        void Loaded(VirgisLayer layer);

        /// <summary>
        /// Lifecycle hook that is called at the start of an edit session
        /// </summary>
        void CheckPoint();

        /// <summary>
        /// Called on a layer at the end of an edit session
        /// </summary>
        /// <returns></returns>
        Task<RecordSetPrototype> Save();

        /// <summary>
        /// Fetch a feature from a layer by GUID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        VirgisFeature GetFeature(Guid id);

        /// <summary>
        /// Fetch the feature shape
        /// </summary>
        /// <returns></returns>
        Shapes GetFeatureShape();

        /// <summary>
        /// Get the layer definition as a RecordSet
        /// </summary>
        /// <returns></returns>
        RecordSetPrototype GetMetadata();

        /// <summary>
        /// Set the layer definition
        /// </summary>
        /// <param name="meta"></param>
        void SetMetadata(RecordSetPrototype meta);

        /// <summary>
        /// Set the visibility of the layer (and sublayers)
        /// </summary>
        /// <param name="visible"></param>
        void SetVisible(bool visible);

        /// <summary>
        /// Checkiof the layer is currently visible
        /// </summary>
        /// <returns></returns>
        bool IsVisible();

        /// <summary>
        /// Is the layer currently available for editing.
        /// This is false if any client currently has the layer checked out 
        /// </summary>
        /// <returns></returns>
        bool IsEditable
        {
            get;
        }

        /// <summary>
        /// Called by a client/server to checkout and checkin a layer
        /// Note - checkin without saving first will delete all changes
        /// </summary>
        /// <param name="checkout"> true for a checkout</param>
        void SetEditable(bool checkout);

        /// <summary>
        /// Is this layer source writeable.
        /// If it is writeable - then data changes should not be allowed - but symbology changes are allowed
        /// </summary>
        /// <returns></returns>
        bool IsWriteable
        {
            get;
            set;
        }

        /// <summary>
        /// Call a methos on all parents in the object tree
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        void MessageUpwards(string method, object args);
    }
}