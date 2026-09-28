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
using OpenGS.Network;
using OpenGSCore; // OpenGSCoreのMatchRoomMessageなどを使用

namespace OpenGS
{
    public class ClientNetworkManager : MonoBehaviour
    {
        public event Action<JObject> UdpMessageReceived;
        /// <summary>Lobby TCP messages, including room and loading notifications not yet mapped to a dedicated event.</summary>
        public event Action<JObject> TcpMessageReceived;
        /// <summary>
        /// Rulings the server made about a match, whichever channel carried them.
        /// <para>
        /// These arrive on the lobby stream, because the authoritative broadcasts
        /// go out through the lobby session rather than the realtime one. The
        /// match scripts used to subscribe to the realtime event only, so a
        /// ruling the server had already made was delivered to nobody.
        /// </para>
        /// </summary>
        public event Action<JObject> ServerMatchRulingReceived;
        /// <summary>The last answer to a match status question this client asked.</summary>
        public JObject LastMatchStatusResponse { get; private set; }
        /// <summary>
        /// The room the server last said this client was admitted to, with the
        /// state it stood at. Null until the admission is confirmed.
        /// </summary>
        public JObject LastMatchRoomInfo { get; private set; }
        public event Action<JObject> MatchStatusResponseReceived;
        /// <summary>試合UDPの接続状態。切断中UIや入力停止処理から購読する。</summary>
        public event Action<bool, string> MatchUdpConnectionChanged;

        [Header("Server Settings")]
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int tcpPort = 60000; // Lobby TCP
        [SerializeField] private int udpPort = 63000; // Match UDP (MatchServerV2)
        
        [Header("Client State")]
        public string ClientPlayerId { get; private set; } = Guid.NewGuid().ToString("N");
        public string CurrentMatchRoomId { get; private set; } = string.Empty;
        public JObject LastDailyListResponse { get; private set; }
        public JObject LastDailyClaimResponse { get; private set; }
        public JObject LastGuildListResponse { get; private set; }
        public JObject LastGuildInfoResponse { get; private set; }
        public JObject LastGuildRoleResponse { get; private set; }
        [Tooltip("Enable detailed UDP receive logs for match traffic. Non-verbose match warnings still remain visible.")]
        [SerializeField] private bool verboseUdpLogs = false;

        // LiteNetLib UDP Client
        private NetManager _netClient;
        private EventBasedNetListener _listener;
        private NetPeer _serverPeer;

        public bool IsMatchUdpConnected =>
            _serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected;

        // TCP Client (Lobby/Match 初期接続用)
        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private const int TcpBufferSize = 8192; // 8KB
        private readonly StringBuilder _tcpMessageBuffer = new StringBuilder();
        private int _tcpSessionVersion;
        private Coroutine _tcpReconnectRoutine;

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
        public event Action<JObject> GuildCreateResponseReceived;
        public event Action<JObject> GuildJoinResponseReceived;
        public event Action<JObject> GuildLeaveResponseReceived;
        public event Action<JObject> GuildInviteResponseReceived;
        public event Action<JObject> GuildInviteNotificationReceived;
        public event Action<JObject> GuildKickResponseReceived;
        public event Action<JObject> GuildKickNotificationReceived;
        public event Action<JObject> GuildChatNotificationReceived;

        // MatchRoomManagerへの参照
        private MatchRoomManager _matchRoomManager;
        private NetworkRequestClient _requestClient;
        private Coroutine _matchConnectRoutine;
        private bool _matchUdpConnectAttempted;
        private bool _isShuttingDown;
        private bool _udpClientStarted;
        private float _lastUdpUnavailableWarningTime = -1f;
        private OpenGS.Network.LagCompensationManager _lagCompensationManager;
        private float _nextLagManagerLookupTime;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(serverIp)) serverIp = "127.0.0.1";
            tcpPort = Mathf.Clamp(tcpPort, 1, 65535);
            udpPort = Mathf.Clamp(udpPort, 1, 65535);
        }

        private void Awake()
        {
            serverIp = serverIp?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(serverIp))
            {
                serverIp = "127.0.0.1";
            }

            tcpPort = Mathf.Clamp(tcpPort, 1, 65535);
            udpPort = Mathf.Clamp(udpPort, 1, 65535);
            _listener = new EventBasedNetListener();
            _netClient = new NetManager(_listener);

            // These runtime services are intentionally created here instead of
            // relying on every match scene to carry a hidden helper object.
            // This keeps production and test scenes on the same receive path.
            EnsureRuntimeNetworkServices();
            FriendListResponseReceived += HandleFriendListResponse;
            FriendRequestNotificationReceived += HandleFriendRequestNotification;
            if (GetComponent<OpenGS.OnlineMetaStateStore>() == null)
            {
                gameObject.AddComponent<OpenGS.OnlineMetaStateStore>();
            }
            
            _listener.NetworkReceiveEvent += OnNetworkReceive;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _listener.NetworkErrorEvent += OnNetworkError;

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
            if (_isShuttingDown) return;
            if (string.IsNullOrWhiteSpace(ClientPlayerId))
            {
                ClientPlayerId = Guid.NewGuid().ToString("N");
            }
            _ = ConnectToLobbyTcpServer();
            _matchConnectRoutine = StartCoroutine(ConnectToMatchUdpWhenReady());
        }

        private void Update()
        {
            _netClient?.PollEvents(); // LiteNetLibのイベントをポーリング
            // TCPデータ受信は非同期で処理するため、ここではポーリング不要
        }

        private void OnDestroy()
        {
            FriendListResponseReceived -= HandleFriendListResponse;
            FriendRequestNotificationReceived -= HandleFriendRequestNotification;
            if (_listener != null)
            {
                _listener.NetworkReceiveEvent -= OnNetworkReceive;
                _listener.PeerConnectedEvent -= OnPeerConnected;
                _listener.PeerDisconnectedEvent -= OnPeerDisconnected;
                _listener.NetworkErrorEvent -= OnNetworkError;
            }
            _isShuttingDown = true;
            DisconnectAll();
        }

        #region TCP Lobby Connection

        private async System.Threading.Tasks.Task ConnectToLobbyTcpServer()
        {
            var sessionVersion = 0;
            TcpClient connectingClient = null;
            try
            {
                // A previous connection may have ended halfway through a
                // packet. Never prepend that stale fragment to a new session.
                _tcpMessageBuffer.Clear();
                sessionVersion = ++_tcpSessionVersion;
                _requestClient?.FailPendingRequests("Lobby TCP session was replaced.");
                _tcpStream?.Dispose();
                _tcpClient?.Close();
                _tcpClient?.Dispose();
                connectingClient = new TcpClient();
                _tcpClient = connectingClient;
                Debug.Log($"[ClientNetwork] Connecting to Lobby TCP {serverIp}:{tcpPort}...");
                await connectingClient.ConnectAsync(serverIp, tcpPort);

                if (_isShuttingDown || sessionVersion != _tcpSessionVersion || !ReferenceEquals(_tcpClient, connectingClient))
                {
                    connectingClient.Close();
                    connectingClient.Dispose();
                    return;
                }

                _tcpStream = connectingClient.GetStream();
                Debug.Log("[ClientNetwork] Connected to Lobby TCP server.");

                // サーバーからの非同期受信を開始
                _ = ReceiveTcpDataAsync(_tcpClient, _tcpStream, sessionVersion);

                // ログイン要求などを送信する（簡略化のためここでは省略）
                SendTcpMessage(new JObject
                {
                    ["MessageType"] = MessageType.LoginRequest, // MessageTypeを使用
                    ["PlayerID"] = ClientPlayerId,
                    ["PlayerName"] = CreateClientPlayerName()
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientNetwork] Failed to connect to Lobby TCP server: {ex.Message}");
                if (connectingClient != null && ReferenceEquals(_tcpClient, connectingClient))
                {
                    _tcpStream = null;
                    _tcpClient = null;
                    connectingClient.Dispose();
                }

                if (!_isShuttingDown && sessionVersion == _tcpSessionVersion)
                {
                    ScheduleLobbyTcpReconnect();
                }
            }
        }

        private static void HandleFriendListResponse(JObject response)
        {
            FriendManager.Instance?.ApplyServerFriendList(response);
        }

        private static void HandleFriendRequestNotification(JObject notification)
        {
            FriendManager.Instance?.ApplyServerFriendRequestNotification(notification);
        }

        private void EnsureRuntimeNetworkServices()
        {
            _lagCompensationManager = GetComponent<OpenGS.Network.LagCompensationManager>();
            if (_lagCompensationManager == null)
            {
                _lagCompensationManager = gameObject.AddComponent<OpenGS.Network.LagCompensationManager>();
            }

            if (GetComponent<OpenGS.NetworkCombatReplay>() == null)
            {
                gameObject.AddComponent<OpenGS.NetworkCombatReplay>();
            }
        }

        private string CreateClientPlayerName()
        {
            var playerId = ClientPlayerId ?? string.Empty;
            var suffixLength = Math.Min(4, playerId.Length);
            var suffix = suffixLength > 0 ? playerId.Substring(0, suffixLength) : "anonymous";
            return "UnityClient_" + suffix;
        }

        private async System.Threading.Tasks.Task ReceiveTcpDataAsync(
            TcpClient tcpClient,
            NetworkStream tcpStream,
            int sessionVersion)
        {
            try
            {
                var receiveBuffer = new byte[TcpBufferSize];
                while (sessionVersion == _tcpSessionVersion &&
                       ReferenceEquals(_tcpClient, tcpClient) &&
                       tcpClient.Connected)
                {
                    int bytesRead = await tcpStream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    if (bytesRead == 0)
                    {
                        Debug.Log("[ClientNetwork] Lobby TCP server disconnected.");
                        break;
                    }

                    if (sessionVersion != _tcpSessionVersion ||
                        !ReferenceEquals(_tcpClient, tcpClient))
                    {
                        break;
                    }

                    string chunk = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    _tcpMessageBuffer.Append(chunk);

                    if (_tcpMessageBuffer.Length > 1024 * 1024)
                    {
                        Debug.LogWarning("[ClientNetwork] TCP receive buffer exceeded 1 MiB; dropping incomplete data.");
                        _tcpMessageBuffer.Clear();
                        continue;
                    }

                    string fullBuffer = _tcpMessageBuffer.ToString();
                    string[] parts = fullBuffer.Split('\x1F');

                    if (parts.Length == 1)
                    {
                        // TCP is a stream: without the delimiter this is only
                        // an incomplete frame, even if it currently resembles JSON.
                        continue;
                    }

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        if (TryParseTcpPacket(parts[i], out JObject message))
                        {
                            TryProcessTcpMessage(message);
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
            finally
            {
                if (!_isShuttingDown &&
                    sessionVersion == _tcpSessionVersion &&
                    ReferenceEquals(_tcpClient, tcpClient))
                {
                    _requestClient?.FailPendingRequests("Lobby TCP connection was lost.");
                    ScheduleLobbyTcpReconnect();
                }
            }
        }

        private void ScheduleLobbyTcpReconnect()
        {
            if (_isShuttingDown || _tcpReconnectRoutine != null)
            {
                return;
            }

            _tcpReconnectRoutine = StartCoroutine(ReconnectLobbyTcpAfterDisconnect());
        }

        private IEnumerator ReconnectLobbyTcpAfterDisconnect()
        {
            yield return new WaitForSecondsRealtime(1f);
            _tcpReconnectRoutine = null;

            if (!_isShuttingDown)
            {
                _ = ConnectToLobbyTcpServer();
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
            if (message == null)
            {
                Debug.LogWarning("[ClientNetwork] Ignoring null TCP message.");
                return;
            }

            InvokeSafely(TcpMessageReceived, message, nameof(TcpMessageReceived));

            if (_requestClient != null && _requestClient.HandleIncomingMessage(message))
            {
                return;
            }

            string messageType = MessageType.Normalize(message.GetStringOrNull("MessageType"));
            switch (messageType)
            {
                case MessageType.EncryptKey:
                    HandleEncryptionKey(message);
                    break;
                case MessageType.LoginResponse:
                    bool success = ReadBool(message, "Success", true);
                    if (success)
                    {
                        string resolvedPlayerId = message.GetStringOrNull("PlayerID") ?? message.GetStringOrNull("GlobalUserId");
                        if (!string.IsNullOrEmpty(resolvedPlayerId))
                        {
                            ClientPlayerId = resolvedPlayerId;
                        }
                        Debug.Log($"[ClientNetwork] Login successful. PlayerID: {ClientPlayerId}");
                        RequestDailyList();
                        RequestGuildList();
                        RequestFriendList();
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
                case "MatchServerInformationNotification":
                    HandleMatchServerInfoResponse(message);
                    break;
                case MessageType.FriendRequestResponse:
                    InvokeSafely(FriendRequestResponseReceived, message, nameof(FriendRequestResponseReceived));
                    break;
                case MessageType.FriendApproveResponse:
                    InvokeSafely(FriendApproveResponseReceived, message, nameof(FriendApproveResponseReceived));
                    if (ReadBool(message, "Success"))
                    {
                        RequestFriendList();
                    }
                    break;
                case MessageType.FriendListResponse:
                    InvokeSafely(FriendListResponseReceived, message, nameof(FriendListResponseReceived));
                    break;
                case MessageType.FriendRequestNotification:
                    InvokeSafely(FriendRequestNotificationReceived, message, nameof(FriendRequestNotificationReceived));
                    break;
                case MessageType.DailyListResponse:
                    LastDailyListResponse = message;
                    InvokeSafely(DailyListResponseReceived, message, nameof(DailyListResponseReceived));
                    break;
                case MessageType.DailyProgressResponse:
                    InvokeSafely(DailyProgressResponseReceived, message, nameof(DailyProgressResponseReceived));
                    break;
                case MessageType.DailyClaimResponse:
                    LastDailyClaimResponse = message;
                    InvokeSafely(DailyClaimResponseReceived, message, nameof(DailyClaimResponseReceived));
                    break;
                case MessageType.GuildRoleResponse:
                    LastGuildRoleResponse = message;
                    InvokeSafely(GuildRoleResponseReceived, message, nameof(GuildRoleResponseReceived));
                    break;
                case MessageType.GuildListResponse:
                    LastGuildListResponse = message;
                    InvokeSafely(GuildListResponseReceived, message, nameof(GuildListResponseReceived));
                    break;
                case MessageType.GuildInfoResponse:
                    LastGuildInfoResponse = message;
                    InvokeSafely(GuildInfoResponseReceived, message, nameof(GuildInfoResponseReceived));
                    break;
                case MessageType.GuildCreateResponse:
                    InvokeSafely(GuildCreateResponseReceived, message, nameof(GuildCreateResponseReceived));
                    break;
                case MessageType.GuildJoinResponse:
                    InvokeSafely(GuildJoinResponseReceived, message, nameof(GuildJoinResponseReceived));
                    break;
                case MessageType.GuildLeaveResponse:
                    InvokeSafely(GuildLeaveResponseReceived, message, nameof(GuildLeaveResponseReceived));
                    break;
                case MessageType.GuildInviteResponse:
                    InvokeSafely(GuildInviteResponseReceived, message, nameof(GuildInviteResponseReceived));
                    break;
                case MessageType.GuildInviteNotification:
                    InvokeSafely(GuildInviteNotificationReceived, message, nameof(GuildInviteNotificationReceived));
                    break;
                case MessageType.GuildKickResponse:
                    InvokeSafely(GuildKickResponseReceived, message, nameof(GuildKickResponseReceived));
                    break;
                case MessageType.GuildKickNotification:
                    InvokeSafely(GuildKickNotificationReceived, message, nameof(GuildKickNotificationReceived));
                    break;
                case MessageType.GuildChatNotification:
                    InvokeSafely(GuildChatNotificationReceived, message, nameof(GuildChatNotificationReceived));
                    break;
                // The pickup ruling arrives on the lobby stream, not on the
                // realtime channel, because the authoritative broadcasts all go
                // out through the lobby session. It used to fall through to the
                // branch below, which only knows how to turn a message into a
                // game event, so a client that had applied a pickup
                // optimistically was never told whether it had been granted and
                // the local timeout took the buff back either way.
                case MessageType.FieldItemPickup:
                    HandleFieldItemPickupResponse(message);
                    break;
                // The server settles a player's health and death over the lobby
                // stream, because the authoritative broadcasts go out through
                // the lobby session. These were handled on the realtime path
                // only, so the ruling never reached the client and the local
                // prediction stood.
                case MessageType.PlayerDamage:
                case MessageType.PlayerDamaged:
                case MessageType.PlayerDeath:
                case MessageType.PlayerKilled:
                    ApplyAuthoritativePlayerRuling(messageType, message);
                    break;
                // A claim about a weapon on the ground is answered with a ruling
                // under a different name, and the ruling is the only thing that
                // makes the other clients honour a claim the server accepted.
                case MessageType.WeaponReserved:
                case MessageType.WeaponReleased:
                case MessageType.WeaponDropped:
                case MessageType.ItemUsed:
                case MessageType.ItemUseRefused:
                case MessageType.FieldItemSpawn:
                case MessageType.FieldItemDespawn:
                case MessageType.FieldItemStateSync:
                    DispatchServerMatchRuling(message);
                    break;
                // The answer to a question this client asked. The server used to
                // send it under a name no client dispatched on, and to everybody
                // in the room rather than to the one that asked, so a client that
                // asked was told nothing.
                case MessageType.MatchStatus:
                    LastMatchStatusResponse = message;
                    InvokeSafely(MatchStatusResponseReceived, message, nameof(MatchStatusResponseReceived));
                    break;
                // 他のTCPメッセージタイプをここで処理
                default:
                    var gameEvent = NetworkEventDeserializer.Deserialize(message);
                    if (gameEvent != null)
                    {
                        PublishGameEvent(gameEvent);
                    }
                    else
                    {
                        Debug.Log($"[ClientNetwork] Received unknown TCP message: {message}");
                    }
                    break;
            }
        }

        private static void HandleEncryptionKey(JObject message)
        {
            var publicKey = message.GetStringOrNull("RSAPublicKey");
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                Debug.LogWarning("[ClientNetwork] EncryptKey response did not contain RSAPublicKey.");
                return;
            }

            try
            {
                EncryptManager.Instance.SetRSAPublicKey(publicKey);
                Debug.Log("[ClientNetwork] Lobby RSA public key accepted.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ClientNetwork] Failed to apply lobby RSA public key: {ex.Message}");
            }
        }

        private void TryProcessTcpMessage(JObject message)
        {
            try
            {
                ProcessTcpMessage(message);
            }
            catch (Exception ex)
            {
                var messageType = message.GetStringOrNull("MessageType") ?? "<missing>";
                Debug.LogError($"[ClientNetwork] TCP listener failed for {messageType}: {ex}");
            }
        }

        public void SendTcpMessage(JObject message)
        {
            _ = TrySendTcpMessage(message);
        }

        /// <summary>
        /// Asks the server what it thinks the state of the match is.
        /// <para>
        /// The answer is now sent to the player that asked and under a name this
        /// client reads. Before, the question had no way to be asked from here and
        /// the answer was sent to the whole room under a label nothing dispatched
        /// on, so a client could neither ask nor be told.
        /// </para>
        /// </summary>
        public void RequestMatchStatus()
        {
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.MatchStatusRequest,
                ["PlayerID"] = ClientPlayerId
            });
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

        public void CreateGuild(string guildName)
        {
            if (string.IsNullOrWhiteSpace(guildName)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildCreateRequest,
                ["GuildName"] = guildName,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void JoinGuild(string guildName)
        {
            if (string.IsNullOrWhiteSpace(guildName)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildJoinRequest,
                ["GuildName"] = guildName,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void LeaveGuild(string guildName)
        {
            if (string.IsNullOrWhiteSpace(guildName)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildLeaveRequest,
                ["GuildName"] = guildName,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void InviteToGuild(string guildName, string targetPlayerId)
        {
            if (string.IsNullOrWhiteSpace(guildName) || string.IsNullOrWhiteSpace(targetPlayerId)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildInviteRequest,
                ["GuildName"] = guildName,
                ["TargetPlayerId"] = targetPlayerId,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void KickFromGuild(string guildName, string memberId)
        {
            if (string.IsNullOrWhiteSpace(guildName) || string.IsNullOrWhiteSpace(memberId)) return;
            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildKickRequest,
                ["GuildName"] = guildName,
                ["MemberId"] = memberId,
                ["PlayerID"] = ClientPlayerId
            });
        }

        public void SendGuildChat(string guildName, string message)
        {
            if (string.IsNullOrWhiteSpace(guildName) || string.IsNullOrWhiteSpace(message)) return;
            message = message.Trim();
            if (message.Length > 200)
            {
                message = message.Substring(0, 200);
            }

            SendTcpMessage(new JObject
            {
                ["MessageType"] = MessageType.GuildChatRequest,
                ["GuildName"] = guildName,
                ["Message"] = message,
                ["PlayerID"] = ClientPlayerId
            });
        }

        private bool TrySendTcpMessage(JObject message)
        {
            if (_isShuttingDown)
            {
                return false;
            }

            if (message == null)
            {
                Debug.LogWarning("[ClientNetwork] Ignoring null TCP message.");
                return false;
            }

            if (_tcpStream != null && _tcpStream.CanWrite)
            {
                var messageType = MessageType.Normalize(message.GetStringOrNull("MessageType"));
                if (string.IsNullOrWhiteSpace(messageType))
                {
                    Debug.LogWarning("[ClientNetwork] Ignoring TCP message without MessageType.");
                    return false;
                }

                message["MessageType"] = messageType;
                string jsonString = message.ToString(Formatting.None);
                // ClientSession parses TCP frames as: "JS" + JSON + 0x1F.
                // Sending JSON without the identifier makes the server reject
                // every request as an unknown transport frame.
                byte[] prefix = Encoding.UTF8.GetBytes("JS");
                byte[] payload = Encoding.UTF8.GetBytes(jsonString);
                byte[] separator = { 0x1F };
                byte[] data = new byte[prefix.Length + payload.Length + separator.Length];
                Buffer.BlockCopy(prefix, 0, data, 0, prefix.Length);
                Buffer.BlockCopy(payload, 0, data, prefix.Length, payload.Length);
                Buffer.BlockCopy(separator, 0, data, prefix.Length + payload.Length, separator.Length);
                try
                {
                    _tcpStream.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ClientNetwork] Failed to send TCP message {messageType}: {ex.Message}");
                    ScheduleLobbyTcpReconnect();
                    return false;
                }
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
            if (string.IsNullOrWhiteSpace(targetPlayerId))
            {
                Debug.LogWarning("[ClientNetwork] PlayerInfoRequest requires a target player ID.");
                return;
            }

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
            bool success = ReadBool(response, "Success");
            string targetPlayerId = response.GetStringOrNull("PlayerID") ?? response.GetStringOrNull("TargetPlayerID");

            if (success)
            {
                Debug.Log($"[ClientNetwork] PlayerInfoResponse for {targetPlayerId}: DisplayName={response.GetStringOrNull("DisplayName")}, Level={ReadInt(response, "Level")}, XP={ReadInt(response, "XP")}");
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
            var port = ReadNullableInt(response, "Port");
            var udp = ReadNullableInt(response, "UdpPort");
            var udpToken = response.GetStringOrNull("UdpToken");
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

            if (!string.IsNullOrWhiteSpace(udpToken))
            {
                OnlineManager.Instance.MatchServerInfo.UdpToken = udpToken;
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
            targetPlayerId = targetPlayerId?.Trim();
            if (string.IsNullOrWhiteSpace(targetPlayerId))
            {
                Debug.LogWarning("[ClientNetwork] FriendRequest requires a target player ID.");
                return;
            }

            if (_isShuttingDown)
            {
                Debug.LogWarning("[ClientNetwork] FriendRequest ignored while network is shutting down.");
                return;
            }

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

                InvokeSafely(FriendRequestResponseReceived, legacyResponse, nameof(FriendRequestResponseReceived));
            }
            catch (NetworkRequestException ex) when (string.Equals(ex.ErrorCode, "UnknownRoute", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[ClientNetwork] Foundation friend request failed. Falling back to legacy request. {ex.Message}");
                SendLegacyFriendRequest(targetPlayerId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ClientNetwork] Foundation friend request failed without fallback: {ex.Message}");
            }
        }

        private void SendLegacyFriendRequest(string targetPlayerId)
        {
            if (_isShuttingDown)
            {
                return;
            }

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
            requestPlayerId = requestPlayerId?.Trim();
            if (string.IsNullOrWhiteSpace(requestPlayerId))
            {
                Debug.LogWarning("[ClientNetwork] FriendApproveRequest requires a request player ID.");
                return;
            }

            if (_isShuttingDown)
            {
                Debug.LogWarning("[ClientNetwork] Friend approval ignored while network is shutting down.");
                return;
            }

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
            if (_isShuttingDown)
            {
                return;
            }

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

            while (!_matchUdpConnectAttempted &&
                   (!OnlineManager.Instance.MatchServerInfo.HasEndpoint() ||
                    string.IsNullOrWhiteSpace(OnlineManager.Instance.MatchServerInfo.UdpToken)))
            {
                if (Time.realtimeSinceStartup - startTime >= timeoutSeconds)
                {
                    Debug.LogWarning("[ClientNetwork] Match UDP authorization was not provided in time; UDP connection will remain disabled.");
                    break;
                }

                yield return null;
            }

            if (!_matchUdpConnectAttempted &&
                !string.IsNullOrWhiteSpace(OnlineManager.Instance.MatchServerInfo.UdpToken))
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
            if (!_udpClientStarted)
            {
                _udpClientStarted = _netClient.Start();
                if (!_udpClientStarted)
                {
                    _matchUdpConnectAttempted = false;
                    Debug.LogError("[ClientNetwork] Failed to start Match UDP client. Retrying shortly.");
                    StartCoroutine(RetryMatchUdpAfterStartFailure());
                    return;
                }
            }
            Debug.Log($"[ClientNetwork] Connecting to Match UDP {resolvedIp}:{resolvedUdpPort} with PlayerID: {ClientPlayerId}...");
            _netClient.Connect(resolvedIp, resolvedUdpPort, "OpenGS"); // "OpenGS"は接続キー
        }

        private IEnumerator RetryMatchUdpAfterStartFailure()
        {
            yield return new WaitForSecondsRealtime(1f);

            if (!_isShuttingDown && !_matchUdpConnectAttempted)
            {
                ConnectToMatchUdpServer();
            }
        }

        private void OnPeerConnected(NetPeer peer)
        {
            _serverPeer = peer;
            Debug.Log("[ClientNetwork] Connected to Match UDP server.");
            InvokeSafely(MatchUdpConnectionChanged, true, string.Empty, nameof(MatchUdpConnectionChanged));

            // サーバーにクライアントのPlayerIDを通知 (サーバー側のOnPeerConnectedでID取得できない場合のため)
            SendUdpInput(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ClientConnect,
                ["PlayerID"] = ClientPlayerId,
                ["RoomID"] = CurrentMatchRoomId ?? string.Empty,
                ["UdpToken"] = OnlineManager.Instance.MatchServerInfo.UdpToken
            }, DeliveryMethod.ReliableOrdered);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"[ClientNetwork] Disconnected from Match UDP server: {disconnectInfo.Reason}");
            InvokeSafely(MatchUdpConnectionChanged, false, disconnectInfo.Reason.ToString(), nameof(MatchUdpConnectionChanged));
            if (_serverPeer == peer)
            {
                _serverPeer = null;
            }

            // Keep the room identity for automatic rejoin. The match server
            // removes its player-room mapping on UDP disconnect and needs the
            // RoomID on the next ClientConnect packet to restore it.
            _matchUdpConnectAttempted = false;

            if (!_isShuttingDown && _matchConnectRoutine == null)
            {
                _matchConnectRoutine = StartCoroutine(ReconnectMatchUdpAfterDisconnect());
            }
        }

        private IEnumerator ReconnectMatchUdpAfterDisconnect()
        {
            yield return new WaitForSecondsRealtime(1f);

            if (!_isShuttingDown && !_matchUdpConnectAttempted)
            {
                ConnectToMatchUdpServer();
            }

            _matchConnectRoutine = null;
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
            if (message == null)
            {
                Debug.LogWarning("[ClientNetwork] Ignoring null UDP message.");
                return;
            }

            // Accept legacy casing/aliases consistently with the TCP path.
            string messageType = MessageType.Normalize(message.GetStringOrNull("MessageType"));
            if (string.IsNullOrWhiteSpace(messageType))
            {
                Debug.LogWarning("[ClientNetwork] Ignoring UDP message without MessageType.");
                return;
            }

            InvokeSafely(UdpMessageReceived, message, nameof(UdpMessageReceived));

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
                    // The server confirms the admission here, on the channel the
                    // match is played on, and says which room and whether it is
                    // running. It used to be confirmed on the lobby stream only,
                    // so a client that had just connected did not know which room
                    // it was in until something else happened to say.
                    CurrentMatchRoomId = message.GetStringOrNull("RoomID") ?? CurrentMatchRoomId;
                    LastMatchRoomInfo = message;
                    Debug.Log(
                        $"[ClientNetwork] Joined Match Room [{FormatRoomTag(CurrentMatchRoomId)}] " +
                        $"playing={message["IsPlaying"]}, players={message["PlayerCount"]}");
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
                case "ServerTransformState":
                    HandleServerTransformState(message);
                    break;
                case RUDPMessageTypes.PlayerDeath:
                case RUDPMessageTypes.PlayerKilled:
                case RUDPMessageTypes.PlayerDamage:
                case RUDPMessageTypes.PlayerDamaged:
                    ApplyAuthoritativePlayerRuling(messageType, message);
                    break;
                case RUDPMessageTypes.PlayerKill:
                case RUDPMessageTypes.PlayerAssist:
                case RUDPMessageTypes.KillScoreUpdate:
                case RUDPMessageTypes.StreakUpdate:
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
                case RUDPMessageTypes.MatchTimeSync:
                case RUDPMessageTypes.RoundStart:
                case RUDPMessageTypes.RoundEnd:
                case RUDPMessageTypes.WarmupStart:
                case RUDPMessageTypes.WarmupEnd:
                case RUDPMessageTypes.PlayerRespawn:
                case RUDPMessageTypes.RespawnCountdown:
                case RUDPMessageTypes.PlayerJoined:
                case RUDPMessageTypes.PlayerLeft:
                case RUDPMessageTypes.PlayerTeamSwitch:
                case RUDPMessageTypes.PlayerSpectating:
                case RUDPMessageTypes.PlayerRevive:
                case RUDPMessageTypes.WeaponChange:
                case RUDPMessageTypes.AmmoUpdate:
                case RUDPMessageTypes.PlayerReload:
                case RUDPMessageTypes.PlayerMelee:
                case RUDPMessageTypes.PlayerBuff:
                case RUDPMessageTypes.PlayerDebuff:
                case RUDPMessageTypes.BuffExpired:
                case RUDPMessageTypes.ItemPickup:
                    // The server ruling on a pickup, not a fresh request:
                    // resolve what the client applied optimistically and let it
                    // run the revert if the claim was refused.
                    HandleFieldItemPickupResponse(message);
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
                // これらは AbstractMatchMainScript が生JSON経路で処理する。
                // ゲームイベントデシリアライズを通すと未対応イベント警告になるため、ここでは二重配信しない。
                case RUDPMessageTypes.ItemUse:
                case RUDPMessageTypes.GameStateSync:
                    break;
                // The server's answer to a claim, a drop, a spend, or an item
                // appearing or going. The server sends these over the lobby session
                // rather than the realtime one, so the match scripts were never
                // told about them.
                case RUDPMessageTypes.WeaponReserved:
                case RUDPMessageTypes.WeaponReleased:
                case RUDPMessageTypes.WeaponDropped:
                case RUDPMessageTypes.ItemUsed:
                case RUDPMessageTypes.ItemUseRefused:
                case RUDPMessageTypes.ItemSpawn:
                case RUDPMessageTypes.ItemDespawn:
                case RUDPMessageTypes.ItemStateSync:
                    DispatchServerMatchRuling(message);
                    break;
                case RUDPMessageTypes.PingRequest:
                {
                    var pong = new JObject();
                    pong["MessageType"] = MessageType.PingResponse;
                    pong["ClientTimestamp"] = message["ClientTimestamp"];
                    SendUdpInput(pong);
                    break;
                }
                case RUDPMessageTypes.PingResponse:
                    PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
                    break;
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

            try
            {
                ProcessUdpMessage(message);
            }
            catch (Exception ex)
            {
                var messageType = message.GetStringOrNull("MessageType") ?? "<missing>";
                Debug.LogError($"[ClientNetwork] Failed to replay UDP message {messageType}: {ex}");
            }
        }

        /// <summary>
        /// Applies the server's ruling on a field item pickup.
        /// <para>
        /// A pickup is applied locally the moment it is touched and only then
        /// asked about, so this reply either confirms the claim or takes the
        /// effect back. The server also decides the duration, which is not
        /// necessarily the one the client assumed, so a grant re-applies with
        /// the server's value.
        /// </para>
        /// </summary>
        /// <summary>
        /// Adopts the health the server reports on the local player.
        /// <para>
        /// The server owns health, so its number is the truth and the local one
        /// is only a prediction made when the player expected to be hit. The two
        /// drift apart otherwise: a miss the client predicted but the server did
        /// not count, or a delay in a message, and the local health bar stops
        /// agreeing with whether the player is actually alive.
        /// </para>
        /// <para>
        /// Only the local player is corrected. A damage message about another
        /// player drives their own representation, and the local bar must not be
        /// moved by someone else's hit.
        /// </para>
        /// </summary>
        /// <summary>
        /// Applies a death the server ruled on.
        /// <para>
        /// The server sends PlayerKilled, while the client side match scripts
        /// listen for PlayerDeath, so both names are routed here. The local
        /// player is marked dead without a fresh report, because the server has
        /// already ruled on it and reporting again would be a claim.
        /// </para>
        /// </summary>
        /// <summary>
        /// Applies a ruling the server made about a player's health or death.
        /// <para>
        /// The ruling reaches the client on the lobby stream, because the
        /// authoritative broadcasts all go out through the lobby session rather
        /// than the realtime one. It used to be handled only on the realtime
        /// path, so the handler was written and then never ran: health stayed
        /// whatever the client predicted and a death was settled by the client's
        /// own claim. Both channels now come through here so there is one place
        /// that decides what the server said, whichever path it arrived on.
        /// </para>
        /// </summary>
        /// <summary>
        /// Hands a ruling about a match to whoever is listening for one.
        /// <para>
        /// The realtime path raises the realtime event and the lobby path raises
        /// the lobby one, so a subscriber to either saw a ruling only when it
        /// happened to arrive on the channel it was watching. The server sends
        /// all of these over the lobby session, so the realtime subscribers saw
        /// none of them. Both paths come through here so the ruling reaches the
        /// match scripts whichever way it was delivered.
        /// </para>
        /// </summary>
        private void DispatchServerMatchRuling(JObject message)
        {
            if (message == null)
            {
                return;
            }

            InvokeSafely(ServerMatchRulingReceived, message, nameof(ServerMatchRulingReceived));
        }

        private void ApplyAuthoritativePlayerRuling(string messageType, JObject message)
        {
            if (message == null)
            {
                return;
            }

            switch (messageType)
            {
                case RUDPMessageTypes.PlayerDeath:
                case RUDPMessageTypes.PlayerKilled:
                    HandleAuthoritativeDeath(message);
                    break;
                case RUDPMessageTypes.PlayerDamage:
                case RUDPMessageTypes.PlayerDamaged:
                    ApplyAuthoritativeHealth(message);
                    break;
                default:
                    Debug.LogWarning($"[ClientNetwork] '{messageType}' is not a player health or death ruling.");
                    return;
            }

            PublishGameEvent(NetworkEventDeserializer.Deserialize(message));
        }

        private void HandleAuthoritativeDeath(JObject message)
        {
            if (message == null)
            {
                return;
            }

            // The dead player is named by KilledPlayerID on the server side and
            // by PlayerId on the older death message, so both are read.
            var deadId = message["KilledPlayerID"]?.ToString()
                ?? message["DeadPlayerID"]?.ToString()
                ?? message["PlayerId"]?.ToString()
                ?? message["PlayerID"]?.ToString();
            if (string.IsNullOrWhiteSpace(deadId))
            {
                return;
            }

            var registry = PlayerRegistry.Instance;
            if (registry == null || !Guid.TryParse(deadId, out var parsed))
            {
                return;
            }

            if (!registry.TryGetPlayer(parsed, out var player) || player == null)
            {
                return;
            }

            if (player is IDamageable damageable)
            {
                // Health of zero is the server's ruling that the player is out.
                damageable.ApplyServerHealth(0, 0);
            }
        }

        private void ApplyAuthoritativeHealth(JObject message)
        {
            if (message == null)
            {
                return;
            }

            if (message["RemainingHealth"] == null)
            {
                // An older server that does not report health. The local value
                // is left alone rather than guessed at.
                return;
            }

            var remaining = message["RemainingHealth"]?.ToObject<int>() ?? 0;
            var maximum = message["MaxHealth"]?.ToObject<int>() ?? 0;

            var registry = PlayerRegistry.Instance;
            if (registry == null || !Guid.TryParse(ClientPlayerId, out var localId))
            {
                return;
            }

            if (!registry.TryGetPlayer(localId, out var player))
            {
                return;
            }

            // AbstractPlayer and PlayerAgent are separate hierarchies, so the
            // contract is the shared interface rather than a cast to one class.
            // AbstractPlayer and PlayerAgent are separate hierarchies and only one
            // of them keeps a health bar, so the server value is adopted through
            // the interface rather than by casting to a class.
            (player as IDamageable)?.ApplyServerHealth(remaining, maximum);
        }

        private void HandleFieldItemPickupResponse(JObject message)
        {
            var manager = WorldItemNetworkManager.Instance;
            if (manager == null)
            {
                return;
            }

            var itemId = message.GetStringOrNull("ItemId");
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            var granted = message["Success"]?.Value<bool>() ?? false;
            var duration = message["Duration"]?.Value<float>() ?? 0f;
            var type = OpenGSCore.EFieldItemType.PowerUpItem;
            if (!OpenGSCore.FieldItemTypeNames.TryParse(message.GetStringOrNull("ItemType"), out var parsed))
            {
                parsed = OpenGSCore.EFieldItemType.PowerUpItem;
            }

            type = parsed;

            if (!manager.ResolvePickup(itemId, granted, type, duration))
            {
                // Nothing is waiting on this id, so the reply is either late or
                // belongs to a room this client is not part of.
                return;
            }

            if (!granted)
            {
                Debug.LogWarning($"[ClientNetwork] Server refused the pickup of item '{itemId}'");
            }
        }
        private void HandlePlayerPositionUpdate(JObject message)
        {
            var playerId = message.GetStringOrNull("PlayerID") ?? message.GetStringOrNull("PlayerId");
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var position = message["Position"] as JObject;
            var positionX = ReadFinite(message, "PosX", ReadFinite(message, "PositionX", ReadFinite(position, "X")));
            var positionY = ReadFinite(message, "PosY", ReadFinite(message, "PositionY", ReadFinite(position, "Y")));
            var positionZ = ReadFinite(message, "PosZ", ReadFinite(message, "PositionZ", ReadFinite(position, "Z")));
            var rotation = ReadFinite(message, "Rotation", ReadFinite(message, "RotationZ"));

            var state = new OpenGS.Network.TransformState
            {
                playerId = playerId,
                position = new Vector3(
                    positionX,
                    positionY,
                    positionZ),
                rotation = Quaternion.Euler(0f, 0f, rotation),
                velocity = Vector3.zero,
                timestamp = GetSafeUnityTime(),
                sequenceNumber = ReadByte(message, "SequenceNumber")
            };

            if (!IsFinite(state.position) || !IsFinite(state.velocity) || !IsFinite(state.rotation))
            {
                Debug.LogWarning($"[ClientNetwork] Ignoring non-finite transform state for player '{playerId}'.");
                return;
            }

            if (QuaternionMath.SqrMagnitude(state.rotation) < 0.0001f)
            {
                state.rotation = Quaternion.identity;
            }
            else
            {
                state.rotation = Quaternion.Normalize(state.rotation);
            }

            var lagManager = ResolveLagCompensationManager();
            if (lagManager != null)
            {
                lagManager.OnPlayerStateReceived(state);
                return;
            }

            Debug.Log($"[ClientNetwork] PlayerPositionUpdate received for {playerId}: {state.position}");
        }

        private void HandleServerTransformState(JObject message)
        {
            var playerId = message.GetStringOrNull("PlayerId") ?? message.GetStringOrNull("PlayerID");
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var state = new OpenGS.Network.TransformState
            {
                networkId = ReadUInt(message, "NetworkId"),
                playerId = playerId,
                position = new Vector3(
                    ReadFinite(message, "PositionX"),
                    ReadFinite(message, "PositionY"),
                    ReadFinite(message, "PositionZ")),
                rotation = new Quaternion(
                    ReadFinite(message, "RotationX"),
                    ReadFinite(message, "RotationY"),
                    ReadFinite(message, "RotationZ"),
                    ReadFinite(message, "RotationW", 1f)),
                velocity = new Vector3(
                    ReadFinite(message, "VelX"),
                    ReadFinite(message, "VelY"),
                    ReadFinite(message, "VelZ")),
                // NetworkInterpolation compares against Unity's local Time.time.
                // The server timestamp uses a different clock/domain.
                timestamp = GetSafeUnityTime(),
                sequenceNumber = ReadByte(message, "SequenceNumber")
            };

            if (!IsFinite(state.position) || !IsFinite(state.velocity) || !IsFinite(state.rotation))
            {
                Debug.LogWarning($"[ClientNetwork] Ignoring non-finite server transform state for player '{playerId}'.");
                return;
            }

            if (QuaternionMath.SqrMagnitude(state.rotation) < 0.0001f)
            {
                state.rotation = Quaternion.identity;
            }
            else
            {
                state.rotation = Quaternion.Normalize(state.rotation);
            }

            var lagManager = ResolveLagCompensationManager();
            if (lagManager != null)
            {
                lagManager.OnPlayerStateReceived(state);
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y)
                && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float GetSafeUnityTime()
        {
            var now = Time.time;
            return IsFinite(now) && now >= 0f ? now : 0f;
        }

        private static float ReadFinite(JObject message, string key, float fallback = 0f)
        {
            if (message == null || string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            try
            {
                var value = message[key]?.ToObject<float>();
                return value.HasValue && IsFinite(value.Value) ? value.Value : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static uint ReadUInt(JObject message, string key)
        {
            if (message == null || string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            try
            {
                return message[key]?.ToObject<uint>() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static byte ReadByte(JObject message, string key)
        {
            if (message == null || string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            try
            {
                return message[key]?.ToObject<byte>() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static bool ReadBool(JObject message, string key, bool fallback = false)
        {
            try
            {
                return message?[key]?.ToObject<bool>() ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static int ReadInt(JObject message, string key, int fallback = 0)
        {
            try
            {
                return message?[key]?.ToObject<int>() ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static int? ReadNullableInt(JObject message, string key)
        {
            try
            {
                return message?[key]?.ToObject<int?>();
            }
            catch
            {
                return null;
            }
        }

        private static void ForwardMatchResult(JObject message)
        {
            try
            {
                var generalServer = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                generalServer?.SendMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ClientNetwork] Failed to forward match result: {ex.Message}");
            }
        }

        private OpenGS.Network.LagCompensationManager ResolveLagCompensationManager()
        {
            if (_lagCompensationManager != null)
            {
                return _lagCompensationManager;
            }

            var now = Time.unscaledTime;
            if (!IsFinite(now) || now < 0f)
            {
                return null;
            }

            if (now < _nextLagManagerLookupTime)
            {
                return null;
            }

            _nextLagManagerLookupTime = now + 1f;
            _lagCompensationManager = OpenGS.Network.LagCompensationManager.Instance;
            if (_lagCompensationManager == null)
            {
                _lagCompensationManager = FindFirstObjectByType<OpenGS.Network.LagCompensationManager>();
            }

            return _lagCompensationManager;
        }

        private static void InvokeSafely<T>(Action<T> handlers, T value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ClientNetwork] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<bool, string> handlers, bool value, string reason, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<bool, string> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value, reason);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ClientNetwork] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void PublishGameEvent(AbstractGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return;
            }

            GameEventBroker.PublishUntyped(gameEvent);
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

        public bool SendUdpInput(JObject input, DeliveryMethod method = DeliveryMethod.Unreliable)
        {
            if (_isShuttingDown)
            {
                return false;
            }

            if (input == null)
            {
                Debug.LogWarning("[ClientNetwork] Ignoring null UDP input.");
                return false;
            }

            if (_serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected)
            {
                var messageType = MessageType.Normalize(input.GetStringOrNull("MessageType"));
                if (string.IsNullOrWhiteSpace(messageType))
                {
                    Debug.LogWarning("[ClientNetwork] Ignoring UDP input without MessageType.");
                    return false;
                }

                input["MessageType"] = messageType;
                if (string.IsNullOrWhiteSpace(input["PlayerID"]?.ToString()))
                {
                    if (string.IsNullOrWhiteSpace(ClientPlayerId))
                    {
                        Debug.LogWarning("[ClientNetwork] Ignoring UDP input without PlayerID.");
                        return false;
                    }

                    input["PlayerID"] = ClientPlayerId;
                }
                if (input["RoomID"] == null && !string.IsNullOrEmpty(CurrentMatchRoomId)) input["RoomID"] = CurrentMatchRoomId;

                string jsonString = input.ToString(Formatting.None);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                try
                {
                    _serverPeer.Send(bytes, 0, method);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ClientNetwork] UDP send failed; will retry when connected: {ex.Message}");
                    return false;
                }

                //Debug.Log($"[ClientNetwork] Sent UDP Input: {jsonString}");
                return true;
            }
            else
            {
                var now = Time.unscaledTime;
                if (!IsFinite(now) || now < 0f)
                {
                    return false;
                }
                if (now - _lastUdpUnavailableWarningTime >= 1f)
                {
                    Debug.LogWarning("[ClientNetwork] Not connected to UDP server. Input not sent.");
                    _lastUdpUnavailableWarningTime = now;
                }

                return false;
            }
        }

        public void SendShootRequest(Vector2 position, Vector2 direction, string weaponType)
        {
            if (!IsFinite(position) || !IsFinite(direction))
            {
                Debug.LogWarning("[ClientNetwork] Ignoring shoot request with non-finite position or direction.");
                return;
            }

            SendUdpInput(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ShootRequest,
                ["PlayerID"] = ClientPlayerId,
                ["PosX"] = position.x,
                ["PosY"] = position.y,
                ["DirX"] = direction.x,
                ["DirY"] = direction.y,
                ["WeaponType"] = string.IsNullOrWhiteSpace(weaponType) ? "Unknown" : weaponType
            }, DeliveryMethod.Unreliable);
        }

        public void SendGrenadeThrow(Vector2 position, Vector2 direction, string grenadeType)
        {
            if (!IsFinite(position) || !IsFinite(direction))
            {
                Debug.LogWarning("[ClientNetwork] Ignoring grenade request with non-finite position or direction.");
                return;
            }

            SendUdpInput(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.GrenadeThrow,
                ["PlayerID"] = ClientPlayerId,
                ["PosX"] = position.x,
                ["PosY"] = position.y,
                ["DirX"] = direction.x,
                ["DirY"] = direction.y,
                ["GrenadeType"] = string.IsNullOrWhiteSpace(grenadeType) ? "Normal" : grenadeType
            }, DeliveryMethod.Unreliable);
        }

        #endregion

        public void DisconnectAll()
        {
            _isShuttingDown = true;
            StopAllCoroutines();
            if (_tcpReconnectRoutine != null)
            {
                _tcpReconnectRoutine = null;
            }
            if (_matchConnectRoutine != null)
            {
                _matchConnectRoutine = null;
            }
            _matchUdpConnectAttempted = false;
            _tcpSessionVersion++;
            _requestClient?.FailPendingRequests("Lobby TCP session was disconnected.");
            CurrentMatchRoomId = string.Empty;
            LastDailyListResponse = null;
            LastDailyClaimResponse = null;
            LastGuildListResponse = null;
            LastGuildInfoResponse = null;
            LastGuildRoleResponse = null;
            OnlineMetaStateStore.Instance?.Clear();
            _serverPeer = null;
            InvokeSafely(MatchUdpConnectionChanged, false, "Client shutdown", nameof(MatchUdpConnectionChanged));
            _netClient?.Stop();
            _udpClientStarted = false;
            _tcpStream?.Dispose();
            _tcpStream = null;
            _tcpClient?.Close();
            _tcpClient?.Dispose();
            _tcpClient = null;
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
