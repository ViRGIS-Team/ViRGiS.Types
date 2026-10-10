using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Virgis
{
    public class VirgisNetworkState : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            State.Instance.networkState = this;
        }

        [Rpc(SendTo.Server)]
        public void SaveRpc(ulong clientId)
        {
            _ = State.Instance.mapInitialize.SaveProjectAsync(clientId);
        }

        [Rpc(SendTo.Everyone)]
        public void NetworkLogMessageRpc(string message)
        {
            Debug.Log(message);
        }
        
        [Rpc(SendTo.Everyone)]
        public void NetworkLogErrorRpc(string message)
        {
            Debug.LogError(message);
        }
        
        [Rpc(SendTo.Server)]
        public void ServerLogMessageRpc(string message, ulong clientId)
        {
            Debug.Log($"Client {clientId} : {message}");
        }

        [Rpc(SendTo.Server)]
        public void ServerLogErrorRpc(string message, ulong clientId)
        {
            Debug.LogError($"Client {clientId} : {message}");
        }
    }
}
