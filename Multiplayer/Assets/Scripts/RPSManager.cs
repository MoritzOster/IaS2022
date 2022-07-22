using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Networking;

using System.Collections;
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

        private const string ROCK = "gesture=rock"; 
        private const string PAPER = "gesture=paper"; 
        private const string SCISSORS = "gesture=scissors"; 

        //private State state = State.CONNECT;
        private bool clientConnected = false;
		private string hostAddress = GetLocalIPAddress();
	
        public NetworkVariable<State> HostReady = new NetworkVariable<State>(State.CONNECT);
        public NetworkVariable<State> ClientReady = new NetworkVariable<State>(State.CONNECT);

        public NetworkVariable<Move> HostMove = new NetworkVariable<Move>();
        public NetworkVariable<Move> ClientMove = new NetworkVariable<Move>();

        private string text = "empty";
        private int updateInterval = 1;
        private double updateNextTime = 0.0;

        void Update()
        {
            if (Time.time >= updateNextTime) {
                if (NetworkManager.Singleton.IsHost)
                {
                    StartCoroutine(GetPlayerMove());
                }
                updateNextTime += updateInterval;
            }
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
                //GUILayout.Label(text);
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
					if (HostReady.Value == State.WAITING || HostReady.Value == State.ROUND || HostReady.Value == State.RESULT)
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
                    //var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                    //var player = playerObject.GetComponent<RPSPlayer>();
                    //player.Move();

					if (GUILayout.Button("Rock"))
					{
				        SubmitClientMoveServerRpc(Move.ROCK);
                        //player.TriggerServerRpc("rock");
						//state = State.RESULT;
                        SubmitClientReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Paper"))
					{
				        SubmitClientMoveServerRpc(Move.PAPER);
                        //player.TriggerServerRpc("paper");
						//state = State.RESULT;
                        SubmitClientReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Scissors"))
					{
				        SubmitClientMoveServerRpc(Move.SCISSORS);
                        //player.TriggerServerRpc("scissors");
						//state = State.RESULT;
                        SubmitClientReadyServerRpc(State.RESULT);
					}
                    break;
                case State.RESULT:
                    if (HostReady.Value == State.RESULT)
					{
                        var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                        var player = playerObject.GetComponent<RPSPlayer>();

                        switch (ClientMove.Value)
                        {
                            case Move.ROCK:
                                player.TriggerServerRpc("rock");
                                break;
                            case Move.PAPER:
                                player.TriggerServerRpc("paper");
                                break;
                            case Move.SCISSORS:
                                player.TriggerServerRpc("scissors");
                                break;
                            default:
                                break;
                        }

                        Result result = GetResult();
                        GUIStyle guiStyle = new GUIStyle();
                        guiStyle.fontSize = 20;
                        switch (result)
                        {
                            case Result.DRAW:
        	                    GUILayout.Label("It is a draw!", guiStyle);
                                break;
                            case Result.CLIENT:
        	                    GUILayout.Label("You win! Congratulations!", guiStyle);
                                break;
                            case Result.HOST:
        	                    GUILayout.Label("Opponent wins!", guiStyle);
                                break;
                            default:
                                break;
                        }
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
                    
                    //var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                    //var player = playerObject.GetComponent<RPSPlayer>();
                    //player.Move();
                    string cleaned = text.Replace("\n", "").Replace("\r", "");
					//if (GUILayout.Button("make move"))
					{
                        switch (cleaned)
                        {
                            case ROCK:
                                SubmitHostMoveServerRpc(Move.ROCK);
                                //player.TriggerServerRpc("rock");
                                SubmitHostReadyServerRpc(State.RESULT);
                                break;
                            case PAPER:
                                SubmitHostMoveServerRpc(Move.PAPER);
                                //player.TriggerServerRpc("paper");
                                SubmitHostReadyServerRpc(State.RESULT);
                                break;
                            case SCISSORS:
                                SubmitHostMoveServerRpc(Move.SCISSORS);
                                //player.TriggerServerRpc("scissors");
                                SubmitHostReadyServerRpc(State.RESULT);
                                break;
                            default:
                                break;
                        }
                    }
                    /*
					if (GUILayout.Button("Rock"))
					{
				        SubmitHostMoveServerRpc(Move.ROCK);
                        player.TriggerServerRpc("rock");
						//state = State.RESULT;
				        SubmitHostReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Paper"))
					{
				        SubmitHostMoveServerRpc(Move.PAPER);
                        player.TriggerServerRpc("paper");
						//state = State.RESULT;
				        SubmitHostReadyServerRpc(State.RESULT);
					}
					if (GUILayout.Button("Scissors"))
					{
				        SubmitHostMoveServerRpc(Move.SCISSORS);
                        player.TriggerServerRpc("scissors");
						//state = State.RESULT;
				        SubmitHostReadyServerRpc(State.RESULT);
					}
                    */
                    break;
                case State.RESULT:
                    if (ClientReady.Value == State.RESULT)
					{
                        var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                        var player = playerObject.GetComponent<RPSPlayer>();

                        switch (HostMove.Value)
                        {
                            case Move.ROCK:
                                player.TriggerServerRpc("rock");
                                break;
                            case Move.PAPER:
                                player.TriggerServerRpc("paper");
                                break;
                            case Move.SCISSORS:
                                player.TriggerServerRpc("scissors");
                                break;
                            default:
                                break;
                        }

                        Result result = GetResult();
                        GUIStyle guiStyle = new GUIStyle();
                        guiStyle.fontSize = 20;
                        switch (result)
                        {
                            case Result.DRAW:
        	                    GUILayout.Label("It is a draw!", guiStyle);
                                break;
                            case Result.HOST:
        	                    GUILayout.Label("You win! Congratulations!", guiStyle);
                                break;
                            case Result.CLIENT:
        	                    GUILayout.Label("Opponent wins!", guiStyle);
                                break;
                            default:
                                break;
                        }
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

        IEnumerator GetPlayerMove()
        {
            UnityWebRequest www = UnityWebRequest.Get("192.168.217.190/gesture");
            yield return www.SendWebRequest();
    
            if (www.result != UnityWebRequest.Result.Success) {
                text = www.error;
            }
            else {
                text = www.downloadHandler.text;
                /*var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<RPSPlayer>();

                switch (text)
                {
                    case ROCK:
                        SubmitHostMoveServerRpc(Move.ROCK);
                        player.TriggerServerRpc("rock");
				        SubmitHostReadyServerRpc(State.RESULT);
                        break;
                    case PAPER:
                        SubmitHostMoveServerRpc(Move.PAPER);
                        player.TriggerServerRpc("paper");
				        SubmitHostReadyServerRpc(State.RESULT);
                        break;
                    case SCISSORS:
                        SubmitHostMoveServerRpc(Move.SCISSORS);
                        player.TriggerServerRpc("scissors");
				        SubmitHostReadyServerRpc(State.RESULT);
                        break;
                    default:
                        break;
                }*/
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

        void SubmitNewPosition()
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
