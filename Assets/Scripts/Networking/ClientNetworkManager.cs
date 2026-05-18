using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using LiteNetLib;
using LiteNetLib.Utils;
using OpenGSCore; // OpenGSCoreのMatchRoomMessageなどを使用
using System.Linq;

namespace OpenGS
{
    public class ClientNetworkManager : MonoBehaviour
    {
        public static ClientNetworkManager Instance { get; private set; }
        private SynchronizationContext _mainThread;

        [Header("Server Settings")]
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int tcpPort = 60000; // Lobby TCP
        [SerializeField] private int matchTcpPort = 60001; // Match TCP (MatchServerV2)
        [SerializeField] private int udpPort = 63000; // Match UDP (MatchServerV2)
        
        [Header("Client State")]
        public string ClientPlayerId { get; private set; } = Guid.NewGuid().ToString("N");
        public string CurrentMatchRoomId { get; private set; } = string.Empty;
        public bool IsLobbyConnected => _tcpClient != null && _tcpClient.Connected;
        public bool IsMatchServerConnected => _serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected;

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
        private NetworkRequestClient _requestClient;
        private Coroutine _matchConnectRoutine;
        private bool _matchUdpConnectAttempted;

        public event Action MatchServerConnected;
        public event Action MatchServerDisconnected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _mainThread = SynchronizationContext.Current;
            _listener = new EventBasedNetListener();
            _netClient = new NetManager(_listener);
            
            _listener.NetworkReceiveEvent += OnNetworkReceive;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _listener.NetworkErrorEvent += OnNetworkError;

            _tcpReceiveBuffer = new byte[TcpBufferSize];
            _requestClient = new NetworkRequestClient(TrySendTcpMessage);
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
            RefreshConfiguredEndpoints();

            if (!IsLobbyConnected)
            {
                ConnectToLobbyTcpServer();
            }

            EnsureMatchUdpConnection();
        }

        private void Update()
        {
            _netClient?.PollEvents(); // LiteNetLibのイベントをポーリング
            // TCPデータ受信は非同期で処理するため、ここではポーリング不要
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            DisconnectAll();
        }

        public static ClientNetworkManager EnsureExists()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindFirstObjectByType<ClientNetworkManager>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(nameof(ClientNetworkManager));
            return go.AddComponent<ClientNetworkManager>();
        }

        #region TCP Lobby Connection

        private async void ConnectToLobbyTcpServer()
        {
            try
            {
                if (IsLobbyConnected)
                {
                    return;
                }

                RefreshConfiguredEndpoints();
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
            if (_requestClient != null && _requestClient.HandleIncomingMessage(message))
            {
                return;
            }

            string messageType = MessageType.Normalize(message.GetStringOrNull("MessageType"));
            switch (messageType)
            {
                case MessageType.LoginResponse:
                    bool success = message["Success"]?.ToObject<bool>() ?? true;
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
                case MessageType.MatchServerInfoResponse:
                    HandleMatchServerInfoResponse(message);
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
            _ = TrySendTcpMessage(message);
        }

        private bool TrySendTcpMessage(JObject message)
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
                return true;
            }

            Debug.LogWarning("[ClientNetwork] Not connected to TCP server. Message not sent.");
            return false;
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

        private void HandleMatchServerInfoResponse(JObject response)
        {
            var ip = response.GetStringOrNull("IP") ?? response.GetStringOrNull("IPAddress");
            var port = response["Port"]?.ToObject<int?>();
            var udp = response["UdpPort"]?.ToObject<int?>();
            var roomId = response.GetStringOrNull("RoomID");

            if (!string.IsNullOrWhiteSpace(ip))
            {
                OnlineManager.Instance.MatchServerInfo.IP = ip;
            }

            if (port.HasValue)
            {
                OnlineManager.Instance.MatchServerInfo.Port = port.Value;
            }

            if (udp.HasValue)
            {
                OnlineManager.Instance.MatchServerInfo.UdpPort = udp.Value;
            }

            if (!string.IsNullOrWhiteSpace(roomId))
            {
                CurrentMatchRoomId = roomId;
            }

            Debug.Log($"[ClientNetwork] MatchServerInfo received: {OnlineManager.Instance.MatchServerInfo.IP}:{OnlineManager.Instance.MatchServerInfo.UdpPort ?? OnlineManager.Instance.MatchServerInfo.Port}");
            EnsureMatchUdpConnection();
        }

        public void SendFriendRequest(string targetPlayerId)
        {
            _ = SendFriendRequestWithFoundationFallbackAsync(targetPlayerId);
        }

        private async Task SendFriendRequestWithFoundationFallbackAsync(string targetPlayerId)
        {
            try
            {
                if (_requestClient == null)
                {
                    throw new InvalidOperationException("NetworkRequestClient is not initialized.");
                }

                var envelopeRequest = new FriendRequestEnvelopeRequest
                {
                    PlayerId = ClientPlayerId,
                    TargetPlayerId = targetPlayerId
                };

                var envelopeResponse = await _requestClient.SendRequestAsync<FriendRequestEnvelopeRequest, FriendRequestEnvelopeResponse>(
                    NetworkFoundationRoutes.FriendRequest,
                    envelopeRequest,
                    NetworkRequestOptions.Default);

                var legacyResponse = new JObject
                {
                    ["MessageType"] = MessageType.FriendRequestResponse,
                    ["PlayerID"] = envelopeResponse?.PlayerId ?? ClientPlayerId,
                    ["TargetPlayerID"] = envelopeResponse?.TargetPlayerId ?? targetPlayerId,
                    ["Success"] = envelopeResponse?.Success ?? false,
                    ["Error"] = envelopeResponse?.Error ?? string.Empty
                };

                FriendRequestResponseReceived?.Invoke(legacyResponse);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ClientNetwork] Foundation friend request failed. Falling back to legacy request. {ex.Message}");
                SendLegacyFriendRequest(targetPlayerId);
            }
        }

        private void SendLegacyFriendRequest(string targetPlayerId)
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
        
        public Task<PingResponse> PingServerAsync(
            int timeoutMs = 3000,
            int retryCount = 0,
            CancellationToken cancellationToken = default)
        {
            if (_requestClient == null)
            {
                throw new InvalidOperationException("NetworkRequestClient is not initialized.");
            }

            var request = new PingRequest
            {
                Nonce = Guid.NewGuid().ToString("N"),
                ClientSentAtUtc = DateTime.UtcNow.ToString("O")
            };

            var options = new NetworkRequestOptions
            {
                TimeoutMs = timeoutMs,
                RetryCount = retryCount
            };

            return _requestClient.SendRequestAsync<PingRequest, PingResponse>(
                NetworkFoundationRoutes.Ping,
                request,
                options,
                cancellationToken);
        }

        #endregion

        #region UDP Match Connection

        private IEnumerator ConnectToMatchUdpWhenReady()
        {
            const float timeoutSeconds = 10f;
            var startTime = Time.realtimeSinceStartup;

            while (!_matchUdpConnectAttempted && !OnlineManager.Instance.MatchServerInfo.HasEndpoint())
            {
                if (Time.realtimeSinceStartup - startTime >= timeoutSeconds)
                {
                    Debug.LogWarning("[ClientNetwork] Match server info was not provided in time. Falling back to configured UDP endpoint.");
                    break;
                }

                yield return null;
            }

            if (!_matchUdpConnectAttempted)
            {
                ConnectToMatchUdpServer();
            }

            _matchConnectRoutine = null;
        }

        private void ConnectToMatchUdpServer()
        {
            if (_matchUdpConnectAttempted)
            {
                return;
            }

            RefreshConfiguredEndpoints();
            var matchInfo = OnlineManager.Instance.MatchServerInfo;
            var resolvedIp = !string.IsNullOrWhiteSpace(matchInfo.IP) ? matchInfo.IP : serverIp;
            var resolvedUdpPort = matchInfo.UdpPort ?? matchInfo.Port ?? udpPort;

            serverIp = resolvedIp;
            udpPort = resolvedUdpPort;
            _matchUdpConnectAttempted = true;
            _netClient.Start();
            Debug.Log($"[ClientNetwork] Connecting to Match UDP {resolvedIp}:{resolvedUdpPort} with PlayerID: {ClientPlayerId}...");
            _netClient.Connect(resolvedIp, resolvedUdpPort, "OpenGS"); // "OpenGS"は接続キー
        }

        private void OnPeerConnected(NetPeer peer)
        {
            _serverPeer = peer;
            Debug.Log("[ClientNetwork] Connected to Match UDP server.");
            RunOnMainThread(() => MatchServerConnected?.Invoke());

            // サーバーにクライアントのPlayerIDを通知 (サーバー側のOnPeerConnectedでID取得できない場合のため)
            SendUdpInput(new JObject
            {
                ["MessageType"] = "ClientConnect",
                ["PlayerID"] = ClientPlayerId,
                ["RoomID"] = CurrentMatchRoomId
            }, DeliveryMethod.ReliableOrdered);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"[ClientNetwork] Disconnected from Match UDP server: {disconnectInfo.Reason}");
            _serverPeer = null;
            RunOnMainThread(() => MatchServerDisconnected?.Invoke());
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
                RunOnMainThread(() => ProcessUdpMessage(message));
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
                        var roomId = message.GetStringOrNull("RoomID");
                        EnsureOnlineMatchRoomExists(roomId);
                        if (_matchRoomManager != null && _matchRoomManager.OnlineMatchRoom != null)
                        {
                            _matchRoomManager.OnlineMatchRoom.PushInput(message);
                        }
                        else
                        {
                            Debug.LogWarning("[ClientNetwork] Received Snapshot but MatchRoom could not be initialized.");
                        }
                    }
                    break;
                case "MatchJoined":
                    HandleMatchJoined(message);
                    break;
                case MessageType.GameStartNotification:
                    EnsureOnlineMatchRoomExists(message.GetStringOrNull("RoomID"));
                    _matchRoomManager?.OnlineMatchRoom?.StartMatch();
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
            _matchUdpConnectAttempted = false;
            _netClient?.Stop();
            _tcpClient?.Close();
            _tcpClient?.Dispose();
            Debug.Log("[ClientNetwork] Disconnected from all servers.");
        }

        private void HandleMatchJoined(JObject message)
        {
            CurrentMatchRoomId = message.GetStringOrNull("RoomID");
            EnsureOnlineMatchRoomExists(CurrentMatchRoomId);

            var roomName = message.GetStringOrNull("RoomName");
            if (_matchRoomManager?.OnlineMatchRoom != null)
            {
                if (!string.IsNullOrWhiteSpace(roomName))
                {
                    _matchRoomManager.OnlineMatchRoom.RoomName = roomName;
                }

                var capacity = message["Capacity"]?.ToObject<int?>();
                if (capacity.HasValue && capacity.Value > 0)
                {
                    _matchRoomManager.OnlineMatchRoom.Capacity = capacity.Value;
                }

                _matchRoomManager.OnlineMatchRoom.ReplacePlayers(ResolveMatchPlayers());
            }

            Debug.Log($"[ClientNetwork] Joined Match Room: {CurrentMatchRoomId}");
        }

        private void EnsureOnlineMatchRoomExists(string roomId)
        {
            if (_matchRoomManager == null)
            {
                try
                {
                    _matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
                }
                catch
                {
                    _matchRoomManager = null;
                }
            }

            if (_matchRoomManager == null || _matchRoomManager.OnlineMatchRoom != null)
            {
                return;
            }

            _matchRoomManager.CreateNewOnlineMatchRoom(roomId);
            _matchRoomManager.OnlineMatchRoom?.ReplacePlayers(ResolveMatchPlayers());
        }

        private List<PlayerInfo> ResolveMatchPlayers()
        {
            var players = new List<PlayerInfo>();
            try
            {
                var waitRoomManager = DependencyInjectionConfig.Resolve<WaitRoomManager>();
                var waitRoomPlayers = waitRoomManager?.WaitRoom?.PlayerList;
                if (waitRoomPlayers != null && waitRoomPlayers.Count > 0)
                {
                    players.AddRange(waitRoomPlayers
                        .Where(player => player != null && !string.IsNullOrWhiteSpace(player.Id))
                        .Select(ClonePlayerInfo));
                }
            }
            catch
            {
            }

            if (players.Count == 0)
            {
                players.Add(BuildLocalPlayerInfo());
            }

            return players;
        }

        private PlayerInfo BuildLocalPlayerInfo()
        {
            var source = AccountManager.Instance.PlayerInfo;
            var cloned = ClonePlayerInfo(source);
            cloned.Id = string.IsNullOrWhiteSpace(cloned.Id) ? ClientPlayerId : cloned.Id;
            cloned.Name = string.IsNullOrWhiteSpace(cloned.Name) ? "Player" : cloned.Name;
            return cloned;
        }

        private static PlayerInfo ClonePlayerInfo(PlayerInfo source)
        {
            if (source == null)
            {
                return new PlayerInfo();
            }

            return new PlayerInfo(source.Id, source.Name, source.CurrentIp, source.Level, source.Exp, source.Health, source.AttackPower, source.DefensePower)
            {
                MaxHealth = source.MaxHealth,
                Ping = source.Ping,
                playerCharacter = source.playerCharacter,
                Team = source.Team,
                IsReady = source.IsReady,
                Kills = source.Kills,
                Deaths = source.Deaths,
                IsBot = source.IsBot
            };
        }

        public void EnsureMatchUdpConnection()
        {
            if (IsMatchServerConnected || _matchUdpConnectAttempted)
            {
                return;
            }

            if (_matchConnectRoutine == null)
            {
                _matchConnectRoutine = StartCoroutine(ConnectToMatchUdpWhenReady());
            }
        }

        private void RefreshConfiguredEndpoints()
        {
            var lobbyInfo = OnlineManager.Instance.LobbyServerInfo;
            if (!string.IsNullOrWhiteSpace(lobbyInfo.IPAddress))
            {
                serverIp = lobbyInfo.IPAddress;
            }

            if (lobbyInfo.Port.HasValue)
            {
                tcpPort = lobbyInfo.Port.Value;
            }

            var matchInfo = OnlineManager.Instance.MatchServerInfo;
            if (!string.IsNullOrWhiteSpace(matchInfo.IP))
            {
                serverIp = matchInfo.IP;
            }

            if (matchInfo.Port.HasValue)
            {
                matchTcpPort = matchInfo.Port.Value;
            }

            if (matchInfo.UdpPort.HasValue)
            {
                udpPort = matchInfo.UdpPort.Value;
            }
        }

        private void RunOnMainThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            var context = _mainThread ?? SynchronizationContext.Current;
            if (context == null || context == SynchronizationContext.Current)
            {
                action();
                return;
            }

            context.Post(_ => action(), null);
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
