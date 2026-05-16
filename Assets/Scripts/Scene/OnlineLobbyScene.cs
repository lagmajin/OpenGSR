﻿using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using OpenGSCore;
using UniRx;

#pragma warning disable 0414

namespace OpenGS
{
    /// <summary>
    /// オンラインロビーシーンの管理クラス。
    /// ルーム一覧の表示・作成・入室などのロビー機能を担当する。
    /// </summary>
    [DisallowMultipleComponent]
    public class OnlineLobbyScene : AbstractNonBattleScene, INetworkManagerScript
    {
        // ─── 定数 ──────────────────────────────────────────────────

        private const int MaxUpdateCount = 50000;

        // ─── Inspector フィールド ───────────────────────────────────

        [SerializeField] public GameObject createNewRoomDialog;
        [SerializeField] public GameObject RoomButton;
        [SerializeField] public GameObject InfoDialog;
        [SerializeField] public GameObject robbyNetworkManager;
        [SerializeField] public GameObject roomPanel;

        [SerializeField] [Required] public LobbySceneMediateObject mediateObject;
        [SerializeField] private OnlineLobbySceneController lobbySceneController;

        // ─── 内部状態 ───────────────────────────────────────────────

        private GeneralServerNetworkManager networkManager;
        private MatchRoomManager matchRoomManager;
        private WaitRoomManager waitRoomManager;
        private SynchronizationContext mainThread;
        private bool canInput = true;
        private int updateCount = 0;
        private JArray currentRoomList = new JArray();
        private string currentRoomFilter = "All";
        private string currentSelectedRoomId = "";
        private readonly List<GameObject> spawnedRoomButtons = new List<GameObject>();

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            mainThread = SynchronizationContext.Current;
            networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            waitRoomManager = DependencyInjectionConfig.Resolve<WaitRoomManager>();
            if (lobbySceneController == null)
            {
                lobbySceneController = GetComponent<OnlineLobbySceneController>();
            }

            if (DebugFlagManager.IsDebug())
            {
                DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
                BackToConnectServerScene();
            }
        }

        void Start()
        {
            SceneManager.sceneLoaded += OnGameSceneLoaded;

            try
            {
                networkManager.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(ParseMessageFromServer)
                    .AddTo(this.gameObject);
                Debug.Log("OnlineLobbyScene: Subscribed to GeneralServerNetworkManager.DataReceivedStream");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"OnlineLobbyScene: Failed to subscribe to DataReceivedStream: {ex.Message}");
            }

            StartCoroutine(PeriodicUpdateCoroutine());
            ShowDefaultRooms();
        }

        void OnDestroy()
        {
            networkManager.UnSubscribe(this);
        }

        private void OnApplicationQuit()
        {
            networkManager.Disconnect();
        }

        void Update()
        {
            if (lobbySceneController != null)
            {
                lobbySceneController.TickInput(
                    canInput,
                    ref updateCount,
                    MaxUpdateCount,
                    () =>
                    {
                        Debug.Log("F5 pressed: Sending UpdateRoomRequest");
                        networkManager.SendUpdateRoomRequest();
                    },
                    DisconnectAndBackToTitle,
                    GoToShop);
                return;
            }

            if (!canInput) return;

            if (Input.anyKeyDown)
            {
                updateCount = 0;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                Debug.Log("F5 pressed: Sending UpdateRoomRequest");
                networkManager.SendUpdateRoomRequest();
            }

            if (Input.GetKeyDown(KeyCode.F6) || Input.GetKey(KeyCode.Escape))
            {
                DisconnectAndBackToTitle();
            }

            if (Input.GetKey(KeyCode.S))
            {
                GoToShop();
            }

            if (updateCount >= MaxUpdateCount)
            {
                DisconnectAndBackToTitle();
            }

            updateCount++;
        }

        // ─── ルーム管理 ──────────────────────────────────────────────

        [Button("ルーム全消去")]
        public void RemoveAllRoom()
        {
            var children = new GameObject[roomPanel.transform.childCount];
            for (int i = 0; i < roomPanel.transform.childCount; i++)
            {
                children[i] = roomPanel.transform.GetChild(i).gameObject;
            }
            foreach (var child in children)
            {
                Destroy(child);
            }
        }

        [Button("ルーム作成ダイアログ表示テスト")]
        public void ShowCreateNewRoomDialog()
        {
            if (createNewRoomDialog != null)
            {
                createNewRoomDialog.SetActive(true);
                return;
            }

            if (mediateObject != null && mediateObject.createNewRoomDialog != null)
            {
                mediateObject.createNewRoomDialog.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// オンラインロビーのダイアログから古い形式で呼ばれる場合のテスト用
        /// </summary>
        [Button("部屋作成テスト (旧)")]
        public void CreateNewWaitRoom()
        {
            if (createNewRoomDialog == null)
            {
                Debug.LogWarning("OnlineLobbyScene.CreateNewWaitRoom: createNewRoomDialog is null");
                return;
            }

            var dialogScript = createNewRoomDialog.GetComponent<ICreateNewRoomDialog>();
            createNewRoomDialog.SetActive(false);

            string roomName = dialogScript != null ? dialogScript.RoomName() : "One Shot One Kill!";
            var maxPlayer = dialogScript != null ? dialogScript.MaxPlayer() : 8;
            var password = dialogScript != null ? dialogScript.Password() : "";
            var gameMode = dialogScript != null ? dialogScript.GameMode().ToString() : EGameMode.TeamDeathMatch.ToString();
            var teamBalance = dialogScript != null && dialogScript.TeamBalance();

            networkManager.SendCreateNewWaitRoomRequest(roomName, maxPlayer, gameMode, teamBalance, password);
        }

        /// <summary>
        /// 入室リクエストをサーバーへ送信する。
        /// </summary>
        public void SendEnterRoomRequest()
        {
            var room = FindSelectedOrFirstFilteredRoom();
            if (room == null)
            {
                Debug.LogWarning("OnlineLobbyScene.SendEnterRoomRequest: room list is empty");
                return;
            }

            var roomId = room["RoomID"]?.ToString() ?? room["RoomId"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogWarning("OnlineLobbyScene.SendEnterRoomRequest: room id is empty");
                return;
            }

            currentSelectedRoomId = roomId;
            var playerName = AccountManager.Instance.CurrentProfile.DisplayName;
            var playerId = AccountManager.Instance.CurrentProfile.GlobalUserId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = "local_player";
            }

            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Player";
            }

            networkManager.SendEnterWaitRoomRequest(roomId, playerId, playerName);
        }

        public void SendEnterRoomRequest(string roomId, string playerId, string playerName, string password = "")
        {
            networkManager.SendEnterWaitRoomRequest(roomId, playerId, playerName, password);
        }

        // ─── ルーム絞り込み (UI ボタンから呼ばれる) ──────────────────

        public void ShowAllRooms()   { SetRoomFilter("All"); }
        public void ShowDMRooms()    { SetRoomFilter(nameof(EGameMode.DeathMatch)); }
        public void ShowTDMRooms()   { SetRoomFilter(nameof(EGameMode.TeamDeathMatch)); }
        public void ShowSUVRooms()   { SetRoomFilter(nameof(EGameMode.Survival)); }
        public void ShowTSUVRooms()  { SetRoomFilter(nameof(EGameMode.TeamSurvival)); }
        public void ShowCTFRooms()   { SetRoomFilter(nameof(EGameMode.CaptureTheFlag)); }
        public void ShowArmsRaceRooms() { SetRoomFilter(nameof(EGameMode.ArmsRace)); }

        // ─── シーン遷移 ──────────────────────────────────────────────

        /// <summary>
        /// サーバーとの接続を切断してタイトルへ戻る。
        /// </summary>
        public void DisconnectAndBackToTitle()
        {
            ResetLobbyState(true);
            networkManager.Disconnect();
            var sceneName = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().TitleScene()
                : GeneralSceneMasterData.Instance().TitleScene();
            SceneManager.LoadSceneAsync(sceneName);
        }

        public void GoToShop()
        {
            var sceneName = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().ShopScene()
                : GeneralSceneMasterData.Instance().ShopScene();
            SceneManager.LoadScene(sceneName);
        }

        public void GotoOnlineWaitRoom()
        {
            mainThread.Post(__ =>
            {
                var sceneName = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                    ? mediateObject.GeneralSceneMasterData().OnlineWaitRoomScene()
                    : GeneralSceneMasterData.Instance().OnlineWaitRoomScene();
                SceneManager.LoadScene(sceneName);
            }, null);
        }

        public void SwitchToMissionServer()
        {
            Debug.Log("SwitchToMissionServer");
            // TODO: ミッションサーバーへの切り替え処理
        }

        // ─── ネットワーク受信 ─────────────────────────────────────────

        public void ParseMessageFromServer(JObject json)
        {
            if (lobbySceneController != null)
            {
                lobbySceneController.ParseServerMessage(
                    json,
                (roomId, roomName, capacity) =>
                    {
                        Debug.Log($"OnlineLobbyScene: Room created. RoomID={roomId}, RoomName={roomName}");
                        matchRoomManager.CreateNewOnlineWaitRoom(roomName, capacity);
                        waitRoomManager.CreateNewWaitRoom(roomName, roomId, capacity);
                        GotoOnlineWaitRoom();
                    },
                    errorMessage =>
                    {
                        Debug.LogWarning($"OnlineLobbyScene: Failed to create room: {errorMessage}");
                        if (InfoDialog != null)
                        {
                            InfoDialog.SetActive(true);
                        }
                    },
                    rooms =>
                    {
                        Debug.Log($"OnlineLobbyScene: Received {rooms?.Count ?? 0} rooms");
                        currentRoomList = rooms ?? new JArray();
                        RefreshRoomListView();
                    },
                    (roomId, roomName, players) =>
                    {
                        Debug.Log($"OnlineLobbyScene: Entered room {roomId} ({roomName}), capacity={players}");
                        matchRoomManager.CreateNewOnlineWaitRoom(roomName, players);
                        waitRoomManager.CreateNewWaitRoom(roomName, roomId, players);
                        GotoOnlineWaitRoom();
                    },
                    errorMessage =>
                    {
                        Debug.LogWarning($"OnlineLobbyScene: Failed to enter room: {errorMessage}");
                    });
                return;
            }

            // Fallback (controller not assigned)
            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (messageType == MessageType.RoomListUpdateNotification)
            {
                var rooms = json["Rooms"] as JArray;
                Debug.Log($"OnlineLobbyScene: Received {rooms?.Count ?? 0} rooms");
                currentRoomList = rooms ?? new JArray();
                RefreshRoomListView();
            }
            else if (messageType == MessageType.JoinRoomResponse)
            {
                var success = json["Success"]?.ToObject<bool>() ?? false;
                if (success)
                {
                    var roomId = json["RoomID"]?.ToString() ?? "";
                    var roomName = json["RoomName"]?.ToString() ?? "Room";
                    var capacity = json["Capacity"]?.ToObject<int>() ?? json["Players"]?.ToObject<int>() ?? 0;
                    matchRoomManager.CreateNewOnlineWaitRoom(roomName, capacity);
                    waitRoomManager.CreateNewWaitRoom(roomName, roomId, capacity);
                    GotoOnlineWaitRoom();
                }
                else
                {
                    Debug.LogWarning($"OnlineLobbyScene: Enter room failed: {json["ErrorMessage"]?.ToString()}");
                }
            }
        }

        // ─── INetworkManagerScript の実装 ────────────────────────────

        public void OnConnected()
        {
            ResetLobbyState(false);
            ShowDefaultRooms();
        }

        public void OnDisconnected()
        {
            ResetLobbyState(true);
        }

        public void ParseNetworkMatchMessageFromServer(JObject json)
        {
        }

        public void TestFunc()
        {
        }

        // ─── ロビー内アクション ───────────────────────────────────────

        public void OnCreateNewRoom()
        {
            Debug.Log("OnCreateNewRoom");

            ICreateNewRoomDialog dialogScript = null;
            if (createNewRoomDialog != null) dialogScript = createNewRoomDialog.GetComponent<ICreateNewRoomDialog>();
            if (dialogScript == null && mediateObject.createNewRoomDialog != null) dialogScript = mediateObject.createNewRoomDialog;

            if (dialogScript == null)
            {
                Debug.LogWarning("OnlineLobbyScene.OnCreateNewRoom: dialogScript is null");
                return;
            }

            var maxPlayer = dialogScript.MaxPlayer();
            var password  = dialogScript.Password();
            var gameMode  = dialogScript.GameMode();

            var json = new JObject
            {
                ["MessageType"] = MessageType.CreateRoomRequest,
                ["RoomName"] = dialogScript.RoomName(),
                ["Capacity"] = maxPlayer.ToString(),
                ["GameMode"] = gameMode.ToString(),
                ["TeamBalance"] = dialogScript.TeamBalance() ? "True" : "False",
                ["Password"] = password ?? ""
            };

            networkManager.SendMessage(json);
            Debug.Log($"OnlineLobbyScene: Sent {MessageType.CreateRoomRequest}: {json.ToString(Formatting.None)}");

            if (createNewRoomDialog != null) createNewRoomDialog.SetActive(false);
            if (mediateObject.createNewRoomDialog != null) mediateObject.createNewRoomDialog.gameObject.SetActive(false);

            // 以前のロジックの名残で必要なら処理を挟む
            switch (gameMode)
            {
                case EGameMode.DeathMatch:
                    break;
                case EGameMode.TowerMatch:
                    break;
            }
        }

        [Button("チャット送信テスト")]
        public void SendChat(string str)
        {
            var json = new JObject
            {
                ["MessageType"] = MessageType.AddLobbyChat,
                ["Chat"] = str
            };
            networkManager.SendMessage(json);
        }

        // ─── 入力ブロック ─────────────────────────────────────────────

        [Button("入力ブロック")]
        public void BlockInput()
        {
            StartCoroutine(BlockKeyInputCoroutine());
        }

        private IEnumerator BlockKeyInputCoroutine()
        {
            canInput = false;
            yield return new WaitForSeconds(30.0f);
            canInput = true;
        }

        // ─── プライベートユーティリティ ───────────────────────────────

        private void ShowDefaultRooms()
        {
            Debug.Log("ShowDefaultRooms");
            networkManager.SendUpdateRoomRequest();
        }

        private IEnumerator PeriodicUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                networkManager.SendUpdateRoomRequest();
            }
        }

        private void OnGameSceneLoaded(Scene next, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }

        private void BackToConnectServerScene()
        {
            var sceneName = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().ConnectToServerScene()
                : GeneralSceneMasterData.Instance().ConnectToServerScene();
            SceneManager.LoadScene(sceneName);
        }

        private void SetRoomFilter(string filter)
        {
            currentRoomFilter = filter;
            RefreshRoomListView();
        }

        private void RefreshRoomListView()
        {
            var filtered = FilterRooms(currentRoomList, currentRoomFilter);
            if (string.IsNullOrWhiteSpace(currentSelectedRoomId) || FindRoomById(filtered, currentSelectedRoomId) == null)
            {
                currentSelectedRoomId = GetFirstRoomId(filtered);
            }
            RenderRoomListView(filtered);
            Debug.Log($"OnlineLobbyScene: room list filtered by {currentRoomFilter}, showing {filtered.Count} rooms");
        }

        private void RenderRoomListView(JArray rooms)
        {
            if (roomPanel == null)
            {
                return;
            }

            ClearSpawnedRoomButtons();
            RemoveAllRoom();

            if (RoomButton == null || rooms == null)
            {
                return;
            }

            foreach (var token in rooms)
            {
                if (token is not JObject room)
                {
                    continue;
                }

                var roomId = room["RoomID"]?.ToString() ?? room["RoomId"]?.ToString() ?? "";
                var roomName = room["RoomName"]?.ToString() ?? "Room";
                var gameMode = room["GameMode"]?.ToString() ?? "";
                var capacity = room["Capacity"]?.ToObject<int>() ?? 0;
                var players = room["PlayerCount"]?.ToObject<int>() ?? room["Players"]?.ToObject<int>() ?? 0;
                var isSelected = string.Equals(roomId, currentSelectedRoomId, StringComparison.OrdinalIgnoreCase);

                var instance = Instantiate(RoomButton, roomPanel.transform);
                instance.SetActive(true);
                spawnedRoomButtons.Add(instance);

                var button = instance.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        currentSelectedRoomId = roomId;
                        RenderRoomListView(rooms);
                    });
                }

                var label = instance.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = $"{roomName}\n{gameMode}  {players}/{capacity}";
                }

                var image = instance.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isSelected ? new Color(0.35f, 0.65f, 1f, 1f) : Color.white;
                }
            }
        }

        private void ClearSpawnedRoomButtons()
        {
            for (int i = 0; i < spawnedRoomButtons.Count; i++)
            {
                if (spawnedRoomButtons[i] != null)
                {
                    Destroy(spawnedRoomButtons[i]);
                }
            }

            spawnedRoomButtons.Clear();
        }

        private void ResetLobbyState(bool clearRooms)
        {
            currentSelectedRoomId = "";
            updateCount = 0;

            if (!clearRooms)
            {
                return;
            }

            currentRoomList = new JArray();
            ClearSpawnedRoomButtons();
            RemoveAllRoom();
        }

        private static JArray FilterRooms(JArray rooms, string filter)
        {
            if (rooms == null || rooms.Count == 0 || string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            {
                return rooms ?? new JArray();
            }

            var filtered = new JArray();
            foreach (var room in rooms)
            {
                var mode = room?["GameMode"]?.ToString() ?? "";
                if (string.Equals(mode, filter, StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(room);
                }
            }

            return filtered;
        }

        private JObject FindFirstFilteredRoom()
        {
            var filtered = FilterRooms(currentRoomList, currentRoomFilter);
            if (filtered.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(currentSelectedRoomId))
            {
                foreach (var room in filtered)
                {
                    var roomId = room?["RoomID"]?.ToString() ?? room?["RoomId"]?.ToString() ?? "";
                    if (string.Equals(roomId, currentSelectedRoomId, StringComparison.OrdinalIgnoreCase))
                    {
                        return room as JObject;
                    }
                }
            }

            return filtered[0] as JObject;
        }

        private JObject FindSelectedOrFirstFilteredRoom()
        {
            var filtered = FilterRooms(currentRoomList, currentRoomFilter);
            if (filtered.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(currentSelectedRoomId))
            {
                var selected = FindRoomById(filtered, currentSelectedRoomId);
                if (selected != null)
                {
                    return selected;
                }
            }

            return filtered[0] as JObject;
        }

        private static JObject FindRoomById(JArray rooms, string roomId)
        {
            if (rooms == null || rooms.Count == 0 || string.IsNullOrWhiteSpace(roomId))
            {
                return null;
            }

            foreach (var room in rooms)
            {
                var currentRoomId = room?["RoomID"]?.ToString() ?? room?["RoomId"]?.ToString() ?? "";
                if (string.Equals(currentRoomId, roomId, StringComparison.OrdinalIgnoreCase))
                {
                    return room as JObject;
                }
            }

            return null;
        }

        private static string GetFirstRoomId(JArray rooms)
        {
            if (rooms == null || rooms.Count == 0)
            {
                return "";
            }

            var room = rooms[0];
            return room?["RoomID"]?.ToString() ?? room?["RoomId"]?.ToString() ?? "";
        }

        // ─── AbstractNonBattleScene の実装 ────────────────────────────

        public override SynchronizationContext MainThread() => mainThread;

        protected override void OnStartUnityEditor()
        {
        }

        protected override void OnQuitUnityEditor()
        {
        }

        protected override void OnStartFromEditorDirectly()
        {
        }
    }
}

