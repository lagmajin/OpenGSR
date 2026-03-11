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
        
        // 現在のルーム情報
        private string currentRoomId = "";
        private string currentRoomName = "";
        private bool isReady = false;

        // プレイヤー一覧（ロビー/ウェイトルーム用）
        private JArray currentPlayers = new JArray();

        // Rx Subjects for UI updates
        private readonly Subject<JObject> onPlayerJoined = new Subject<JObject>();
        private readonly Subject<JObject> onPlayerLeft = new Subject<JObject>();
        private readonly Subject<JObject> onPlayerReady = new Subject<JObject>();
        private readonly Subject<JObject> onRoomSettingsChanged = new Subject<JObject>();
        private readonly Subject<JObject> onChatMessage = new Subject<JObject>();
        private readonly Subject<int> onStartCountdown = new Subject<int>();
        private readonly Subject<string> onCancelCountdown = new Subject<string>();

        public IObservable<JObject> OnPlayerJoinedStream => onPlayerJoined.AsObservable();
        public IObservable<JObject> OnPlayerLeftStream => onPlayerLeft.AsObservable();
        public IObservable<JObject> OnPlayerReadyStream => onPlayerReady.AsObservable();
        public IObservable<JObject> OnRoomSettingsChangedStream => onRoomSettingsChanged.AsObservable();
        public IObservable<JObject> OnChatMessageStream => onChatMessage.AsObservable();
        public IObservable<int> OnStartCountdownStream => onStartCountdown.AsObservable();
        public IObservable<string> OnCancelCountdownStream => onCancelCountdown.AsObservable();

        // Start is called before the first frame update
        void Start()
        {
            networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            
            // ネットワークマネージャのイベントを購読
            if (networkManager != null)
            {
                networkManager.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(OnDataReceived)
                    .AddTo(this);
                    
                networkManager.Subscribe(this);
            }
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.UnSubscribe(this);
            }
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
            DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>().ConnectToGeneralServerSync("127.0.0.1", 50000, "test", "test");
        }

        private void SendMessage(in JObject json)
        {
            DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>().SendMessage(json);
        }


        // Update is called once per frame
        void Update()
        {

        }

        #region 送信メソッド

        /// <summary>
        /// ゲーム開始リクエストを送信
        /// </summary>
        public void SendGameStart()
        {
            var json = new JObject();
            json["MessageType"] = "GameStartRequest";
            json["PlayerAccountID"] = "";
            json["RoomId"] = currentRoomId;
            SendMessage(json);
        }

        /// <summary>
        /// 準備完了を送信
        /// </summary>
        public void SendReady()
        {
            var json = RUDPMessageBuilder.CreateWaitRoomPlayerReady("", currentRoomId);
            isReady = true;
            SendMessage(json);
        }

        /// <summary>
        /// 準備解除を送信
        /// </summary>
        public void SendUnready()
        {
            var json = RUDPMessageBuilder.CreateWaitRoomPlayerUnready("", currentRoomId);
            isReady = false;
            SendMessage(json);
        }

        /// <summary>
        /// ロビー入室を送信
        /// </summary>
        public void SendLobbyEnter(string playerId, string playerName)
        {
            var json = RUDPMessageBuilder.CreateLobbyEnter(playerId, playerName);
            SendMessage(json);
        }

        /// <summary>
        /// ロビー退室を送信
        /// </summary>
        public void SendLobbyLeave(string playerId)
        {
            var json = RUDPMessageBuilder.CreateLobbyLeave(playerId);
            SendMessage(json);
        }

        /// <summary>
        /// ロビーチャットを送信
        /// </summary>
        public void SendLobbyChat(string playerId, string playerName, string message)
        {
            var json = RUDPMessageBuilder.CreateLobbyChat(playerId, playerName, message);
            SendMessage(json);
        }

        /// <summary>
        /// ウェイトルーム入室を送信
        /// </summary>
        public void SendWaitRoomEnter(string playerId, string playerName, string roomId)
        {
            var json = RUDPMessageBuilder.CreateWaitRoomEnter(playerId, playerName, roomId);
            currentRoomId = roomId;
            SendMessage(json);
        }

        /// <summary>
        /// ウェイトルーム退室を送信
        /// </summary>
        public void SendWaitRoomLeave(string playerId)
        {
            var json = RUDPMessageBuilder.CreateWaitRoomLeave(playerId, currentRoomId);
            SendMessage(json);
        }

        /// <summary>
        /// ウェイトルームチャットを送信
        /// </summary>
        public void SendWaitRoomChat(string playerId, string playerName, string message)
        {
            var json = RUDPMessageBuilder.CreateWaitRoomChat(playerId, playerName, message, currentRoomId);
            SendMessage(json);
        }

        /// <summary>
        /// ルーム設定変更を送信
        /// </summary>
        public void SendWaitRoomSettingsChange(JObject settings)
        {
            var json = RUDPMessageBuilder.CreateWaitRoomSettingsChange(currentRoomId, settings);
            SendMessage(json);
        }

        /// <summary>
        /// プレイヤーキックを送信（オーナーのみ）
        /// </summary>
        public void SendWaitRoomKickPlayer(string targetPlayerId, string reason)
        {
            var json = RUDPMessageBuilder.CreateWaitRoomKickPlayer(targetPlayerId, currentRoomId, reason);
            SendMessage(json);
        }

        /// <summary>
        /// ルーム作成リクエストを送信
        /// </summary>
        public void SendCreateRoomRequest(string roomName, int capacity, string gameMode, bool teamBalance, string password = "")
        {
            var json = new JObject();
            json["MessageType"] = "CreateNewWaitRoomRequest";
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
            var json = new JObject();
            json["MessageType"] = "SendEnterRoom";
            json["RoomId"] = roomId;
            json["PlayerId"] = playerId;
            json["Password"] = password;
            SendMessage(json);
        }

        /// <summary>
        /// ルーム一覧リクエストを送信
        /// </summary>
        public void SendRoomListRequest()
        {
            var json = new JObject();
            json["MessageType"] = "UpdateRoomRequest";
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
            
            var messageType = json["MessageType"]?.ToString();
            if (string.IsNullOrEmpty(messageType))
            {
                Debug.LogWarning("Received message without MessageType");
                return;
            }

            Debug.Log($"[WaitRoomNetworkManager] Received: {messageType}");

            switch (messageType)
            {
                // ロビー関連
                case RUDPMessageTypes.LobbyEnter:
                    HandleLobbyEnter(json);
                    break;
                case RUDPMessageTypes.LobbyLeave:
                    HandleLobbyLeave(json);
                    break;
                case RUDPMessageTypes.LobbyPlayerList:
                    HandleLobbyPlayerList(json);
                    break;
                case RUDPMessageTypes.LobbyChat:
                    HandleLobbyChat(json);
                    break;

                // ウェイトルーム関連
                case RUDPMessageTypes.WaitRoomEnter:
                    HandleWaitRoomEnter(json);
                    break;
                case RUDPMessageTypes.WaitRoomLeave:
                    HandleWaitRoomLeave(json);
                    break;
                case RUDPMessageTypes.WaitRoomPlayerList:
                    HandleWaitRoomPlayerList(json);
                    break;
                case RUDPMessageTypes.WaitRoomChat:
                    HandleWaitRoomChat(json);
                    break;
                case RUDPMessageTypes.WaitRoomPlayerReady:
                    HandleWaitRoomPlayerReady(json);
                    break;
                case RUDPMessageTypes.WaitRoomPlayerUnready:
                    HandleWaitRoomPlayerUnready(json);
                    break;
                case RUDPMessageTypes.WaitRoomSettingsChange:
                    HandleWaitRoomSettingsChange(json);
                    break;
                case RUDPMessageTypes.WaitRoomKickPlayer:
                    HandleWaitRoomKickPlayer(json);
                    break;
                case RUDPMessageTypes.WaitRoomOwnerChange:
                    HandleWaitRoomOwnerChange(json);
                    break;
                case RUDPMessageTypes.WaitRoomStartCountdown:
                    HandleWaitRoomStartCountdown(json);
                    break;
                case RUDPMessageTypes.WaitRoomCancelCountdown:
                    HandleWaitRoomCancelCountdown(json);
                    break;

                // ルームリスト関連
                case RUDPMessageTypes.RoomListUpdate:
                    HandleRoomListUpdate(json);
                    break;
                case RUDPMessageTypes.RoomCreated:
                    HandleRoomCreated(json);
                    break;
                case RUDPMessageTypes.RoomDeleted:
                    HandleRoomDeleted(json);
                    break;
                case RUDPMessageTypes.RoomFull:
                    HandleRoomFull(json);
                    break;

                default:
                    Debug.Log($"[WaitRoomNetworkManager] Unhandled message type: {messageType}");
                    break;
            }
        }

        #region ロビーハンドラー

        private void HandleLobbyEnter(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            var playerName = json["PlayerName"]?.ToString();
            
            PrettyLogger.Bold("Lobby", $"Player entered: {playerName} ({playerId})");
            onPlayerJoined.OnNext(json);
        }

        private void HandleLobbyLeave(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            
            PrettyLogger.Bold("Lobby", $"Player left: {playerId}");
            onPlayerLeft.OnNext(json);
        }

        private void HandleLobbyPlayerList(JObject json)
        {
            var players = json["Players"] as JArray;
            if (players != null)
            {
                currentPlayers = players;
                PrettyLogger.Bold("Lobby", $"Player list updated: {players.Count} players");
            }
        }

        private void HandleLobbyChat(JObject json)
        {
            var playerName = json["PlayerName"]?.ToString();
            var message = json["Message"]?.ToString();
            
            PrettyLogger.Bold("Lobby", $"[Chat] {playerName}: {message}");
            onChatMessage.OnNext(json);
        }

        #endregion

        #region ウェイトルームハンドラー

        private void HandleWaitRoomEnter(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            var playerName = json["PlayerName"]?.ToString();
            var roomId = json["RoomId"]?.ToString();
            
            if (!string.IsNullOrEmpty(roomId))
            {
                currentRoomId = roomId;
            }
            
            PrettyLogger.Bold("WaitRoom", $"Player entered: {playerName} ({playerId}) in room {roomId}");
            onPlayerJoined.OnNext(json);
        }

        private void HandleWaitRoomLeave(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            var roomId = json["RoomId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"Player left: {playerId} from room {roomId}");
            onPlayerLeft.OnNext(json);
        }

        private void HandleWaitRoomPlayerList(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            var players = json["Players"] as JArray;
            
            if (players != null)
            {
                currentPlayers = players;
                PrettyLogger.Bold("WaitRoom", $"Player list updated: {players.Count} players in room {roomId}");
            }
        }

        private void HandleWaitRoomChat(JObject json)
        {
            var playerName = json["PlayerName"]?.ToString();
            var message = json["Message"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"[Chat] {playerName}: {message}");
            onChatMessage.OnNext(json);
        }

        private void HandleWaitRoomPlayerReady(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"Player ready: {playerId}");
            onPlayerReady.OnNext(json);
        }

        private void HandleWaitRoomPlayerUnready(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"Player unready: {playerId}");
            onPlayerReady.OnNext(json);
        }

        private void HandleWaitRoomSettingsChange(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            var settings = json["Settings"] as JObject;
            
            PrettyLogger.Bold("WaitRoom", $"Settings changed for room {roomId}");
            onRoomSettingsChanged.OnNext(json);
        }

        private void HandleWaitRoomKickPlayer(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString();
            var reason = json["Reason"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"Player kicked: {playerId}, reason: {reason}");
            
            // 自分がキックされた場合
            // TODO: 自分のプレイヤーIDと比較してロビーに戻す処理
        }

        private void HandleWaitRoomOwnerChange(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            var newOwnerId = json["NewOwnerId"]?.ToString();
            
            PrettyLogger.Bold("WaitRoom", $"Owner changed in room {roomId} to {newOwnerId}");
            onRoomSettingsChanged.OnNext(json);
        }

        private void HandleWaitRoomStartCountdown(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            var countdown = json["Countdown"]?.ToObject<int>() ?? 0;
            
            PrettyLogger.Bold("WaitRoom", $"Game starting in {countdown} seconds");
            onStartCountdown.OnNext(countdown);
        }

        private void HandleWaitRoomCancelCountdown(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            var reason = json["Reason"]?.ToString() ?? "Unknown";
            
            PrettyLogger.Bold("WaitRoom", $"Countdown cancelled: {reason}");
            onCancelCountdown.OnNext(reason);
        }

        #endregion

        #region ルームリストハンドラー

        private void HandleRoomListUpdate(JObject json)
        {
            var rooms = json["Rooms"] as JArray;
            var roomCount = rooms?.Count ?? 0;
            
            PrettyLogger.Bold("RoomList", $"Room list updated: {roomCount} rooms");
            
            // TODO: UIのルームリストを更新
        }

        private void HandleRoomCreated(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            var roomName = json["RoomName"]?.ToString();
            var ownerId = json["OwnerId"]?.ToString();
            
            PrettyLogger.Bold("RoomList", $"Room created: {roomName} ({roomId}) by {ownerId}");
        }

        private void HandleRoomDeleted(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            
            PrettyLogger.Bold("RoomList", $"Room deleted: {roomId}");
        }

        private void HandleRoomFull(JObject json)
        {
            var roomId = json["RoomId"]?.ToString();
            
            PrettyLogger.Bold("RoomList", $"Room full: {roomId}");
        }

        #endregion

        #region フィールドアイテムハンドラー

        private void HandleItemSpawnNotification(JObject json)
        {
            var typeStr = json["ItemType"]?.ToString();
            var pointId = json["SpawnPointId"]?.Value<int>() ?? 0;

            if (Enum.TryParse<EFieldItemType>(typeStr, out var type))
            {
                if (ItemSpawnPoint.AllSpawnPoints.TryGetValue(pointId, out var point))
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
                foreach (var point in ItemSpawnPoint.AllSpawnPoints.Values)
                {
                    point.DespawnItem();
                }
            }
            else if (ItemSpawnPoint.AllSpawnPoints.TryGetValue(pointId, out var point))
            {
                point.DespawnItem();
            }
        }

        #endregion

        // Implement missing INetworkManagerScript members
        public void TestFunc()
        {
            // placeholder for compatibility with INetworkManagerScript
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
        }

        public void ParseMessageFromMatchServer(JObject json)
        {

        }


    }

}
