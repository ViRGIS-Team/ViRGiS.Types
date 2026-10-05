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
        public void LogMessageRpc(string message)
        {
            Debug.Log(message);
        }
        
        [Rpc(SendTo.Everyone)]
        public void LogErrorRpc(string message)
        {
            Debug.LogError(message);
        }
    }
}
