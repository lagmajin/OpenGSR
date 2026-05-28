﻿using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] private GuildPanel guildPanel;
        [SerializeField] private GameObject roomPasswordDialog;
        [SerializeField] private TMP_InputField roomPasswordInput;
        [SerializeField] private Button roomPasswordOkButton;
        [SerializeField] private Button roomPasswordCancelButton;
        [SerializeField] private TextMeshProUGUI roomPasswordTitleText;
        [SerializeField] private TextMeshProUGUI roomPasswordMessageText;

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
        private string pendingPasswordRoomId = "";
        private string pendingPasswordRoomName = "";
        private string pendingPasswordPlayerId = "";
        private string pendingPasswordPlayerName = "";
        private readonly List<GameObject> spawnedRoomButtons = new List<GameObject>();

        // ─── Unity ライフサイクル ────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            mainThread = SynchronizationContext.Current;
            EnsureNetworkDependencies();
            if (lobbySceneController == null)
            {
                lobbySceneController = GetComponent<OnlineLobbySceneController>();
            }

            if (DebugFlagManager.IsDebug())
            {
                DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            }
        }

        void Start()
        {
            SceneManager.sceneLoaded += OnGameSceneLoaded;
            EnsureTitleBgm();

            var net = EnsureNetworkManager();
            if (net != null)
            {
                net.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(ParseMessageFromServer)
                    .AddTo(this.gameObject);
                Debug.Log("OnlineLobbyScene: Subscribed to GeneralServerNetworkManager.DataReceivedStream");
            }
            else
            {
                Debug.LogWarning("OnlineLobbyScene: GeneralServerNetworkManager is not available at Start.");
            }

            StartCoroutine(PeriodicUpdateCoroutine());
            ShowDefaultRooms();
        }

        private void EnsureTitleBgm()
        {
            if (SoundManager.Instance.IsBgmPlaying(EBgm.Title))
            {
                Debug.Log("[OnlineLobbyScene] Title BGM is already playing.");
                return;
            }

            Debug.Log("[OnlineLobbyScene] Switching to Title BGM.");
            SoundManager.Instance.EnsureBgm(EBgm.Title, 0f);
        }

        protected override void OnDestroy()
        {
            networkManager?.UnSubscribe(this);
            base.OnDestroy();
        }

        private void OnApplicationQuit()
        {
            networkManager?.Disconnect();
        }

        protected override void Update()
        {
            base.Update();
            if (lobbySceneController != null)
            {
                lobbySceneController.TickInput(
                    canInput,
                    ref updateCount,
                    MaxUpdateCount,
                    () =>
                    {
                        Debug.Log("F5 pressed: Sending UpdateRoomRequest");
                        EnsureNetworkManager()?.SendUpdateRoomRequest();
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
                EnsureNetworkManager()?.SendUpdateRoomRequest();
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
            if (mediateObject != null)
            {
                mediateObject.ShowCreateNewRoomDialog();
                return;
            }

            if (createNewRoomDialog != null)
            {
                createNewRoomDialog.SetActive(true);
            }
        }

        /// <summary>
        /// オンラインロビーのダイアログから古い形式で呼ばれる場合のテスト用
        /// </summary>
        [Button("部屋作成テスト (旧)")]
        public void CreateNewWaitRoom()
        {
            var net = EnsureNetworkManager();
            if (net == null)
            {
                Debug.LogWarning("OnlineLobbyScene.CreateNewWaitRoom: networkManager is not available");
                return;
            }

            var dialogScript = ResolveCreateNewRoomDialog();
            if (dialogScript == null)
            {
                Debug.LogWarning("OnlineLobbyScene.CreateNewWaitRoom: createNewRoomDialog is null");
                return;
            }

            HideCreateNewRoomDialogUi();

            string roomName = dialogScript != null ? dialogScript.RoomName() : "One Shot One Kill!";
            var maxPlayer = dialogScript != null ? dialogScript.MaxPlayer() : 8;
            var password = dialogScript != null ? dialogScript.Password() : "";
            var gameMode = dialogScript != null ? dialogScript.GameMode().ToString() : EGameMode.TeamDeathMatch.ToString();
            var map = dialogScript != null ? dialogScript.Map() : EMap.DryDays;
            var teamBalance = dialogScript != null && dialogScript.TeamBalance();

            net.SendCreateNewWaitRoomRequest(roomName, maxPlayer, gameMode, map.ToString(), teamBalance, password);
        }

        /// <summary>
        /// 入室リクエストをサーバーへ送信する。
        /// </summary>
        public void SendEnterRoomRequest()
        {
            var net = EnsureNetworkManager();
            if (net == null)
            {
                Debug.LogWarning("OnlineLobbyScene.SendEnterRoomRequest: networkManager is not available");
                return;
            }

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

            if (IsPasswordProtectedRoom(room))
            {
                ShowPasswordPrompt(
                    roomId,
                    room["RoomName"]?.ToString() ?? "Room",
                    GetCurrentPlayerId(),
                    GetCurrentPlayerName());
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

            net.SendEnterWaitRoomRequest(roomId, playerId, playerName);
        }

        public void SendEnterRoomRequest(string roomId, string playerId, string playerName, string password = "")
        {
            var net = EnsureNetworkManager();
            if (net == null)
            {
                Debug.LogWarning("OnlineLobbyScene.SendEnterRoomRequest(string): networkManager is not available");
                return;
            }

            net.SendEnterWaitRoomRequest(roomId, playerId, playerName, password);
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
            EnsureNetworkManager()?.Disconnect();
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
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
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(sceneName);
        }

        [Button("ギルドパネル表示")]
        public void ShowGuildPanel()
        {
            if (guildPanel != null)
            {
                guildPanel.Show();
                return;
            }

            Debug.LogWarning("[OnlineLobbyScene] GuildPanel is not assigned.");
        }

        public void HideGuildPanel()
        {
            if (guildPanel != null)
            {
                guildPanel.Hide();
            }
        }

        public void ToggleGuildPanel()
        {
            if (guildPanel != null)
            {
                guildPanel.Toggle();
            }
        }

        public void GotoOnlineWaitRoom()
        {
            mainThread.Post(__ =>
            {
                GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
                var sceneName = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                    ? mediateObject.GeneralSceneMasterData().OnlineWaitRoomScene()
                    : GeneralSceneMasterData.Instance().OnlineWaitRoomScene();
                SceneManager.LoadScene(sceneName);
            }, null);
        }

        public void SwitchToMissionServer()
        {
            var missionScene = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().MissionLobbyScene()
                : GeneralSceneMasterData.Instance().MissionLobbyScene();

            Debug.Log($"[OnlineLobbyScene] Switching to mission lobby: {missionScene}");
            RequestSceneTransition(missionScene, "SwitchToMissionServer");
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
                        var roomInfo = RoomInfoSnapshot.FromJson(json);
                        matchRoomManager.CreateNewOnlineWaitRoom(roomInfo.RoomName, roomInfo.Capacity);
                        var createdGameMode = ParseGameMode(roomInfo.GameMode);
                        var createdMap = ParseMap(roomInfo.Map);
                        var createdOwnerId = string.IsNullOrWhiteSpace(roomInfo.OwnerId) ? "local_player" : roomInfo.OwnerId;
                        var createdTeamBalance = roomInfo.TeamBalance;
                        var createdPassword = json["Password"]?.ToString() ?? "";
                        var waitRoom = waitRoomManager.CreateNewWaitRoom(roomInfo.RoomName, roomInfo.RoomId, roomInfo.Capacity, roomInfo.PlayerCount > 0 ? roomInfo.PlayerCount : 1, createdGameMode, createdOwnerId, createdTeamBalance, createdMap, createdPassword);
                        waitRoom.AddNewPlayer(new PlayerInfo(createdOwnerId, AccountManager.Instance.CurrentProfile.DisplayName)
                        {
                            playerCharacter = GamePlayerManager.Instance.SelectedPlayerCharacter()
                        });
                        GotoOnlineWaitRoom();
                    },
                    errorMessage =>
                    {
                        Debug.LogWarning($"OnlineLobbyScene: Failed to create room: {errorMessage}");
                        ShowInfoDialog("ルーム作成に失敗しました", errorMessage);
                    },
                    (roomListSnapshot, roomListJson) =>
                    {
                        var rooms = ExtractRoomArray(roomListJson, roomListSnapshot);
                        Debug.Log($"OnlineLobbyScene: Received {rooms.Count} rooms");
                        currentRoomList = rooms;
                        RefreshRoomListView();
                    },
                    (roomId, roomName, capacity, playerCount) =>
                    {
                        Debug.Log($"OnlineLobbyScene: Entered room {roomId} ({roomName}), capacity={capacity}, playerCount={playerCount}");
                        var roomInfo = RoomInfoSnapshot.FromJson(json);
                        matchRoomManager.CreateNewOnlineWaitRoom(roomInfo.RoomName, roomInfo.Capacity);
                        var selectedGameMode = ParseGameMode(roomInfo.GameMode);
                        var selectedMap = ParseMap(roomInfo.Map);
                        var selectedOwnerId = string.IsNullOrWhiteSpace(roomInfo.OwnerId) ? "" : roomInfo.OwnerId;
                        var selectedCapacity = roomInfo.Capacity > 0 ? roomInfo.Capacity : capacity;
                        var selectedTeamBalance = roomInfo.TeamBalance;
                        var selectedPassword = json["Password"]?.ToString() ?? "";
                        var waitRoom = waitRoomManager.CreateNewWaitRoom(roomInfo.RoomName, roomInfo.RoomId, selectedCapacity, roomInfo.PlayerCount > 0 ? roomInfo.PlayerCount : playerCount, selectedGameMode, selectedOwnerId, selectedTeamBalance, selectedMap, selectedPassword);
                        waitRoom.AddNewPlayer(new PlayerInfo(
                            id: AccountManager.Instance.CurrentProfile.GlobalUserId,
                            name: AccountManager.Instance.CurrentProfile.DisplayName)
                        {
                            playerCharacter = GamePlayerManager.Instance.SelectedPlayerCharacter()
                        });
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
                var roomListSnapshot = OpenGSCore.RoomListSnapshot.FromJson(json);
                var rooms = ExtractRoomArray(json, roomListSnapshot);
                Debug.Log($"OnlineLobbyScene: Received {rooms.Count} rooms");
                currentRoomList = rooms;
                RefreshRoomListView();
            }
            else if (messageType == MessageType.CreateRoomResponse)
            {
                var success = json["Success"]?.ToObject<bool>() ?? false;
                if (success)
                {
                    var roomInfo = RoomInfoSnapshot.FromJson(json);
                    Debug.Log($"OnlineLobbyScene: Room created. RoomID={roomInfo.RoomId}, RoomName={roomInfo.RoomName}");
                    matchRoomManager.CreateNewOnlineWaitRoom(roomInfo.RoomName, roomInfo.Capacity);
                    var createdGameMode = ParseGameMode(roomInfo.GameMode);
                    var createdMap = ParseMap(roomInfo.Map);
                    var createdOwnerId = string.IsNullOrWhiteSpace(roomInfo.OwnerId) ? "local_player" : roomInfo.OwnerId;
                    var createdTeamBalance = roomInfo.TeamBalance;
                    var createdPassword = json["Password"]?.ToString() ?? "";
                    var waitRoom = waitRoomManager.CreateNewWaitRoom(roomInfo.RoomName, roomInfo.RoomId, roomInfo.Capacity, roomInfo.PlayerCount > 0 ? roomInfo.PlayerCount : 1, createdGameMode, createdOwnerId, createdTeamBalance, createdMap, createdPassword);
                    waitRoom.AddNewPlayer(new PlayerInfo(createdOwnerId, AccountManager.Instance.CurrentProfile.DisplayName)
                    {
                        playerCharacter = GamePlayerManager.Instance.SelectedPlayerCharacter()
                    });
                    GotoOnlineWaitRoom();
                }
                else
                {
                    var errorMessage = json["ErrorMessage"]?.ToString();
                    Debug.LogWarning($"OnlineLobbyScene: Failed to create room: {errorMessage}");
                    ShowInfoDialog("ルーム作成に失敗しました", errorMessage);
                }
            }
            else if (messageType == MessageType.JoinRoomResponse)
            {
                var success = json["Success"]?.ToObject<bool>() ?? false;
                if (success)
                {
                    var roomInfo = RoomInfoSnapshot.FromJson(json);
                    var roomId = roomInfo.RoomId;
                    var roomName = roomInfo.RoomName;
                    var playerCount = roomInfo.PlayerCount > 0 ? roomInfo.PlayerCount : ReadPlayerCount(json);
                    matchRoomManager.CreateNewOnlineWaitRoom(roomName, roomInfo.Capacity);
                    var selectedGameMode = ParseGameMode(roomInfo.GameMode);
                    var selectedMap = ParseMap(roomInfo.Map);
                    var selectedOwnerId = roomInfo.OwnerId;
                    var selectedCapacity = roomInfo.Capacity;
                    var selectedTeamBalance = roomInfo.TeamBalance;
                    var selectedPassword = json["Password"]?.ToString() ?? "";
                    var waitRoom = waitRoomManager.CreateNewWaitRoom(roomName, roomId, selectedCapacity, playerCount, selectedGameMode, selectedOwnerId, selectedTeamBalance, selectedMap, selectedPassword);
                    waitRoom.AddNewPlayer(new PlayerInfo(
                        id: AccountManager.Instance.CurrentProfile.GlobalUserId,
                        name: AccountManager.Instance.CurrentProfile.DisplayName)
                    {
                        playerCharacter = GamePlayerManager.Instance.SelectedPlayerCharacter()
                    });
                    GotoOnlineWaitRoom();
                }
                else
                {
                    var errorMessage = json["ErrorMessage"]?.ToString();
                    Debug.LogWarning($"OnlineLobbyScene: Enter room failed: {errorMessage}");
                    ShowInfoDialog("ルームに入れませんでした", errorMessage);
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
            if (json == null)
            {
                Debug.LogWarning("[OnlineLobbyScene] ParseNetworkMatchMessageFromServer received null json.");
                return;
            }

            ParseMessageFromServer(json);
        }

        public void TestFunc()
        {
            Debug.Log("[OnlineLobbyScene] TestFunc");
        }

        // ─── ロビー内アクション ───────────────────────────────────────

        public void OnCreateNewRoom()
        {
            OnCreateNewRoom(null);
        }

        public void OnQuickStart()
        {
            Debug.Log("OnQuickStart");

            if (networkManager == null)
            {
                Debug.LogWarning("OnlineLobbyScene.OnQuickStart: networkManager is null");
                return;
            }

            var quickStartRoom = FindBestQuickStartRoom();
            if (quickStartRoom == null)
            {
                Debug.LogWarning("OnlineLobbyScene.OnQuickStart: no joinable rooms found");
                ShowInfoDialog("クイック参加", "参加できる部屋がありませんでした");
                return;
            }

            var roomId = quickStartRoom["RoomID"]?.ToString() ?? quickStartRoom["RoomId"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogWarning("OnlineLobbyScene.OnQuickStart: room id is empty");
                return;
            }

            var roomName = quickStartRoom["RoomName"]?.ToString() ?? "Room";
            var playerCount = GetRoomPlayerCount(quickStartRoom);
            var capacity = quickStartRoom["Capacity"]?.ToObject<int>() ?? 0;
            Debug.Log($"OnlineLobbyScene.OnQuickStart: joining {roomName} ({playerCount}/{capacity})");

            currentSelectedRoomId = roomId;
            SendEnterRoomRequest(roomId, GetCurrentPlayerId(), GetCurrentPlayerName());
        }

        public void OnCreateNewRoom(ICreateNewRoomDialog sourceDialog)
        {
            Debug.Log("OnCreateNewRoom");

            var net = EnsureNetworkManager();
            if (net == null)
            {
                Debug.LogWarning("OnlineLobbyScene.OnCreateNewRoom: networkManager is null");
                return;
            }

            ICreateNewRoomDialog dialogScript = sourceDialog ?? ResolveCreateNewRoomDialog();
            var dialogComponent = sourceDialog as Component;

            if (dialogScript == null)
            {
                Debug.LogWarning("OnlineLobbyScene.OnCreateNewRoom: dialogScript is null");
                return;
            }

            var maxPlayer = dialogScript.MaxPlayer();
            var password  = dialogScript.Password();
            var gameMode  = dialogScript.GameMode();
            var map = dialogScript.Map();

            var json = new JObject
            {
                ["MessageType"] = MessageType.CreateRoomRequest,
                ["RoomName"] = dialogScript.RoomName(),
                ["Capacity"] = maxPlayer.ToString(),
                ["GameMode"] = gameMode.ToString(),
                ["Map"] = map.ToString(),
                ["TeamBalance"] = dialogScript.TeamBalance() ? "True" : "False",
                ["Password"] = password ?? ""
            };

            net.SendCreateNewWaitRoomRequest(
                dialogScript.RoomName(),
                maxPlayer,
                gameMode.ToString(),
                map.ToString(),
                dialogScript.TeamBalance(),
                password);
            Debug.Log($"OnlineLobbyScene: Sent {MessageType.CreateRoomRequest}: {json.ToString(Formatting.None)}");

            if (dialogComponent != null)
            {
                dialogComponent.gameObject.SetActive(false);
            }
            HideCreateNewRoomDialogUi();

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
            EnsureNetworkManager()?.SendUpdateRoomRequest();
        }

        private IEnumerator PeriodicUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                EnsureNetworkManager()?.SendUpdateRoomRequest();
            }
        }

        private void EnsureNetworkDependencies()
        {
            EnsureNetworkManager();

            if (matchRoomManager == null)
            {
                try
                {
                    matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"OnlineLobbyScene: Failed to resolve MatchRoomManager: {ex.Message}");
                }
            }

            if (waitRoomManager == null)
            {
                try
                {
                    waitRoomManager = DependencyInjectionConfig.Resolve<WaitRoomManager>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"OnlineLobbyScene: Failed to resolve WaitRoomManager: {ex.Message}");
                }
            }
        }

        private GeneralServerNetworkManager EnsureNetworkManager()
        {
            if (networkManager != null)
            {
                return networkManager;
            }

            try
            {
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"OnlineLobbyScene: Failed to resolve GeneralServerNetworkManager: {ex.Message}");
            }

            return networkManager;
        }

        private ICreateNewRoomDialog ResolveCreateNewRoomDialog()
        {
            if (mediateObject != null)
            {
                var runtimeDialog = mediateObject.CurrentCreateNewRoomDialog;
                if (runtimeDialog != null)
                {
                    return runtimeDialog;
                }
            }

            if (createNewRoomDialog != null)
            {
                return createNewRoomDialog.GetComponent<ICreateNewRoomDialog>();
            }

            return null;
        }

        private void HideCreateNewRoomDialogUi()
        {
            if (mediateObject != null)
            {
                mediateObject.HideCreateNewRoomDialog();
            }

            if (createNewRoomDialog != null)
            {
                createNewRoomDialog.SetActive(false);
            }
        }

        private Transform GetCreateNewRoomDialogParent()
        {
            if (createNewRoomDialog != null && createNewRoomDialog.transform.parent != null)
            {
                return createNewRoomDialog.transform.parent;
            }

            if (InfoDialog != null && InfoDialog.transform.parent != null)
            {
                return InfoDialog.transform.parent;
            }

            if (roomPanel != null && roomPanel.transform.parent != null)
            {
                return roomPanel.transform.parent;
            }

            return null;
        }

        private void ShowPasswordPrompt(string roomId, string roomName, string playerId, string playerName)
        {
            pendingPasswordRoomId = roomId ?? "";
            pendingPasswordRoomName = roomName ?? "Room";
            pendingPasswordPlayerId = playerId ?? GetCurrentPlayerId();
            pendingPasswordPlayerName = playerName ?? GetCurrentPlayerName();

            EnsurePasswordPromptUi();

            if (roomPasswordTitleText != null)
            {
                roomPasswordTitleText.text = "パスワードが必要です";
            }

            if (roomPasswordMessageText != null)
            {
                roomPasswordMessageText.text = $"{pendingPasswordRoomName} は鍵付きです。";
            }

            if (roomPasswordInput != null)
            {
                roomPasswordInput.text = "";
                roomPasswordInput.ActivateInputField();
                roomPasswordInput.Select();
            }

            if (roomPasswordDialog != null)
            {
                roomPasswordDialog.SetActive(true);
            }
        }

        private void HidePasswordPrompt()
        {
            if (roomPasswordDialog != null)
            {
                roomPasswordDialog.SetActive(false);
            }
        }

        private void ConfirmPasswordPrompt()
        {
            var password = roomPasswordInput != null ? roomPasswordInput.text : "";
            var roomId = pendingPasswordRoomId;
            var playerId = pendingPasswordPlayerId;
            var playerName = pendingPasswordPlayerName;

            HidePasswordPrompt();

            if (string.IsNullOrWhiteSpace(roomId))
            {
                ShowInfoDialog("パスワード入力", "部屋情報が見つかりませんでした。");
                return;
            }

            SendEnterRoomRequest(roomId, playerId, playerName, password);
        }

        private void CancelPasswordPrompt()
        {
            pendingPasswordRoomId = "";
            pendingPasswordRoomName = "";
            pendingPasswordPlayerId = "";
            pendingPasswordPlayerName = "";
            HidePasswordPrompt();
        }

        private void EnsurePasswordPromptUi()
        {
            if (roomPasswordDialog == null)
            {
                roomPasswordDialog = GetOrCreatePasswordPromptRoot();
            }

            if (roomPasswordDialog == null)
            {
                return;
            }

            if (roomPasswordTitleText == null)
            {
                roomPasswordTitleText = roomPasswordDialog.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (roomPasswordMessageText == null)
            {
                var texts = roomPasswordDialog.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length > 1)
                {
                    roomPasswordMessageText = texts[1];
                }
            }

            if (roomPasswordInput == null)
            {
                roomPasswordInput = roomPasswordDialog.GetComponentInChildren<TMP_InputField>(true);
            }

            if (roomPasswordOkButton == null)
            {
                roomPasswordOkButton = FindChildButton(roomPasswordDialog.transform, "OK");
            }

            if (roomPasswordCancelButton == null)
            {
                roomPasswordCancelButton = FindChildButton(roomPasswordDialog.transform, "Cancel");
            }

            if (roomPasswordOkButton != null)
            {
                roomPasswordOkButton.onClick.RemoveListener(ConfirmPasswordPrompt);
                roomPasswordOkButton.onClick.AddListener(ConfirmPasswordPrompt);
            }

            if (roomPasswordCancelButton != null)
            {
                roomPasswordCancelButton.onClick.RemoveListener(CancelPasswordPrompt);
                roomPasswordCancelButton.onClick.AddListener(CancelPasswordPrompt);
            }
        }

        private GameObject GetOrCreatePasswordPromptRoot()
        {
            if (roomPasswordDialog != null)
            {
                return roomPasswordDialog;
            }

            var parent = GetCreateNewRoomDialogParent();
            if (parent == null)
            {
                parent = transform;
            }

            var root = new GameObject("RoomPasswordDialog", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(520f, 260f);
            rect.anchoredPosition = Vector2.zero;

            var background = root.GetComponent<Image>();
            background.color = new Color(0.08f, 0.12f, 0.18f, 0.96f);

            CreatePromptText(root.transform, "Title", "パスワードが必要です", new Vector2(0f, 90f), 30f, FontStyles.Bold);
            CreatePromptText(root.transform, "Message", "この部屋は鍵付きです。", new Vector2(0f, 56f), 20f, FontStyles.Normal);
            roomPasswordInput = CreatePasswordInput(root.transform);
            roomPasswordOkButton = CreatePromptButton(root.transform, "OK", "OK", new Vector2(-90f, -78f));
            roomPasswordCancelButton = CreatePromptButton(root.transform, "Cancel", "Cancel", new Vector2(90f, -78f));

            roomPasswordTitleText = root.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            roomPasswordMessageText = root.transform.Find("Message")?.GetComponent<TextMeshProUGUI>();

            root.SetActive(false);
            return root;
        }

        private static TextMeshProUGUI CreatePromptText(Transform parent, string name, string text, Vector2 anchoredPosition, float fontSize, FontStyles fontStyle)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(460f, 32f);
            rect.anchoredPosition = anchoredPosition;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static TMP_InputField CreatePasswordInput(Transform parent)
        {
            var go = new GameObject("PasswordInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 40f);
            rect.anchoredPosition = new Vector2(0f, 5f);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.95f);

            var textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(go.transform, false);
            var textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0f, 0f);
            textAreaRect.anchorMax = new Vector2(1f, 1f);
            textAreaRect.offsetMin = new Vector2(10f, 6f);
            textAreaRect.offsetMax = new Vector2(-10f, -6f);

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(textArea.transform, false);
            var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.text = "4桁のパスワード";
            placeholder.fontSize = 18f;
            placeholder.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.raycastTarget = false;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(textArea.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 18f;
            text.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;

            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = textAreaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 4;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static Button CreatePromptButton(Transform parent, string name, string labelText, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120f, 36f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.35f, 0.55f, 1f);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private static Button FindChildButton(Transform root, string buttonName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button != null && string.Equals(button.name, buttonName, StringComparison.OrdinalIgnoreCase))
                {
                    return button;
                }
            }

            return null;
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
                var accessMark = IsPasswordProtectedRoom(room) ? " [LOCK]" : "";
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
                    label.text = $"{roomName}{accessMark}\n{gameMode}  {players}/{capacity}";
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

        private void ShowInfoDialog(string title, string message)
        {
            if (InfoDialog == null)
            {
                return;
            }

            var lines = string.IsNullOrWhiteSpace(message)
                ? new[] { title }
                : new[] { title, message };

            var legacyText = InfoDialog.GetComponentInChildren<Text>(true);
            if (legacyText != null)
            {
                legacyText.text = string.Join("\n", lines);
            }

            var tmpText = InfoDialog.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmpText != null)
            {
                tmpText.text = string.Join("\n", lines);
            }

            InfoDialog.SetActive(true);
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

        private static JArray ExtractRoomArray(JObject json, OpenGSCore.RoomListSnapshot roomListSnapshot)
        {
            if (json?["Rooms"] is JArray rawRooms)
            {
                return rawRooms;
            }

            return roomListSnapshot?.ToRoomArray() ?? new JArray();
        }

        private JObject FindFirstFilteredRoom()
        {
            var filtered = FilterRooms(currentRoomList, currentRoomFilter);
            if (filtered.Count == 0)
            {
                Debug.LogWarning("[OnlineLobbyScene] No rooms matched the current filter.");
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
                Debug.LogWarning("[OnlineLobbyScene] No rooms available for selection.");
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
                Debug.LogWarning("[OnlineLobbyScene] FindRoomById received invalid arguments.");
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

            Debug.LogWarning($"[OnlineLobbyScene] Room not found: {roomId}");
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

        private JObject FindBestQuickStartRoom()
        {
            var filtered = FilterRooms(currentRoomList, currentRoomFilter);
            var room = FindBestJoinableRoom(filtered);
            if (room != null)
            {
                return room;
            }

            if (!string.Equals(currentRoomFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                room = FindBestJoinableRoom(currentRoomList);
            }

            return room;
        }

        private static JObject FindBestJoinableRoom(JArray rooms)
        {
            if (rooms == null || rooms.Count == 0)
            {
                return null;
            }

            JObject bestRoom = null;
            var bestFill = -1f;
            var bestPlayerCount = -1;
            var bestCapacity = int.MaxValue;

            foreach (var token in rooms)
            {
                if (token is not JObject room)
                {
                    continue;
                }

                var roomId = room["RoomID"]?.ToString() ?? room["RoomId"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(roomId))
                {
                    continue;
                }

                var capacity = room["Capacity"]?.ToObject<int>() ?? 0;
                var playerCount = GetRoomPlayerCount(room);
                if (IsPasswordProtectedRoom(room))
                {
                    continue;
                }
                if (capacity <= 0 || playerCount >= capacity)
                {
                    continue;
                }

                var fillRatio = capacity > 0 ? (float)playerCount / capacity : 0f;
                if (fillRatio > bestFill ||
                    (Mathf.Approximately(fillRatio, bestFill) && playerCount > bestPlayerCount) ||
                    (Mathf.Approximately(fillRatio, bestFill) && playerCount == bestPlayerCount && capacity < bestCapacity))
                {
                    bestFill = fillRatio;
                    bestPlayerCount = playerCount;
                    bestCapacity = capacity;
                    bestRoom = room;
                }
            }

            return bestRoom;
        }

        private static int GetRoomPlayerCount(JObject room)
        {
            if (room == null)
            {
                return 0;
            }

            var playerCountToken = room["PlayerCount"];
            if (playerCountToken != null && int.TryParse(playerCountToken.ToString(), out var playerCount))
            {
                return playerCount;
            }

            var playersToken = room["Players"];
            if (playersToken is JArray playersArray)
            {
                return playersArray.Count;
            }

            if (playersToken != null && int.TryParse(playersToken.ToString(), out playerCount))
            {
                return playerCount;
            }

            return 0;
        }

        private static bool IsPasswordProtectedRoom(JObject room)
        {
            if (room == null)
            {
                return false;
            }

            var hasPasswordToken = room["HasPassword"];
            if (hasPasswordToken != null && bool.TryParse(hasPasswordToken.ToString(), out var hasPassword))
            {
                return hasPassword;
            }

            return !string.IsNullOrWhiteSpace(room["Password"]?.ToString());
        }

        private string GetCurrentPlayerName()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.DisplayName) ? "Player" : profile.DisplayName;
        }

        private string GetCurrentPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            var playerId = profile?.GlobalUserId;
            return string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
        }

        private static EGameMode ParseGameMode(string value)
        {
            return Enum.TryParse(value, out EGameMode parsed) ? parsed : EGameMode.DeathMatch;
        }

        private static EMap ParseMap(string value)
        {
            return Enum.TryParse(value, out EMap parsed) ? parsed : EMap.DryDays;
        }

        private static bool ParseBool(string value)
        {
            return bool.TryParse(value, out var parsed) && parsed;
        }

        private static int ReadPlayerCount(JObject json)
        {
            if (json == null)
            {
                return 1;
            }

            var playerCountToken = json["PlayerCount"];
            if (playerCountToken != null && int.TryParse(playerCountToken.ToString(), out var playerCount))
            {
                return playerCount;
            }

            var playersToken = json["Players"];
            if (playersToken is JArray playersArray)
            {
                return playersArray.Count;
            }

            if (playersToken != null && int.TryParse(playersToken.ToString(), out playerCount))
            {
                return playerCount;
            }

            return 1;
        }

        // ─── AbstractNonBattleScene の実装 ────────────────────────────

        public override SynchronizationContext MainThread() => mainThread;

        protected override void OnStartUnityEditor()
        {
            EnsureTitleBgm();
        }

        protected override void OnQuitUnityEditor()
        {
            networkManager?.Disconnect();
        }

        protected override void OnStartFromEditorDirectly()
        {
            PrettyLogger.Log("System", "Online lobby started from editor.");
            canInput = true;
        }
    }
}

