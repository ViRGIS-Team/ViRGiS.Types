using System;
using System.Net;
using UnityEngine;
using Unity.Netcode;

namespace Virgis
{
    /// <summary>
    /// Structure used to hold the details of a generic move request sent to a target enitity
    /// </summary>
    public struct MoveArgs: INetworkSerializable
    {
        public Guid id;
        public Vector3 pos; // OPTIONAL point to move TO in world space coordinates
        public Vector3 translate; // OPTIONSAL translation in world units to be applied to target
        public Quaternion rotate; // OPTIONAL rotation to be applied to target
        public Vector3 oldPos; // OPTIONAL point to move from
        public float scale; // OPTIONAL change in scale to apply to target

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref pos);
            serializer.SerializeValue(ref translate);
            serializer.SerializeValue(ref rotate);
            serializer.SerializeValue(ref oldPos);
            serializer.SerializeValue(ref scale);
        }
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

    public struct VirgisFeatureState : INetworkSerializable
    {
        public Vector3 FirstHitPosition;
        public bool NullifyHitPos; // set when the move axis should not be changed
        public bool BlockMove; // is entity in a block-move state
        public Vector3 LastHit; // last hit location

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref FirstHitPosition);
            serializer.SerializeValue(ref NullifyHitPos);
            serializer.SerializeValue(ref BlockMove);
            serializer.SerializeValue(ref LastHit);
        }
    }
}
