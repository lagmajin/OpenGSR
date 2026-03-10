using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Newtonsoft.Json.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using OpenGSCore; // OpenGSCoreのMatchRoomMessageなどを使用

namespace OpenGS
{
    public class ClientNetworkManager : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int tcpPort = 60000; // Lobby TCP
        [SerializeField] private int matchTcpPort = 60001; // Match TCP (MatchServerV2)
        [SerializeField] private int udpPort = 63000; // Match UDP (MatchServerV2)
        
        [Header("Client State")]
        public string ClientPlayerId { get; private set; } = Guid.NewGuid().ToString("N");
        public string CurrentMatchRoomId { get; private set; } = string.Empty;

        // LiteNetLib UDP Client
        private NetManager _netClient;
        private EventBasedNetListener _listener;
        private NetPeer _serverPeer;

        // TCP Client (Lobby/Match 初期接続用)
        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private byte[] _tcpReceiveBuffer;
        private const int TcpBufferSize = 8192; // 8KB

        // MatchRoomManagerへの参照
        private MatchRoomManager _matchRoomManager;

        private void Awake()
        {
            _listener = new EventBasedNetListener();
            _netClient = new NetManager(_listener);
            
            _listener.NetworkReceiveEvent += OnNetworkReceive;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _listener.NetworkErrorEvent += OnNetworkError;

            _tcpReceiveBuffer = new byte[TcpBufferSize];
            try
            {
                _matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }
            catch
            {
                _matchRoomManager = null;
            }
            if (_matchRoomManager == null)
            {
                Debug.LogWarning("[ClientNetwork] MatchRoomManager is not available.");
            }
        }

        private void Start()
        {
            ConnectToLobbyTcpServer();
            ConnectToMatchUdpServer();
        }

        private void Update()
        {
            _netClient?.PollEvents(); // LiteNetLibのイベントをポーリング
            // TCPデータ受信は非同期で処理するため、ここではポーリング不要
        }

        private void OnDestroy()
        {
            DisconnectAll();
        }

        #region TCP Lobby Connection

        private async void ConnectToLobbyTcpServer()
        {
            try
            {
                _tcpClient = new TcpClient();
                Debug.Log($"[ClientNetwork] Connecting to Lobby TCP {serverIp}:{tcpPort}...");
                await _tcpClient.ConnectAsync(serverIp, tcpPort);
                _tcpStream = _tcpClient.GetStream();
                Debug.Log("[ClientNetwork] Connected to Lobby TCP server.");

                // サーバーからの非同期受信を開始
                _ = ReceiveTcpDataAsync();

                // ログイン要求などを送信する（簡略化のためここでは省略）
                SendTcpMessage(new JObject
                {
                    ["MessageType"] = MessageType.LoginRequest, // MessageTypeを使用
                    ["PlayerID"] = ClientPlayerId,
                    ["PlayerName"] = "UnityClient_" + ClientPlayerId.Substring(0, 4)
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientNetwork] Failed to connect to Lobby TCP server: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ReceiveTcpDataAsync()
        {
            try
            {
                while (_tcpClient != null && _tcpClient.Connected)
                {
                    int bytesRead = await _tcpStream.ReadAsync(_tcpReceiveBuffer, 0, _tcpReceiveBuffer.Length);
                    if (bytesRead == 0)
                    {
                        Debug.Log("[ClientNetwork] Lobby TCP server disconnected.");
                        break;
                    }

                    string jsonString = System.Text.Encoding.UTF8.GetString(_tcpReceiveBuffer, 0, bytesRead);
                    JObject message = JObject.Parse(jsonString);
                    ProcessTcpMessage(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientNetwork] Error receiving TCP data: {ex.Message}");
            }
        }
        
        private void ProcessTcpMessage(JObject message)
        {
            string messageType = message.GetStringOrNull("MessageType");
            switch (messageType)
            {
                case MessageType.LoginResponse:
                    bool success = message.Value<bool>("Success");
                    if (success)
                    {
                        Debug.Log($"[ClientNetwork] Login successful. PlayerID: {message.GetStringOrNull("PlayerID")}");
                        // ログイン成功後の処理
                    }
                    else
                    {
                        Debug.LogError($"[ClientNetwork] Login failed: {message.GetStringOrNull("Error")}");
                    }
                    break;
                case MessageType.PlayerInfoResponse:
                    HandlePlayerInfoResponse(message);
                    break;
                // 他のTCPメッセージタイプをここで処理
                default:
                    Debug.Log($"[ClientNetwork] Received unknown TCP message: {message}");
                    break;
            }
        }

        public void SendTcpMessage(JObject message)
        {
            if (_tcpStream != null && _tcpStream.CanWrite)
            {
                string jsonString = message.ToString(Formatting.None);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonString);
                _tcpStream.Write(data, 0, data.Length);
                //Debug.Log($"[ClientNetwork] Sent TCP data: {jsonString}");
            }
            else
            {
                Debug.LogWarning("[ClientNetwork] Not connected to TCP server. Message not sent.");
            }
        }

        /// <summary>
        /// プレイヤー情報のリクエストを送信
        /// </summary>
        public void RequestPlayerInfo(string targetPlayerId)
        {
            JObject request = new JObject
            {
                ["MessageType"] = MessageType.PlayerInfoRequest,
                ["TargetPlayerID"] = targetPlayerId
            };
            SendTcpMessage(request);
            Debug.Log($"[ClientNetwork] Sent PlayerInfoRequest for {targetPlayerId}");
        }

        private void HandlePlayerInfoResponse(JObject response)
        {
            bool success = response.Value<bool>("Success");
            string targetPlayerId = response.GetStringOrNull("PlayerID") ?? response.GetStringOrNull("TargetPlayerID");

            if (success)
            {
                Debug.Log($"[ClientNetwork] PlayerInfoResponse for {targetPlayerId}: DisplayName={response.GetStringOrNull("DisplayName")}, Level={response.Value<int>("Level")}, XP={response.Value<int>("XP")}");
                // ここで受信したプレイヤー情報をUIに表示したり、データモデルに保存したりします
                // 例: OnPlayerInfoReceived?.Invoke(response);
            }
            else
            {
                Debug.LogError($"[ClientNetwork] Failed to get player info for {targetPlayerId}: {response.GetStringOrNull("Error")}");
            }
        }

        #endregion

        #region UDP Match Connection

        private void ConnectToMatchUdpServer()
        {
            _netClient.Start();
            Debug.Log($"[ClientNetwork] Connecting to Match UDP {serverIp}:{udpPort} with PlayerID: {ClientPlayerId}...");
            _netClient.Connect(serverIp, udpPort, "OpenGS"); // "OpenGS"は接続キー
        }

        private void OnPeerConnected(NetPeer peer)
        {
            _serverPeer = peer;
            Debug.Log($"[ClientNetwork] Connected to Match UDP server: {peer.EndPoint}");

            // サーバーにクライアントのPlayerIDを通知 (サーバー側のOnPeerConnectedでID取得できない場合のため)
            SendUdpInput(new JObject
            {
                ["MessageType"] = "ClientConnect",
                ["PlayerID"] = ClientPlayerId
            }, DeliveryMethod.ReliableOrdered);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"[ClientNetwork] Disconnected from Match UDP server: {disconnectInfo.Reason}");
            _serverPeer = null;
        }

        private void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.LogError($"[ClientNetwork] Network Error: {socketError} from {endPoint}");
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                string jsonString = reader.GetString();
                JObject message = JObject.Parse(jsonString);
                ProcessUdpMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientNetwork] Error parsing UDP message: {ex.Message}");
            }
            finally
            {
                reader.Recycle();
            }
        }

        private void ProcessUdpMessage(JObject message)
        {
            string messageType = message.GetStringOrNull("MessageType");
            switch (messageType)
            {
                case "Snapshot":
                    if (_matchRoomManager != null && _matchRoomManager.OnlineMatchRoom != null)
                    {
                        _matchRoomManager.OnlineMatchRoom.PushInput(message); // スナップショットをクライアントのMatchRoomバッファへ
                    }
                    else
                    {
                        Debug.LogWarning("[ClientNetwork] Received Snapshot but MatchRoom is not ready.");
                    }
                    break;
                case "MatchJoined":
                    CurrentMatchRoomId = message.GetStringOrNull("RoomID");
                    Debug.Log($"[ClientNetwork] Joined Match Room: {CurrentMatchRoomId}");
                    break;
                // 他のUDPメッセージタイプをここで処理
                default:
                    Debug.Log($"[ClientNetwork] Received unknown UDP message: {message}");
                    break;
            }
        }

        public void SendUdpInput(JObject input, DeliveryMethod method = DeliveryMethod.Unreliable)
        {
            if (_serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected)
            {
                if (input["PlayerID"] == null) input["PlayerID"] = ClientPlayerId;
                if (input["RoomID"] == null && !string.IsNullOrEmpty(CurrentMatchRoomId)) input["RoomID"] = CurrentMatchRoomId;

                string jsonString = input.ToString(Formatting.None);
                _serverPeer.Send(jsonString, method);
                //Debug.Log($"[ClientNetwork] Sent UDP Input: {jsonString}");
            }
            else
            {
                Debug.LogWarning("[ClientNetwork] Not connected to UDP server. Input not sent.");
            }
        }

        #endregion

        public void DisconnectAll()
        {
            _netClient?.Stop();
            _tcpClient?.Close();
            _tcpClient?.Dispose();
            Debug.Log("[ClientNetwork] Disconnected from all servers.");
        }
    }

    public static class JObjectExtensions
    {
        public static string GetStringOrNull(this JObject obj, string key)
        {
            return obj.TryGetValue(key, out JToken token) ? token.ToString() : null;
        }
    }
}
