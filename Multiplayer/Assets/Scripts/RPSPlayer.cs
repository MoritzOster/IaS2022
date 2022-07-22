using Unity.Netcode;
using UnityEngine;
//using UnityEngine.Networking;

namespace RPS
{
    public class RPSPlayer : NetworkBehaviour
    {
        private Animator anim;
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Move();
            }
        }

        public void Move()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var randomPosition = new Vector3(0f, 1f, 2f);
                transform.position = randomPosition;
                transform.eulerAngles = new Vector3(0f, 180f, -90f);
                //transform.localScale = new Vector3(10f, 10f, 10f);
                Position.Value = randomPosition;
            }
            else
            {
                transform.position = new Vector3(0f, 1f, -2f);
                transform.eulerAngles = new Vector3(0f, 0f, -90f);
                SubmitPositionRequestServerRpc();
            }
        }

        [ServerRpc]
        void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
        {
            transform.position = new Vector3(0f, 1f, -2f);
            transform.eulerAngles = new Vector3(0f, 0f, -90f);
        }

        void Start()
        {
            //var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            anim = GetComponent<Animator>();
            //anim.SetTrigger("ActivateIdle");
        }

        void Update()
        {
            //if (Input.GetMouseButtonDown(0))
            //{
            //    TriggerServerRpc("scissors");
            //}
        }

        public void Trigger(string move) {
            TriggerServerRpc(move);
        }

        [ServerRpc]
        public void TriggerServerRpc(string move, ServerRpcParams rpcParams = default)
        {
            //transform.localScale = new Vector3(10f, 10f, 10f);
            switch (move)
            {
                case "rock":
                    anim.SetTrigger("ActivateRock");
                    break;
                case "paper":
                    anim.SetTrigger("ActivatePaper");
                    break;
                case "scissors":
                    anim.SetTrigger("ActivateScissors");
                    break;
                default:
                    break;
            }
        }

    }
}
