using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Networking.Transport;
using UnityEngine;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RPS
{
    public class RPSManager : NetworkBehaviour
    {
        public enum State
        {
            CONNECT,
            PREPARE,
            WAITING,
            ROUND,
            RESULT
        }

        public enum Move
        {
            ROCK,
            PAPER,
            SCISSORS
        }

        public enum Result
        {
            HOST,
            CLIENT,
            DRAW
        }

        //private State state = State.CONNECT;
        private bool clientConnected = false;
		private string hostAddress = GetLocalIPAddress();
	
        public NetworkVariable<State> HostReady = new NetworkVariable<State>(State.CONNECT);
        public NetworkVariable<State> ClientReady = new NetworkVariable<State>(State.CONNECT);

        public NetworkVariable<Move> HostMove = new NetworkVariable<Move>();
        public NetworkVariable<Move> ClientMove = new NetworkVariable<Move>();

        void Update()
        {
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            if (!NetworkManager.Singleton.IsClient)
            {
                StartButtons();
            }
            else if (NetworkManager.Singleton.IsHost)
            {
                HostUI();
            }
            else
            {
                ClientUI();
            }

            GUILayout.EndArea();
        }

        void ClientUI()
        {
            switch (ClientReady.Value)
            {
                case State.CONNECT:
                    if (clientConnected)
                    {
                        SubmitClientReadyServerRpc(State.PREPARE);
                    }

                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                case State.PREPARE:
                    UNetTransport transport = NetworkManager.Singleton.gameObject.GetComponent<UNetTransport>();
                    GUILayout.Label("Connected to IP: " + transport.ConnectAddress);
					if (GUILayout.Button("Ready"))
					{
						//state = State.WAITING;
                        SubmitClientReadyServerRpc(State.WAITING);
					}
                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                case State.WAITING:
					if (HostReady.Value == State.WAITING || HostReady.Value == State.ROUND)
					{
						//state = State.ROUND;						
                        SubmitClientReadyServerRpc(State.ROUND);
                    }
					else
					{
	                    GUILayout.Label("Waiting for the opponent...");
					}
                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                case State.ROUND:
					if (GUILayout.Button("Rock"))
					{
				        SubmitClientMoveServerRpc(Move.ROCK);
						//state = State.RESULT;
                        SubmitClientReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Paper"))
					{
				        SubmitClientMoveServerRpc(Move.PAPER);
						//state = State.RESULT;
                        SubmitClientReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Scissors"))
					{
				        SubmitClientMoveServerRpc(Move.SCISSORS);
						//state = State.RESULT;
                        SubmitClientReadyServerRpc(State.RESULT);
					}
                    break;
                case State.RESULT:
                    if (HostReady.Value == State.RESULT)
					{
                        Result result = GetResult();
	                    GUILayout.Label("Winner: " + result);
					}
					else
					{
	                    GUILayout.Label("Waiting for the opponent...");
					}
                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                default:
                    break;
            }
        }

        void HostUI()
        {
            switch (HostReady.Value)
            {
                case State.CONNECT:
                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                case State.PREPARE:
                    //UnityTransport transport = (UnityTransport)NetworkManager.NetworkConfig.NetworkTransport;
                    //UNetTransport transport = NetworkManager.Singleton.gameObject.GetComponent<UNetTransport>();
                    //GUILayout.Label("Your IP: " + transport.ConnectAddress);
                    GUILayout.Label("Your IP: " + GetLocalIPAddress());
					if (GUILayout.Button("Ready"))
					{
				        SubmitHostReadyServerRpc(State.WAITING);
						//state = State.WAITING;
					}
                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                case State.WAITING:
                    if (ClientReady.Value == State.WAITING || ClientReady.Value == State.ROUND)
					{
				        SubmitHostReadyServerRpc(State.ROUND);
						//state = State.ROUND;						
					}
					else
					{
	                    GUILayout.Label("Waiting for the opponent...");
					}
                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                case State.ROUND:
					if (GUILayout.Button("Rock"))
					{
				        SubmitHostMoveServerRpc(Move.ROCK);
						//state = State.RESULT;
				        SubmitHostReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Paper"))
					{
				        SubmitHostMoveServerRpc(Move.PAPER);
						//state = State.RESULT;
				        SubmitHostReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Scissors"))
					{
				        SubmitHostMoveServerRpc(Move.SCISSORS);
						//state = State.RESULT;
				        SubmitHostReadyServerRpc(State.RESULT);
					}
                    break;
                case State.RESULT:
                    if (ClientReady.Value == State.RESULT)
					{
                        Result result = GetResult();
	                    GUILayout.Label("Winner: " + result);
					}
					else
					{
	                    GUILayout.Label("Waiting for the opponent...");
					}
                    GUILayout.Label("Host ready: " + HostReady.Value);
                    GUILayout.Label("Client ready: " + ClientReady.Value);
                    break;
                default:
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void SubmitClientReadyServerRpc(State state, ServerRpcParams rpcParams = default)
        {
            ClientReady.Value = state;
        }

        [ServerRpc(RequireOwnership = false)]
        void SubmitHostReadyServerRpc(State state, ServerRpcParams rpcParams = default)
        {
            HostReady.Value = state;
        }

        [ServerRpc(RequireOwnership = false)]
        void SubmitClientMoveServerRpc(Move move, ServerRpcParams rpcParams = default)
        {
            ClientMove.Value = move;
        }

        [ServerRpc(RequireOwnership = false)]
        void SubmitHostMoveServerRpc(Move move, ServerRpcParams rpcParams = default)
        {
            HostMove.Value = move;
        }

		// from satckoverflow
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new System.Exception("No network adapters with an IPv4 address in the system!");
        }

        private Result GetResult()
        {
            if (HostMove.Value == ClientMove.Value)
            {
                return Result.DRAW;
            }

            if (HostMove.Value == Move.ROCK)
            {
                if (ClientMove.Value == Move.PAPER)
                {
                    return Result.CLIENT;
                }
                return Result.HOST;
            }

            if (HostMove.Value == Move.PAPER)
            {
                if (ClientMove.Value == Move.SCISSORS)
                {
                    return Result.CLIENT;
                }
                return Result.HOST;
            }

            // HostMove.Value == Move.SCISSORS)
            if (ClientMove.Value == Move.ROCK)
            {
                return Result.CLIENT;
            }
            return Result.HOST;
        } 

        void StartButtons()
        {        
            if (GUILayout.Button("Host"))
            {
                NetworkManager.Singleton.StartHost();
                //state = State.PREPARE;
                SubmitHostReadyServerRpc(State.PREPARE);
            }
            GUILayout.Label("Your IP: " + GetLocalIPAddress());
            if (GUILayout.Button("Client"))
            {
				UNetTransport transport = NetworkManager.Singleton.gameObject.GetComponent<UNetTransport>();
				transport.ConnectAddress = hostAddress;
            	
                NetworkManager.Singleton.StartClient();
                //state = State.PREPARE;
                SubmitClientReadyServerRpc(State.PREPARE);
                clientConnected = true;
            }
            hostAddress = GUILayout.TextField(hostAddress);
        }

        static void SubmitNewPosition()
        {
            if (GUILayout.Button(NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change"))
            {
                if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient )
                {
                    //foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                    //    NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<RPSPlayer>().Move();
                }
                else
                {
                    var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                    var player = playerObject.GetComponent<RPSPlayer>();
                    //player.Move();
                }
            }
        }
    }
}
