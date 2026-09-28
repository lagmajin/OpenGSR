using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using System;
using UniRx;
using UnityEngine;
using OpenGSCore;


namespace OpenGS
{

    interface IWaitRoomNetworkManager
    {

    }



    [DisallowMultipleComponent]
    public class WaitRoomNetworkManager : MonoBehaviour, INetworkManagerScript
    {

        [Required]
        public OnlineWaitRoomScene waitroom;

        [Required] [SerializeField] private WaitRoomMediateObject mediateObject;
        
        private GeneralServerNetworkManager networkManager;
        private WaitRoomManager waitRoomManager;
        
        // 現在のルーム情報
        private string currentRoomId = "";
        private string currentRoomName = "";
        private bool isReady = false;
        [SerializeField] private float chatCooldownSeconds = 0.5f;
        private float nextChatSendTime;
        private float nextGameStartSendTime;

        // プレイヤー一覧（ロビー/ウェイトルーム用）
        private JArray currentPlayers = new JArray();
        private JArray currentRooms = new JArray();

        // Rx Subjects for UI updates
        private readonly Subject<JObject> onPlayerJoined = new Subject<JObject>();
        private readonly Subject<JObject> onPlayerLeft = new Subject<JObject>();
        private readonly Subject<JObject> onPlayerReady = new Subject<JObject>();
        private readonly Subject<JArray> onPlayerList = new Subject<JArray>();
        private readonly Subject<JArray> onRoomList = new Subject<JArray>();
        private readonly Subject<JObject> onRoomSettingsChanged = new Subject<JObject>();
        private readonly Subject<JObject> onChatMessage = new Subject<JObject>();
        private readonly Subject<int> onStartCountdown = new Subject<int>();
        private readonly Subject<string> onCancelCountdown = new Subject<string>();
        private readonly Subject<JObject> onRoomDeleted = new Subject<JObject>();
        private readonly Subject<JObject> onRoomNotFound = new Subject<JObject>();
        private readonly Subject<JObject> onSelfKicked = new Subject<JObject>();

        public IObservable<JObject> OnPlayerJoinedStream => onPlayerJoined.AsObservable();
        public IObservable<JObject> OnPlayerLeftStream => onPlayerLeft.AsObservable();
        public IObservable<JObject> OnPlayerReadyStream => onPlayerReady.AsObservable();
        public IObservable<JArray> OnPlayerListStream => onPlayerList.AsObservable();
        public IObservable<JArray> OnRoomListStream => onRoomList.AsObservable();
        public IObservable<JObject> OnRoomSettingsChangedStream => onRoomSettingsChanged.AsObservable();
        public IObservable<JObject> OnChatMessageStream => onChatMessage.AsObservable();
        public IObservable<int> OnStartCountdownStream => onStartCountdown.AsObservable();
        public IObservable<string> OnCancelCountdownStream => onCancelCountdown.AsObservable();
        public IObservable<JObject> OnRoomDeletedStream => onRoomDeleted.AsObservable();
        public IObservable<JObject> OnRoomNotFoundStream => onRoomNotFound.AsObservable();
        public IObservable<JObject> OnSelfKickedStream => onSelfKicked.AsObservable();

        // Start is called before the first frame update
        void Start()
        {
            try
            {
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                waitRoomManager = DependencyInjectionConfig.Resolve<WaitRoomManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WaitRoomNetworkManager] Failed to resolve dependencies: {ex.Message}");
                networkManager = null;
                waitRoomManager = null;
            }
            
            // ネットワークマネージャのイベントを購読
            if (networkManager != null)
            {
                networkManager.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(OnDataReceived)
                    .AddTo(this);
                    
                networkManager.Subscribe(this);
                EmitInitialState();
            }
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.UnSubscribe(this);
            }

            onPlayerJoined.Dispose();
            onPlayerLeft.Dispose();
            onPlayerReady.Dispose();
            onPlayerList.Dispose();
            onRoomList.Dispose();
            onRoomSettingsChanged.Dispose();
            onChatMessage.Dispose();
            onStartCountdown.Dispose();
            onCancelCountdown.Dispose();
            onRoomDeleted.Dispose();
            onRoomNotFound.Dispose();
            onSelfKicked.Dispose();
        }
        
        /// <summary>
        /// ネットワークからデータを受信した時の処理
        /// </summary>
        private void OnDataReceived(JObject json)
        {
            ParseMessageFromGeneralServer(json);
        }

        [Button("\u0083e\u0083X\u0083g\u0090\u00DA\u0091\u00B1")]
        private void DebugConnect()
        {
            if (networkManager == null)
            {
                try
                {
                    networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WaitRoomNetworkManager] Debug connection skipped: {ex.Message}");
                    return;
                }
            }

            networkManager?.ConnectToGeneralServerSync("127.0.0.1", 50000, "test", "test");
        }

        private void SendMessage(in JObject json)
        {
            if (json == null)
            {
                return;
            }

            if (networkManager == null)
            {
                try
                {
                    networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WaitRoomNetworkManager] Message send skipped: {ex.Message}");
                    return;
                }
            }

            if (networkManager == null)
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Message send skipped: network manager is unavailable.");
                return;
            }

            networkManager.SendMessage(json);
        }


        #region 送信メソッド

        /// <summary>
        /// ゲーム開始リクエストを送信
        /// </summary>
        public void SendGameStart()
        {
            if (string.IsNullOrWhiteSpace(currentRoomId))
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Game start ignored without a room.");
                return;
            }

            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f || now < nextGameStartSendTime)
            {
                return;
            }

            var json = new JObject();
            json["MessageType"] = MessageType.GameStartRequest;
            json["PlayerAccountID"] = ResolveLocalPlayerId();
            json["RoomID"] = currentRoomId;
            SendMessage(json);
            nextGameStartSendTime = now + 1f;
        }

        /// <summary>
        /// 準備完了を送信
        /// </summary>
        public void SendReady()
        {
            if (string.IsNullOrWhiteSpace(currentRoomId) || isReady)
            {
                return;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.PlayerReadyRequest,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["RoomID"] = currentRoomId
            };
            isReady = true;
            SendMessage(json);
        }

        /// <summary>
        /// 準備解除を送信
        /// </summary>
        public void SendUnready()
        {
            if (string.IsNullOrWhiteSpace(currentRoomId) || !isReady)
            {
                return;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.PlayerUnready,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["RoomID"] = currentRoomId
            };
            isReady = false;
            SendMessage(json);
        }

        /// <summary>
        /// ロビー入室を送信
        /// </summary>
        public void SendLobbyEnter(string playerId, string playerName)
        {
            var json = new JObject
            {
                ["MessageType"] = MessageType.LobbyEnter,
                ["PlayerID"] = playerId,
                ["PlayerName"] = playerName
            };
            SendMessage(json);
        }

        /// <summary>
        /// ロビー退室を送信
        /// </summary>
        public void SendLobbyLeave(string playerId)
        {
            var json = new JObject
            {
                ["MessageType"] = MessageType.LobbyLeave,
                ["PlayerID"] = playerId
            };
            SendMessage(json);
        }

        /// <summary>
        /// ロビーチャットを送信
        /// </summary>
        public void SendLobbyChat(string playerId, string playerName, string message)
        {
            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f || now < nextChatSendTime)
            {
                return;
            }

            message = NormalizeChatField(message, 200);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.LobbyChat,
                ["PlayerID"] = NormalizeChatField(playerId, 128),
                ["PlayerName"] = NormalizeChatField(playerName, 64),
                ["Message"] = message
            };
            SendMessage(json);
            nextChatSendTime = now + GetChatCooldown();
        }

        /// <summary>
        /// ウェイトルーム入室を送信
        /// </summary>
        public void SendWaitRoomEnter(string playerId, string playerName, string roomId)
        {
            playerId = playerId?.Trim();
            roomId = roomId?.Trim();
            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Enter room ignored with missing player or room ID.");
                return;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.WaitRoomEnter,
                ["PlayerID"] = playerId,
                ["PlayerName"] = playerName,
                ["RoomID"] = roomId
            };
            currentRoomId = roomId;
            SendMessage(json);
        }

        /// <summary>
        /// ウェイトルーム退室を送信
        /// </summary>
        public void SendWaitRoomLeave(string playerId)
        {
            var json = new JObject
            {
                ["MessageType"] = MessageType.WaitRoomLeave,
                ["PlayerID"] = playerId,
                ["RoomID"] = currentRoomId
            };
            SendMessage(json);
        }

        /// <summary>
        /// ウェイトルームチャットを送信
        /// </summary>
        public void SendWaitRoomChat(string playerId, string playerName, string message)
        {
            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f || now < nextChatSendTime)
            {
                return;
            }

            message = NormalizeChatField(message, 200);
            if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(currentRoomId))
            {
                return;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.WaitRoomChat,
                ["PlayerID"] = NormalizeChatField(playerId, 128),
                ["PlayerName"] = NormalizeChatField(playerName, 64),
                ["Message"] = message,
                ["RoomID"] = NormalizeChatField(currentRoomId, 128)
            };
            SendMessage(json);
            nextChatSendTime = now + GetChatCooldown();
        }

        private static string NormalizeChatField(string value, int maxLength)
        {
            var normalized = value?.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim()
                ?? string.Empty;
            return normalized.Length > maxLength ? normalized.Substring(0, maxLength) : normalized;
        }

        private float GetChatCooldown()
        {
            return float.IsFinite(chatCooldownSeconds) ? Mathf.Max(0f, chatCooldownSeconds) : 0.5f;
        }

        /// <summary>
        /// ルーム設定変更を送信
        /// </summary>
        public void SendWaitRoomSettingsChange(JObject settings)
        {
            if (string.IsNullOrWhiteSpace(currentRoomId) || settings == null)
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Room settings change ignored without room or settings.");
                return;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.WaitRoomSettingsChange,
                ["RoomID"] = currentRoomId,
                ["Settings"] = settings
            };
            SendMessage(json);
        }

        /// <summary>
        /// プレイヤーキックを送信（オーナーのみ）
        /// </summary>
        public void SendWaitRoomKickPlayer(string targetPlayerId, string reason)
        {
            targetPlayerId = targetPlayerId?.Trim();
            reason = NormalizeChatField(reason, 200);
            if (string.IsNullOrWhiteSpace(targetPlayerId) || string.IsNullOrWhiteSpace(currentRoomId))
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Kick ignored with missing target or room ID.");
                return;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.WaitRoomKickPlayer,
                ["PlayerID"] = targetPlayerId,
                ["RoomID"] = currentRoomId,
                ["Reason"] = reason
            };
            SendMessage(json);
        }

        /// <summary>
        /// ルーム作成リクエストを送信
        /// </summary>
        public void SendCreateRoomRequest(string roomName, int capacity, string gameMode, bool teamBalance, string password = "")
        {
            roomName = roomName?.Trim();
            gameMode = gameMode?.Trim();
            password = password?.Trim();
            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(gameMode))
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Room creation ignored with missing name or game mode.");
                return;
            }

            if (roomName.Length > 64)
            {
                roomName = roomName.Substring(0, 64);
            }

            capacity = Mathf.Clamp(capacity, 2, 32);
            if (password.Length > 64)
            {
                password = password.Substring(0, 64);
            }

            var json = new JObject();
            json["MessageType"] = MessageType.CreateRoomRequest;
            json["OwnerPlayerID"] = "";
            json["RoomName"] = roomName;
            json["Capacity"] = capacity.ToString();
            json["GameMode"] = gameMode;
            json["TeamBalance"] = teamBalance.ToString().ToLower();
            json["Password"] = password;
            SendMessage(json);
        }

        /// <summary>
        /// ルーム参加リクエストを送信
        /// </summary>
        public void SendEnterRoomRequest(string roomId, string playerId, string password = "")
        {
            roomId = roomId?.Trim();
            playerId = playerId?.Trim();
            password = password?.Trim();
            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(playerId))
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Join room ignored with missing player or room ID.");
                return;
            }

            if (password.Length > 64)
            {
                password = password.Substring(0, 64);
            }

            var json = new JObject();
            json["MessageType"] = MessageType.JoinRoomRequest;
            json["RoomID"] = roomId;
            json["PlayerID"] = playerId;
            json["Password"] = password;
            SendMessage(json);
        }

        /// <summary>
        /// ルーム一覧リクエストを送信
        /// </summary>
        public void SendRoomListRequest()
        {
            var json = new JObject();
            json["MessageType"] = MessageType.RoomListUpdateRequest;
            json["MatchRoomType"] = "All";
            json["Options"] = "";
            SendMessage(json);
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// 現在のルームIDを取得
        /// </summary>
        public string CurrentRoomId => currentRoomId;

        /// <summary>
        /// 現在のルーム名を取得
        /// </summary>
        public string CurrentRoomName => currentRoomName;

        /// <summary>
        /// 準備完了状態を取得
        /// </summary>
        public bool IsReady => isReady;

        /// <summary>
        /// 現在のプレイヤー一覧を取得
        /// </summary>
        public JArray CurrentPlayers => currentPlayers;

        #endregion

        public void ParseMessageFromGeneralServer(JObject json)
        {
            if (json == null) return;
            
            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (string.IsNullOrEmpty(messageType))
            {
                Debug.LogWarning("Received message without MessageType");
                return;
            }

            Debug.Log($"[WaitRoomNetworkManager] Received: {messageType}");

            switch (messageType)
            {
                // ロビー関連
                // ウェイトルーム関連
                case MessageType.JoinRoomRequest:
                    HandleWaitRoomEnter(json);
                    break;
                case MessageType.LeaveRoomRequest:
                case MessageType.LeaveRoomResponse:
                    HandleWaitRoomLeave(json);
                    break;
                case MessageType.WaitRoomPlayerList:
                    HandleWaitRoomPlayerList(json);
                    break;
                case MessageType.LobbyChatRequest:
                case MessageType.LobbyChatNotification:
                    HandleWaitRoomChat(json);
                    break;
                case MessageType.WaitRoomPlayerReady:
                    HandleWaitRoomPlayerReady(json);
                    break;
                case MessageType.PlayerReadyNotification:
                    HandleWaitRoomPlayerReady(json);
                    break;
                case MessageType.WaitRoomPlayerUnready:
                    HandleWaitRoomPlayerUnready(json);
                    break;
                case MessageType.WaitRoomSettingsChange:
                    HandleWaitRoomSettingsChange(json);
                    break;
                case MessageType.WaitRoomKickPlayer:
                    HandleWaitRoomKickPlayer(json);
                    break;
                case MessageType.LoadingStartedNotification:
                    waitroom?.LoadGameSceneFromNetwork();
                    break;
                case MessageType.WaitRoomOwnerChange:
                    HandleWaitRoomOwnerChange(json);
                    break;
                case MessageType.WaitRoomStartCountdown:
                    HandleWaitRoomStartCountdown(json);
                    break;
                case MessageType.WaitRoomCancelCountdown:
                    HandleWaitRoomCancelCountdown(json);
                    break;

                // ルームリスト関連
                case MessageType.RoomListUpdateNotification:
                    HandleRoomListUpdate(json);
                    break;
                case MessageType.WaitRoomUpdateNotification:
                    HandleWaitRoomUpdate(json);
                    break;
                case MessageType.RoomCreated:
                    HandleRoomCreated(json);
                    break;
                case MessageType.RoomDeleted:
                    HandleRoomDeleted(json);
                    break;
                case MessageType.RoomFull:
                    HandleRoomFull(json);
                    break;
                case MessageType.RoomNotFound:
                    HandleRoomNotFound(json);
                    break;
                // The server's answer when it will not do what was asked: a match
                // that cannot start because the room is gone or the players are
                // not ready. It had no case anywhere, so the answer was dropped on
                // arrival and a player who pressed start was told nothing at all.
                case MessageType.ErrorNotification:
                    HandleErrorNotification(json);
                    break;
                case MessageType.RoomSettingChanged:
                    HandleRoomSettingChanged(json);
                    break;

                default:
                    Debug.Log($"[WaitRoomNetworkManager] Unhandled message type: {messageType}");
                    break;
            }
        }

        #region ウェイトルームハンドラー

        private void HandleWaitRoomEnter(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            var playerName = json["PlayerName"]?.ToString();
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var roomName = json["RoomName"]?.ToString();
            
            if (!string.IsNullOrEmpty(roomId))
            {
                currentRoomId = roomId;
            }
            if (!string.IsNullOrWhiteSpace(roomName))
            {
                currentRoomName = roomName;
            }
            
            PrettyLogger.Bold("WaitRoom", $"入室: {playerName} ({playerId}) -> room={roomId}");
            onPlayerJoined.OnNext(json);
        }

        private void HandleWaitRoomLeave(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"退室: {playerId} <- room={roomId}");
            onPlayerLeft.OnNext(json);
        }

        private void HandleWaitRoomPlayerList(JObject json)
        {
            var snapshot = OpenGSCore.WaitRoomSnapshot.FromJson(json);
            var roomId = snapshot.RoomId;
            var roomName = snapshot.RoomName;
            var players = snapshot.Players;
            
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                currentRoomId = roomId;
            }
            if (!string.IsNullOrWhiteSpace(roomName))
            {
                currentRoomName = roomName;
            }

            if (players != null)
            {
                currentPlayers = json["Players"] as JArray ?? new JArray();
                if (waitRoomManager?.WaitRoom != null)
                {
                    waitRoomManager.WaitRoom.RoomName = currentRoomName;
                    waitRoomManager.WaitRoom.PlayerCount = Mathf.Max(1, players.Count);
                    if (!string.IsNullOrWhiteSpace(snapshot.Map) && Enum.TryParse(snapshot.Map, true, out EMap map))
                    {
                        waitRoomManager.WaitRoom.Map = map;
                    }
                    waitRoomManager.WaitRoom.PlayerList.Clear();
                    foreach (var playerInfo in players)
                    {
                        if (playerInfo == null)
                        {
                            continue;
                        }

                        waitRoomManager.WaitRoom.AddNewPlayer(playerInfo);

                        if (string.Equals(playerInfo.Id, ResolveLocalPlayerId(), StringComparison.OrdinalIgnoreCase))
                        {
                            isReady = playerInfo.IsReady;
                        }

                        if (string.Equals(playerInfo.Id, snapshot.OwnerId, StringComparison.OrdinalIgnoreCase))
                        {
                            waitRoomManager.WaitRoom.OwnerId = playerInfo.Id;
                        }
                    }
                }

                PrettyLogger.Bold("WaitRoom", $"参加者更新: {players.Count}人 room={roomId}");
                onPlayerList.OnNext(currentPlayers);
            }
        }

        private void HandleWaitRoomChat(JObject json)
        {
            var playerName = json["PlayerName"]?.ToString();
            var message = json["Message"]?.ToString();
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"チャット[{roomId}] {playerName}: {message}");
            onChatMessage.OnNext(json);
        }

        private void HandleWaitRoomPlayerReady(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"準備完了: {playerId}");
            onPlayerReady.OnNext(json);
        }

        private void HandleWaitRoomPlayerUnready(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"準備解除: {playerId}");
            onPlayerReady.OnNext(json);
        }

        private void HandleWaitRoomSettingsChange(JObject json)
        {
            var roomInfo = RoomInfoSnapshot.FromJson(json);
            var roomId = roomInfo.RoomId;
            var roomName = roomInfo.RoomName;
            var settings = json["Settings"] as JObject;

            if (!string.IsNullOrWhiteSpace(roomId))
            {
                currentRoomId = roomId;
            }
            if (!string.IsNullOrWhiteSpace(roomName))
            {
                currentRoomName = roomName;
            }

            if (waitRoomManager?.WaitRoom != null && settings != null)
            {
                waitRoomManager.WaitRoom.RoomName = currentRoomName;
                if (!string.IsNullOrWhiteSpace(roomInfo.GameMode) && Enum.TryParse(roomInfo.GameMode, out EGameMode roomGameMode))
                {
                    waitRoomManager.WaitRoom.GameMode = roomGameMode;
                }

                if (!string.IsNullOrWhiteSpace(roomInfo.Map) && Enum.TryParse(roomInfo.Map, out EMap map))
                {
                    waitRoomManager.WaitRoom.Map = map;
                }

                if (settings["PlayerCharacter"] != null)
                {
                    var character = ParsePlayerCharacter(settings["PlayerCharacter"]?.ToString());
                    var localPlayer = waitRoomManager.WaitRoom.PlayerList.Find(player =>
                        player != null && string.Equals(player.Id, ResolveLocalPlayerId(), StringComparison.OrdinalIgnoreCase));
                    if (localPlayer != null)
                    {
                        localPlayer.playerCharacter = character;
                    }
                }

                if (roomInfo.Capacity > 0)
                {
                    waitRoomManager.WaitRoom.Capacity = roomInfo.Capacity;
                }

                waitRoomManager.WaitRoom.TeamBalance = roomInfo.TeamBalance;

                if (roomInfo.PlayerCount > 0)
                {
                    waitRoomManager.WaitRoom.PlayerCount = roomInfo.PlayerCount;
                }
            }
            
            PrettyLogger.Bold("WaitRoom", $"ルーム設定更新: room={roomId}");
            onRoomSettingsChanged.OnNext(json);
        }

        private void HandleWaitRoomKickPlayer(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            var reason = json["Reason"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"キック通知: {playerId}, reason={reason}");
            
            if (!string.IsNullOrWhiteSpace(playerId) &&
                string.Equals(playerId, ResolveLocalPlayerId(), StringComparison.OrdinalIgnoreCase))
            {
                ClearCurrentRoomState();
                onSelfKicked.OnNext(json);
            }
        }

        private void HandleWaitRoomOwnerChange(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var newOwnerId = json["NewOwnerId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"オーナー変更: room={roomId} -> {newOwnerId}");
            onRoomSettingsChanged.OnNext(json);
        }

        private void HandleWaitRoomStartCountdown(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var countdown = json["Countdown"]?.ToObject<int>() ?? 0;
            countdown = Mathf.Clamp(countdown, 0, 300);
            
            PrettyLogger.Bold("WaitRoom", $"ゲーム開始カウントダウン: {countdown}s");
            onStartCountdown.OnNext(countdown);
        }

        private void HandleWaitRoomCancelCountdown(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var reason = json["Reason"]?.ToString() ?? "Unknown";
            
            PrettyLogger.Bold("WaitRoom", $"カウントダウン中止: {reason}");
            onCancelCountdown.OnNext(reason);
        }

        #endregion

        #region ルームリストハンドラー

        private void HandleRoomListUpdate(JObject json)
        {
            var rooms = json["Rooms"] as JArray;
            var roomCount = rooms?.Count ?? 0;
            currentRooms = rooms ?? new JArray();
            
            PrettyLogger.Bold("RoomList", $"ルーム一覧更新: {roomCount}件");
            onRoomList.OnNext(currentRooms);
        }

        private void HandleWaitRoomUpdate(JObject json)
        {
            if (json["RoomInfo"] is not JObject roomInfo)
            {
                Debug.LogWarning("[WaitRoomNetworkManager] WaitRoomUpdateNotification did not include RoomInfo");
                return;
            }

            HandleWaitRoomPlayerList(roomInfo);
        }

        private void HandleRoomCreated(JObject json)
        {
            var roomInfo = RoomInfoSnapshot.FromJson(json);
            var roomId = roomInfo.RoomId;
            var roomName = roomInfo.RoomName;
            var ownerId = roomInfo.OwnerId;

            if (!string.IsNullOrWhiteSpace(roomId))
            {
                currentRoomId = roomId;
            }
            if (!string.IsNullOrWhiteSpace(roomName))
            {
                currentRoomName = roomName;
            }

            if (waitRoomManager?.WaitRoom != null && !string.IsNullOrWhiteSpace(roomInfo.Map) && Enum.TryParse(roomInfo.Map, true, out EMap map))
            {
                waitRoomManager.WaitRoom.Map = map;
            }
            
            PrettyLogger.Bold("RoomList", $"ルーム作成: {roomName} ({roomId}) owner={ownerId}");
        }

        private void HandleRoomDeleted(JObject json)
        {
            var roomInfo = RoomInfoSnapshot.FromJson(json);
            var roomId = roomInfo.RoomId;
            
            PrettyLogger.Bold("RoomList", $"ルーム削除: {roomId}");
            if (string.IsNullOrWhiteSpace(roomId) || string.Equals(roomId, currentRoomId, StringComparison.OrdinalIgnoreCase))
            {
                ClearCurrentRoomState();
            }
            onRoomDeleted.OnNext(json);
        }

        private void HandleRoomFull(JObject json)
        {
            var roomInfo = RoomInfoSnapshot.FromJson(json);
            var roomId = roomInfo.RoomId;
            
            PrettyLogger.Bold("RoomList", $"満室ルーム: {roomId}");
            ServerRefusalNotice.Show("そのルームは満室です。");
        }

        /// <summary>
        /// The server declining to do something.
        /// <para>
        /// The wording is the server's rather than one written here, because the
        /// server is the side that knows why: it knows which room it could not
        /// find and which players were not ready, and a client guessing would be
        /// guessing about the server's own state.
        /// </para>
        /// <para>
        /// This records it in the same place the other refusals are recorded. It
        /// is not yet on the screen: the refusals beside it are not either, and
        /// giving this one a panel on its own would leave the others looking
        /// deliberate. They need one place between them.
        /// </para>
        /// </summary>
        private void HandleErrorNotification(JObject json)
        {
            var reason = json["ErrorMessage"]?.ToString() ?? "The server declined that request.";
            var roomId = json["RoomID"]?.ToString() ?? string.Empty;

            PrettyLogger.Bold("WaitRoom", $"サーバーエラー: {reason}{(string.IsNullOrEmpty(roomId) ? "" : $" (room {roomId})")}");
            ServerRefusalNotice.Show(reason);
        }

        private void HandleRoomNotFound(JObject json)
        {
            var roomInfo = RoomInfoSnapshot.FromJson(json);
            var roomId = roomInfo.RoomId;

            PrettyLogger.Bold("RoomList", $"ルーム未検出: {roomId}");
            ServerRefusalNotice.Show("そのルームは見つかりませんでした。");
            if (string.IsNullOrWhiteSpace(roomId) || string.Equals(roomId, currentRoomId, StringComparison.OrdinalIgnoreCase))
            {
                ClearCurrentRoomState();
            }
            onRoomNotFound.OnNext(json);
        }

        private void HandleRoomSettingChanged(JObject json)
        {
            var roomSnapshot = RoomInfoSnapshot.FromJson(json);
            if (!string.IsNullOrWhiteSpace(roomSnapshot.RoomId))
            {
                currentRoomId = roomSnapshot.RoomId;
                currentRoomName = roomSnapshot.RoomName ?? currentRoomName;

                if (waitRoomManager?.WaitRoom != null)
                {
                    waitRoomManager.WaitRoom.RoomName = currentRoomName;
                    waitRoomManager.WaitRoom.Capacity = roomSnapshot.Capacity > 0 ? roomSnapshot.Capacity : waitRoomManager.WaitRoom.Capacity;
                    waitRoomManager.WaitRoom.TeamBalance = roomSnapshot.TeamBalance;

                    if (!string.IsNullOrWhiteSpace(roomSnapshot.GameMode) && Enum.TryParse(roomSnapshot.GameMode, true, out EGameMode gameMode))
                    {
                        waitRoomManager.WaitRoom.GameMode = gameMode;
                    }

                    if (!string.IsNullOrWhiteSpace(roomSnapshot.Map) && Enum.TryParse(roomSnapshot.Map, true, out EMap map))
                    {
                        waitRoomManager.WaitRoom.Map = map;
                    }
                }
            }

            PrettyLogger.Bold("WaitRoom", $"ルーム設定適用: room={currentRoomId}");
            onRoomSettingsChanged.OnNext(json);
        }

        #endregion

        #region フィールドアイテムハンドラー

        private void HandleItemSpawnNotification(JObject json)
        {
            var typeStr = json["ItemType"]?.ToString();
            var pointId = json["SpawnPointId"]?.Value<int>() ?? 0;

            if (Enum.TryParse<EFieldItemType>(typeStr, out var type))
            {
                if (ItemSpawnPoints.TryGetPoint(pointId, out var point))
                {
                    point.SpawnItem(type);
                    PrettyLogger.Bold("Item", $"Item spawned: {type} at point {pointId}");
                }
            }
        }

        private void HandleItemDespawnNotification(JObject json)
        {
            var pointId = json["SpawnPointId"]?.Value<int>() ?? -1;

            if (pointId == -1)
            {
                // 全てのポイントのアイテムを消去
                foreach (var point in ItemSpawnPoints.AllSpawnPoints)
                {
                    point?.DespawnItem();
                }
            }
            else if (ItemSpawnPoints.TryGetPoint(pointId, out var point))
            {
                point.DespawnItem();
            }
        }

        #endregion

        // Implement missing INetworkManagerScript members
        public void TestFunc()
        {
            Debug.Log($"[WaitRoomNetworkManager] TestFunc: room={currentRoomId}, players={currentPlayers.Count}, ready={isReady}");
        }

        public void ParseNetworkMatchMessageFromServer(JObject json)
        {
            // Delegate to existing match server parser (if logic needed, extend ParseMessageFromMatchServer)
            ParseMessageFromMatchServer(json);
        }

        public void OnConnected()
        {
            Debug.Log("[WaitRoomNetworkManager] Connected to general server");
        }

        public void OnDisconnected()
        {
            Debug.Log("[WaitRoomNetworkManager] Disconnected from general server");
            ClearCurrentRoomState();
            onPlayerList.OnNext(new JArray());
            onRoomList.OnNext(new JArray());
        }

        public void ParseMessageFromMatchServer(JObject json)
        {
            if (json == null)
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Received null match-server message.");
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (string.IsNullOrWhiteSpace(messageType))
            {
                Debug.LogWarning("[WaitRoomNetworkManager] Match-server message has no MessageType.");
                return;
            }

            // Match server and general server use the same notification envelope for
            // room updates. Keep one source of truth for those handlers.
            if (messageType == MessageType.GameStartNotification)
            {
                Debug.Log("[WaitRoomNetworkManager] Match start notification received.");
                waitroom?.LoadGameSceneFromNetwork();
                return;
            }

            ParseMessageFromGeneralServer(json);
        }

        private void EmitInitialState()
        {
            if (networkManager == null)
            {
                return;
            }

            var snapshot = networkManager.GetCurrentWaitRoomPlayerListSnapshot();
            if (snapshot != null)
            {
                HandleWaitRoomPlayerList(snapshot);
            }
        }

        public JArray CurrentRoomList => currentRooms;

        private static string ResolveLocalPlayerId()
        {
            var playerId = AccountManager.Instance?.CurrentProfile?.GlobalUserId;
            return string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
        }

        private static EPlayerCharacter ParsePlayerCharacter(string characterName)
        {
            if (!string.IsNullOrWhiteSpace(characterName) &&
                Enum.TryParse(characterName, true, out EPlayerCharacter parsedCharacter))
            {
                return parsedCharacter;
            }

            return GamePlayerManager.Instance != null
                ? GamePlayerManager.Instance.SelectedPlayerCharacter()
                : EPlayerCharacter.Misty;
        }

        private void ClearCurrentRoomState()
        {
            currentRoomId = "";
            currentRoomName = "";
            isReady = false;
            nextChatSendTime = 0f;
            nextGameStartSendTime = 0f;
            currentPlayers = new JArray();
            currentRooms = new JArray();

            if (waitRoomManager?.WaitRoom != null)
            {
                waitRoomManager.WaitRoom.RoomName = "";
                waitRoomManager.WaitRoom.PlayerCount = 0;
                waitRoomManager.WaitRoom.PlayerList.Clear();
                waitRoomManager.WaitRoom.OwnerId = "";
            }
        }


    }

}
