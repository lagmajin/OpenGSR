using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
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
        private readonly StringBuilder _tcpMessageBuffer = new StringBuilder();

        public event Action<JObject> FriendRequestResponseReceived;
        public event Action<JObject> FriendApproveResponseReceived;
        public event Action<JObject> FriendListResponseReceived;
        public event Action<JObject> FriendRequestNotificationReceived;

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

                    string chunk = Encoding.UTF8.GetString(_tcpReceiveBuffer, 0, bytesRead);
                    _tcpMessageBuffer.Append(chunk);

                    string fullBuffer = _tcpMessageBuffer.ToString();
                    string[] parts = fullBuffer.Split('\x1F');

                    if (parts.Length == 1)
                    {
                        if (TryParseTcpPacket(parts[0], out JObject singleMessage))
                        {
                            ProcessTcpMessage(singleMessage);
                            _tcpMessageBuffer.Clear();
                        }
                        continue;
                    }

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        if (TryParseTcpPacket(parts[i], out JObject message))
                        {
                            ProcessTcpMessage(message);
                        }
                    }

                    _tcpMessageBuffer.Clear();
                    _tcpMessageBuffer.Append(parts[^1]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientNetwork] Error receiving TCP data: {ex.Message}");
            }
        }

        private static bool TryParseTcpPacket(string rawPacket, out JObject message)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(rawPacket))
            {
                return false;
            }

            string parseTarget = rawPacket.Trim();
            int firstBrace = parseTarget.IndexOf('{');
            int lastBrace = parseTarget.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                parseTarget = parseTarget.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            else if (firstBrace >= 0)
            {
                parseTarget = parseTarget.Substring(firstBrace);
            }

            try
            {
                message = JObject.Parse(parseTarget);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ClientNetwork] Failed to parse TCP packet: {ex.Message}, packet={rawPacket}");
                return false;
            }
        }
        
        private void ProcessTcpMessage(JObject message)
        {
            string messageType = message.GetStringOrNull("MessageType");
            switch (messageType)
            {
                case MessageType.LoginResponse:
                case "LoginSuccessful":
                    bool success = messageType == "LoginSuccessful" || (message["Success"]?.ToObject<bool>() ?? false);
                    if (success)
                    {
                        string resolvedPlayerId = message.GetStringOrNull("PlayerID") ?? message.GetStringOrNull("GlobalUserId");
                        if (!string.IsNullOrEmpty(resolvedPlayerId))
                        {
                            ClientPlayerId = resolvedPlayerId;
                        }
                        Debug.Log($"[ClientNetwork] Login successful. PlayerID: {ClientPlayerId}");
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
                case MessageType.FriendRequestResponse:
                    FriendRequestResponseReceived?.Invoke(message);
                    break;
                case MessageType.FriendApproveResponse:
                    FriendApproveResponseReceived?.Invoke(message);
                    break;
                case MessageType.FriendListResponse:
                    FriendListResponseReceived?.Invoke(message);
                    break;
                case MessageType.FriendRequestNotification:
                    FriendRequestNotificationReceived?.Invoke(message);
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
                byte[] payload = Encoding.UTF8.GetBytes(jsonString);
                byte[] separator = { 0x1F };
                byte[] data = new byte[payload.Length + separator.Length];
                Buffer.BlockCopy(payload, 0, data, 0, payload.Length);
                Buffer.BlockCopy(separator, 0, data, payload.Length, separator.Length);
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

        public void SendFriendRequest(string targetPlayerId)
        {
            JObject request = new JObject
            {
                ["MessageType"] = MessageType.FriendRequest,
                ["PlayerID"] = ClientPlayerId,
                ["TargetPlayerID"] = targetPlayerId
            };

            SendTcpMessage(request);
        }

        public void ApproveFriendRequest(string requestPlayerId, bool approve = true)
        {
            JObject request = new JObject
            {
                ["MessageType"] = MessageType.FriendApproveRequest,
                ["PlayerID"] = ClientPlayerId,
                ["RequestPlayerID"] = requestPlayerId,
                ["Approve"] = approve
            };

            SendTcpMessage(request);
        }

        public void RequestFriendList()
        {
            JObject request = new JObject
            {
                ["MessageType"] = MessageType.FriendListRequest,
                ["PlayerID"] = ClientPlayerId
            };

            SendTcpMessage(request);
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
            Debug.Log("[ClientNetwork] Connected to Match UDP server.");

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
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                _serverPeer.Send(bytes, 0, method);
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
