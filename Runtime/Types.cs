using System;
using System.Net;
using UnityEngine;

namespace Virgis
{
    /// <summary>
    /// Structure used to hold the details of a generic move request sent to a target enitity
    /// </summary>
    public struct MoveArgs
    {
        public Guid id; // id of the sending entity
        public Vector3 pos; // OPTIONAL point to move TO in world space coordinates
        public Vector3 translate; // OPTIONSAL translation in world units to be applied to target
        public Quaternion rotate; // OPTIONAL rotation to be applied to target
        public Vector3 oldPos; // OPTIONAL point to move from
        public float scale; // OPTIONAL change in scale to apply to target
    }

    /// <summary>
    /// Enum holding the types of "selection"tha the user can make
    /// </summary>
    public enum SelectionType
    {
        SELECT,     // Select a sing;le vertex
        SELECTALL,  // Select all verteces
        MOVEAXIS,   // select for Rotation and sizing event
        INFO,       // Slection Actin related to the Info screen
        BROADCAST   // Selection event rebroadcast by parent event. DO NOT retransmit to avoid endless circles
    }

    public enum FeatureType
    {
        POINT,
        LINE,
        POLYGON,
        MESH,
        POINTCLOUD,
        RASTER,
        MAP,
        CUSTOM_FEATURE
    }

    public struct VirgisServerDetails
    {
        public IPEndPoint Endpoint;
        public string ServerName;
        public string ModelName;
    }
}
