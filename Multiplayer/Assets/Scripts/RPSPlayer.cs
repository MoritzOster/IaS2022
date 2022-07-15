using Unity.Netcode;
using UnityEngine;

namespace RPS
{
    public class RPSPlayer : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

        [ServerRpc]
        void SubmitHostReadyServerRpc(ServerRpcParams rpcParams = default)
        {
        }

        [ServerRpc]
        void SubmitClientReadyServerRpc(ServerRpcParams rpcParams = default)
        {
        }

        void Update()
        {
        }
    }
}
