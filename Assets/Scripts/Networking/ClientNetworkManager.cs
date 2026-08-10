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

namespace OpenGS
{
    public class ClientNetworkManager : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int tcpPort = 60000; // Lobby TCP
        [SerializeField] private int udpPort = 63000; // Match UDP (MatchServerV2)
        
        [Header("Client State")]
        public string ClientPlayerId { get; private set; } = Guid.NewGuid().ToString("N");
        public string CurrentMatchRoomId { get; private set; } = string.Empty;
        public JObject LastDailyListResponse { get; private set; }
        [Tooltip("Enable detailed UDP receive logs for match traffic. Non-verbose match warnings still remain visible.")]
        [SerializeField] private bool verboseUdpLogs = false;

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
        public event Action<JObject> DailyListResponseReceived;
        public event Action<JObject> DailyProgressResponseReceived;
        public event Action<JObject> DailyClaimResponseReceived;
        public event Action<JObject> GuildRoleResponseReceived;
        public event Action<JObject> GuildListResponseReceived;
        public event Action<JObject> GuildInfoResponseReceived;

        // MatchRoomManagerへの参照
        private MatchRoomManager _matchRoomManager;
        private NetworkRequestClient _requestClient;
        private Coroutine _matchConnectRoutine;
        private bool _matchUdpConnectAttempted;

        private void Awake()
        {
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
            ConnectToLobbyTcpServer();
            _matchConnectRoutine = StartCoroutine(ConnectToMatchUdpWhenReady());
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
                        RequestDailyList();
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
                case MessageType.DailyListResponse:
                    LastDailyListResponse = message;
                    DailyListResponseReceived?.Invoke(message);
                    break;
                case MessageType.DailyProgressResponse:
                    DailyProgressResponseReceived?.Invoke(message);
                    break;
                case MessageType.DailyClaimResponse:
                    DailyClaimResponseReceived?.Invoke(message);
                    break;
                case MessageType.GuildRoleResponse:
                    GuildRoleResponseReceived?.Invoke(message);
                    break;
                case MessageType.GuildListResponse:
                    GuildListResponseReceived?.Invoke(message);
                    break;
                case MessageType.GuildInfoResponse:
                    GuildInfoResponseReceived?.Invoke(message);
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

        public void RequestDailyList()
        {
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.DailyListRequest,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void ClaimDailyReward(string dailyId)
        {
            if (string.IsNullOrWhiteSpace(dailyId)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.DailyClaimRequest,
                ["PlayerID"] = ClientPlayerId,
                ["DailyId"] = dailyId
            });
        }

        public void ChangeGuildMemberRole(string guildName, string memberId, string role)
        {
            if (string.IsNullOrWhiteSpace(guildName) || string.IsNullOrWhiteSpace(memberId) || string.IsNullOrWhiteSpace(role)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildRoleRequest,
                ["GuildName"] = guildName,
                ["MemberId"] = memberId,
                ["Role"] = role,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void RequestGuildList()
        {
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildListRequest,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void RequestGuildInfo(string guildName)
        {
            if (string.IsNullOrWhiteSpace(guildName)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildInfoRequest,
                ["GuildName"] = guildName,
                ["PlayerID"] = ClientPlayerId
            });
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

            if (!_matchUdpConnectAttempted && _matchConnectRoutine == null)
            {
                _matchConnectRoutine = StartCoroutine(ConnectToMatchUdpWhenReady());
            }
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

            // サーバーにクライアントのPlayerIDを通知 (サーバー側のOnPeerConnectedでID取得できない場合のため)
            SendUdpInput(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ClientConnect,
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
                NetworkReplayRecorder.RecordIncoming(message);
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
                case RUDPMessageTypes.Snapshot:
                    if (_matchRoomManager != null && _matchRoomManager.OnlineMatchRoom != null)
                    {
                        _matchRoomManager.OnlineMatchRoom.PushInput(message); // スナップショットをクライアントのMatchRoomバッファへ
                    }
                    else
                    {
                        Debug.LogWarning("[ClientNetwork] Received Snapshot but MatchRoom is not ready.");
                    }
                    break;
                case RUDPMessageTypes.MatchJoined:
                    CurrentMatchRoomId = message.GetStringOrNull("RoomID");
                    Debug.Log($"[ClientNetwork] Joined Match Room [{FormatRoomTag(CurrentMatchRoomId)}]");
                    break;
                case RUDPMessageTypes.PlayerShot:
                    LogUdpEvent("PlayerShot", message.GetStringOrNull("RoomID"), message.GetStringOrNull("PlayerID"), message.GetStringOrNull("ObjectId"));
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
                case RUDPMessageTypes.GrenadeThrow:
                    LogUdpEvent("GrenadeThrow", message.GetStringOrNull("RoomID"), message.GetStringOrNull("PlayerID"), message.GetStringOrNull("ObjectId"));
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
                case RUDPMessageTypes.ObjectSpawned:
                    LogUdpEvent("ObjectSpawned", message.GetStringOrNull("RoomID"), message.GetStringOrNull("ObjectType"), message.GetStringOrNull("ObjectId"));
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
                case RUDPMessageTypes.ObjectDestroyed:
                    LogUdpEvent("ObjectDestroyed", message.GetStringOrNull("RoomID"), message.GetStringOrNull("ObjectType"), message.GetStringOrNull("ObjectId"));
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
                case RUDPMessageTypes.PlayerPose:
                    LogUdpEvent("PlayerPose", message.GetStringOrNull("RoomID"), message.GetStringOrNull("PlayerID"), message.GetStringOrNull("PoseState"));
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
                case RUDPMessageTypes.PlayerPositionUpdate:
                    HandlePlayerPositionUpdate(message);
                    break;
                case RUDPMessageTypes.PlayerDeath:
                case RUDPMessageTypes.PlayerKilled:
                case RUDPMessageTypes.PlayerDamage:
                case RUDPMessageTypes.PlayerDamaged:
                case RUDPMessageTypes.PlayerKill:
                case RUDPMessageTypes.KillScoreUpdate:
                case RUDPMessageTypes.FlagCaptured:
                case RUDPMessageTypes.FlagLost:
                case RUDPMessageTypes.FlagReturn:
                case RUDPMessageTypes.FlagBurst:
                case RUDPMessageTypes.FlagPickup:
                case RUDPMessageTypes.FlagScoreUpdate:
                case RUDPMessageTypes.MatchStart:
                case RUDPMessageTypes.MatchEnd:
                case RUDPMessageTypes.MatchPause:
                case RUDPMessageTypes.MatchResume:
                case RUDPMessageTypes.RoundStart:
                case RUDPMessageTypes.RoundEnd:
                case RUDPMessageTypes.PlayerRespawn:
                case RUDPMessageTypes.RespawnCountdown:
                case RUDPMessageTypes.PlayerJoined:
                case RUDPMessageTypes.PlayerLeft:
                case RUDPMessageTypes.PlayerTeamSwitch:
                case RUDPMessageTypes.WeaponChange:
                case RUDPMessageTypes.PlayerReload:
                case RUDPMessageTypes.ItemPickup:
                case RUDPMessageTypes.ItemUse:
                case RUDPMessageTypes.ItemSpawn:
                case RUDPMessageTypes.GameStateSync:
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
                case RUDPMessageTypes.PingRequest:
                {
                    var pong = new JObject();
                    pong["MessageType"] = "PingResponse";
                    pong["ClientTimestamp"] = message["ClientTimestamp"];
                    SendUdpInput(pong);
                    break;
                }
                // 他のUDPメッセージタイプをここで処理
                default:
                    Debug.Log($"[ClientNetwork] Received unknown UDP message: {message}");
                    break;
            }
        }

        public void ReplayUdpMessage(JObject message)
        {
            if (message == null)
            {
                return;
            }

            ProcessUdpMessage(message);
        }

        private void HandlePlayerPositionUpdate(JObject message)
        {
            var playerId = message.GetStringOrNull("PlayerID") ?? message.GetStringOrNull("PlayerId");
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var state = new OpenGS.Network.TransformState
            {
                playerId = playerId,
                position = new Vector3(
                    message["PosX"]?.ToObject<float>() ?? 0f,
                    message["PosY"]?.ToObject<float>() ?? 0f,
                    0f),
                rotation = Quaternion.Euler(0f, 0f, message["Rotation"]?.ToObject<float>() ?? 0f),
                velocity = Vector3.zero,
                timestamp = Time.time,
                sequenceNumber = message["SequenceNumber"]?.ToObject<byte>() ?? 0
            };

            var lagManager = FindFirstObjectByType<OpenGS.Network.LagCompensationManager>();
            if (lagManager != null)
            {
                lagManager.OnPlayerStateReceived(state);
                return;
            }

            Debug.Log($"[ClientNetwork] PlayerPositionUpdate received for {playerId}: {state.position}");
        }

        private static void PublishGameEvent(AbstractGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return;
            }

            GameEventBroker.Publish(gameEvent);
        }

        private void LogUdpEvent(string eventType, string roomId, string primary, string secondary)
        {
            if (!verboseUdpLogs)
            {
                return;
            }

            var roomTag = FormatRoomTag(roomId);
            Debug.Log($"[ClientNetwork] UDP {eventType} [{roomTag}]: {primary} / {secondary}");
        }

        private static string FormatRoomTag(string roomId)
        {
            return string.IsNullOrWhiteSpace(roomId) ? "no-room" : roomId;
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

        public void SendShootRequest(Vector2 position, Vector2 direction, string weaponType)
        {
            SendUdpInput(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ShootRequest,
                ["PlayerID"] = ClientPlayerId,
                ["Position"] = new JObject
                {
                    ["X"] = position.x,
                    ["Y"] = position.y
                },
                ["Direction"] = new JObject
                {
                    ["X"] = direction.x,
                    ["Y"] = direction.y
                },
                ["WeaponType"] = string.IsNullOrWhiteSpace(weaponType) ? "Unknown" : weaponType
            }, DeliveryMethod.Unreliable);
        }

        public void SendGrenadeThrow(Vector2 position, Vector2 direction, string grenadeType)
        {
            SendUdpInput(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.GrenadeThrow,
                ["PlayerID"] = ClientPlayerId,
                ["Position"] = new JObject
                {
                    ["X"] = position.x,
                    ["Y"] = position.y
                },
                ["Direction"] = new JObject
                {
                    ["X"] = direction.x,
                    ["Y"] = direction.y
                },
                ["GrenadeType"] = string.IsNullOrWhiteSpace(grenadeType) ? "Normal" : grenadeType
            }, DeliveryMethod.Unreliable);
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
    }

    public static class JObjectExtensions
    {
        public static string GetStringOrNull(this JObject obj, string key)
        {
            return obj.TryGetValue(key, out JToken token) ? token.ToString() : null;
        }
    }
}
