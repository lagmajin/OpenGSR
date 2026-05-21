#pragma warning disable 0219
#pragma warning disable 0105

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenGS
{
    public partial class OnlineWaitRoomScene : AbstractNonBattleScene, IOnlineWaitRoom, IWaitRoom, IWaitRoomUiManager
    {
        private GeneralServerNetworkManager generalNetworkManager;
        public Button chara;
        public Button map;

        [SerializeField] public InputField inputField;
        [SerializeField] public Text text;

        [Required] public WaitRoomNetworkManager networkManager;

        [SerializeField] [Required] private WaitRoomMediateObject mediateObject;
        [SerializeField] private GameObject weaponLimitDialog;
        [SerializeField] private Button weaponLimitButton;
        [SerializeField] private Button gameModeButton;
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button roomNameApplyButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button minusButton;
        [SerializeField] private Graphic readyButtonGraphic;
        [SerializeField] private Text roomTitleText;
        [SerializeField] private TMP_Text roomTitleTmpText;
        [SerializeField] private InputField roomNameLegacyInputField;
        [SerializeField] private TMP_InputField roomNameTmpInputField;
        [SerializeField] private Text gameModeText;
        [SerializeField] private TMP_Text gameModeTmpText;
        [SerializeField] private Text teamBalanceText;
        [SerializeField] private TMP_Text teamBalanceTmpText;
        [SerializeField] private Transform waitRoomPlayerSlotsRoot;
        [SerializeField] private GameObject playerSlotTemplate;
        [SerializeField] private InviteDialog inviteDialog;

        private bool roomOwner = true;

        [SerializeField] private WaitRoomPlayerSlot mySlot;

        private readonly List<GameObject> activePlayerSlotObjects = new List<GameObject>();
        private SynchronizationContext mainThread;
        private Coroutine startCountdownCoroutine;

        public override SynchronizationContext MainThread()
        {
            return mainThread;
        }

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);

            mainThread = SynchronizationContext.Current;
            generalNetworkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            AutoBindIfNeeded();
            SetupListeners();
        }

        void Start()
        {
            PlayWaitRoomBgm();

            timer.timeupEvent.AddListener(TimeUp);

            LoadRoomSetting();
            InitializePlayerInfoUi();
            BindNetworkStreams();
            RefreshWaitRoomUi();
        }

        void Update()
        {
            if (Input.anyKey)
            {
                timer.ReStartTimer();
            }
        }

        private void PlayWaitRoomBgm()
        {
            if (SoundManager.Instance.IsBgmPlaying(EBgm.WaitRoom))
            {
                return;
            }

            SoundManager.Instance.StopBgm();
            SoundManager.Instance.EnsureBgm(EBgm.WaitRoom, 0f);

            if (SoundManager.Instance.IsBgmPlaying(EBgm.WaitRoom))
            {
                return;
            }

            var waitRoomClip = Resources.Load<AudioClip>("BGM/BGM_WaitRoom");
            if (waitRoomClip != null)
            {
                SoundManager.Instance.PlayBgm(waitRoomClip);
            }
            else
            {
                Debug.LogWarning("[OnlineWaitRoomScene] WaitRoom BGM clip was not found.");
            }
        }

        private void DebugConnect()
        {
            DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>().ConnectToGeneralServerSync("127.0.0.1", 50000, "test", "test");
        }

        protected override void OnStartFromEditorDirectly()
        {
        }

        protected override void OnStartUnityEditor()
        {
        }

        protected override void OnQuitUnityEditor()
        {
        }

        private void Reset()
        {
            if (!timer)
            {
                timer = GetComponent<GameTimer>();
            }

            if (!mediateObject)
            {
                mediateObject = GetComponent<WaitRoomMediateObject>();
            }
        }

        void OnBackRoomFromBattle()
        {
        }

        public void ChangeGameMode()
        {
            if (!IsRoomOwner())
            {
                return;
            }

            var room = ResolveWaitRoom();
            if (room == null)
            {
                return;
            }

            var nextMode = ResolveNextGameMode(room.GameMode);
            ApplyGameMode(nextMode, true);
        }

        public void ChangeGameMode(EGameMode mode)
        {
            if (!IsRoomOwner())
            {
                return;
            }

            ApplyGameMode(mode, true);
        }

        private void ApplyGameMode(EGameMode mode, bool sendToServer)
        {
            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.GameMode = mode;
            }

            if (sendToServer)
            {
                SendWaitRoomSettingsChange(new JObject
                {
                    ["GameMode"] = mode.ToString()
                });
            }

            SetText(gameModeText, gameModeTmpText, mode.ToString());
        }

        public void ChangeMap(EMap map)
        {
            if (!IsRoomOwner())
            {
                return;
            }

            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.Map = map;
            }

            SendWaitRoomSettingsChange(new JObject
            {
                ["Map"] = map.ToString()
            });
        }

        public void ChangeTeamBalance(bool balance)
        {
            if (!IsRoomOwner())
            {
                return;
            }

            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.TeamBalance = balance;
            }

            SendWaitRoomSettingsChange(new JObject
            {
                ["TeamBalance"] = balance
            });

            SetTeamBalanceText();
        }

        public bool IsRoomOwner()
        {
            return roomOwner;
        }

        public void ResignOwner()
        {
            if (IsRoomOwner())
            {
                roomOwner = false;
            }
        }

        private void GameSceneLoaded(Scene next, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= GameSceneLoaded;
        }

        private void WaitRoomSettingChanged()
        {
            RefreshWaitRoomUi();
        }

        void Ready(bool ready)
        {
            if (ready)
            {
                SendReadyRequest();
            }
            else
            {
                SendUnReadyRequest();
            }
        }

        void LoadGameScene()
        {
            Debug.Log("Go to loading Scene...");
            GameFlagsManager.GetInstance().BeforeSceneName = generalSceneMasterData.OnlineWaitRoomScene();
            RequestSceneTransition(generalSceneMasterData.OnlineLoadingScene(), "GameStart");
        }

        public void Plus()
        {
            if (!IsRoomOwner())
            {
                return;
            }

            var room = ResolveWaitRoom();
            if (room == null)
            {
                return;
            }

            ChangeRoomCapacity(Mathf.Min(room.Capacity + 1, 16));
        }

        public void Minus()
        {
            if (!IsRoomOwner())
            {
                return;
            }

            var room = ResolveWaitRoom();
            if (room == null)
            {
                return;
            }

            ChangeRoomCapacity(Mathf.Max(room.PlayerCount, room.Capacity - 1));
        }

        [Button("チャット送信テスト")]
        public void SendChat(string str)
        {
            var message = str;
            if (string.IsNullOrWhiteSpace(message) && inputField != null)
            {
                message = inputField.text;
            }

            if (string.IsNullOrWhiteSpace(message) && text != null)
            {
                message = text.text;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var playerName = AccountManager.Instance.CurrentProfile.DisplayName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Player";
            }

            var playerId = ResolveLocalPlayerId();
            if (networkManager == null)
            {
                Debug.LogWarning("[OnlineWaitRoomScene] WaitRoomNetworkManager is not assigned.");
                return;
            }

            networkManager.SendWaitRoomChat(playerId, playerName, message);

            if (inputField != null)
            {
                inputField.text = string.Empty;
            }
        }

        public void ExitWaitRoom()
        {
            if (networkManager != null)
            {
                networkManager.SendWaitRoomLeave(ResolveLocalPlayerId());
            }

            GoToLobby();
        }

        public void ShowWeaponLimitDialog()
        {
            AutoBindIfNeeded();
            if (weaponLimitDialog == null)
            {
                return;
            }

            weaponLimitDialog.SetActive(true);
        }

        public void showWeaponLimitDialog()
        {
            ShowWeaponLimitDialog();
        }

        public void TimeUp()
        {
            if (IsRoomOwner())
            {
                ResignOwner();
            }

            ExitWaitRoom();
        }

        public void ChangeRoomTitle(string roomTitle)
        {
            if (!IsRoomOwner())
            {
                return;
            }

            ApplyRoomTitle(roomTitle, true);
        }

        private void ApplyRoomTitle(string roomTitle, bool sendToServer)
        {
            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.RoomName = roomTitle ?? "";
            }

            if (sendToServer)
            {
                SendWaitRoomSettingsChange(new JObject
                {
                    ["RoomName"] = roomTitle ?? ""
                });
            }

            SetText(roomTitleText, roomTitleTmpText, BuildRoomTitle());
        }

        public void ChangeRoomCapacity(int capacity)
        {
            ApplyRoomCapacity(capacity, true);
        }

        private void ApplyRoomCapacity(int capacity, bool sendToServer)
        {
            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.Capacity = capacity;
            }

            if (sendToServer)
            {
                SendWaitRoomSettingsChange(new JObject
                {
                    ["Capacity"] = capacity
                });
            }

            SetText(roomTitleText, roomTitleTmpText, BuildRoomTitle());
        }

        private void LoadRoomSetting()
        {
            var room = ResolveWaitRoom();
            if (room == null)
            {
                return;
            }

            var uiManager = mediateObject != null ? mediateObject.WaitRoomUiManager() : this;
            if (uiManager == null)
            {
                uiManager = this;
            }

            ApplyRoomTitle(room.RoomName, false);
            ApplyRoomCapacity(room.Capacity, false);
            ApplyGameMode(room.GameMode, false);
        }

        private void AutoBindIfNeeded()
        {
            if (!weaponLimitDialog)
            {
                weaponLimitDialog = FindInactiveGameObject("WeaponLimitDialog");
            }

            if (!weaponLimitButton)
            {
                weaponLimitButton = FindInactiveComponent<Button>("WeaponLimitButton");
            }

            readyButton ??= FindInactiveComponent<Button>("ReadyButton");
            startButton ??= FindInactiveComponent<Button>("StartButton");
            exitButton ??= FindInactiveComponent<Button>("ExitButton");
            plusButton ??= FindInactiveComponent<Button>("Plus");
            minusButton ??= FindInactiveComponent<Button>("Minus");
            gameModeButton ??= FindInactiveComponent<Button>("GameModeButton");
            inviteButton ??= FindInactiveComponent<Button>("InviteButton");
            roomNameApplyButton ??= FindInactiveComponent<Button>("RoomNameApply");
            roomNameLegacyInputField ??= FindInactiveComponent<InputField>("RoomName");
            roomNameTmpInputField ??= FindInactiveComponent<TMP_InputField>("RoomName");
            inviteDialog ??= FindInactiveComponent<InviteDialog>("InviteDialog");

            if (readyButtonGraphic == null && readyButton != null)
            {
                readyButtonGraphic = readyButton.targetGraphic;
            }

            roomTitleText ??= FindInactiveComponent<Text>("RoomTitle");
            roomTitleTmpText ??= FindInactiveComponent<TMP_Text>("RoomTitle");
            gameModeText ??= FindInactiveComponent<Text>("GameModeText");
            gameModeTmpText ??= FindInactiveComponent<TMP_Text>("GameModeText");
            teamBalanceText ??= FindInactiveComponent<Text>("TeamBalanceText");
            teamBalanceTmpText ??= FindInactiveComponent<TMP_Text>("TeamBalanceText");

            if (waitRoomPlayerSlotsRoot == null)
            {
                if (mySlot != null && mySlot.transform != null && mySlot.transform.parent != null)
                {
                    waitRoomPlayerSlotsRoot = mySlot.transform.parent;
                }
                else if (playerSlotTemplate != null && playerSlotTemplate.transform != null && playerSlotTemplate.transform.parent != null)
                {
                    waitRoomPlayerSlotsRoot = playerSlotTemplate.transform.parent;
                }
                else
                {
                    var rootObject = FindInactiveGameObject("WaitroomPlayerSlots");
                    waitRoomPlayerSlotsRoot = rootObject != null ? rootObject.transform : null;
                }
            }

            if (playerSlotTemplate == null && waitRoomPlayerSlotsRoot != null)
            {
                for (var i = 0; i < waitRoomPlayerSlotsRoot.childCount; i++)
                {
                    var child = waitRoomPlayerSlotsRoot.GetChild(i);
                    if (child != null && child.name.Contains("PlayerSlot"))
                    {
                        playerSlotTemplate = child.gameObject;
                        break;
                    }
                }
            }
        }

        private void SetupListeners()
        {
            if (weaponLimitButton != null)
            {
                weaponLimitButton.onClick.RemoveListener(ShowWeaponLimitDialog);
                weaponLimitButton.onClick.AddListener(ShowWeaponLimitDialog);
            }

            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(ToggleReadyState);
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(RequestGameStart);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(ExitWaitRoom);
            }

            if (plusButton != null)
            {
                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(Plus);
            }

            if (minusButton != null)
            {
                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(Minus);
            }

            if (gameModeButton != null)
            {
                gameModeButton.onClick.RemoveAllListeners();
                gameModeButton.onClick.AddListener(ChangeGameMode);
            }

            if (inviteButton != null)
            {
                inviteButton.onClick.RemoveAllListeners();
                inviteButton.onClick.AddListener(ShowInviteDialog);
            }

            if (roomNameApplyButton != null)
            {
                roomNameApplyButton.onClick.RemoveAllListeners();
                roomNameApplyButton.onClick.AddListener(ApplyRoomNameFromInput);
            }
        }

        private void BindNetworkStreams()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnPlayerListStream
                .ObserveOnMainThread()
                .Subscribe(_ => RefreshWaitRoomUi())
                .AddTo(this);

            networkManager.OnRoomSettingsChangedStream
                .ObserveOnMainThread()
                .Subscribe(_ => RefreshWaitRoomUi())
                .AddTo(this);

            networkManager.OnPlayerReadyStream
                .ObserveOnMainThread()
                .Subscribe(_ => RefreshWaitRoomUi())
                .AddTo(this);

            networkManager.OnPlayerJoinedStream
                .ObserveOnMainThread()
                .Subscribe(_ => RefreshWaitRoomUi())
                .AddTo(this);

            networkManager.OnPlayerLeftStream
                .ObserveOnMainThread()
                .Subscribe(_ => RefreshWaitRoomUi())
                .AddTo(this);

            networkManager.OnStartCountdownStream
                .ObserveOnMainThread()
                .Subscribe(StartCountdown)
                .AddTo(this);

            networkManager.OnCancelCountdownStream
                .ObserveOnMainThread()
                .Subscribe(_ => CancelCountdown())
                .AddTo(this);

            networkManager.OnRoomDeletedStream
                .ObserveOnMainThread()
                .Subscribe(_ => GoToLobby())
                .AddTo(this);

            networkManager.OnRoomNotFoundStream
                .ObserveOnMainThread()
                .Subscribe(_ => GoToLobby())
                .AddTo(this);

            networkManager.OnSelfKickedStream
                .ObserveOnMainThread()
                .Subscribe(_ => GoToLobby())
                .AddTo(this);
        }

        private void RefreshWaitRoomUi()
        {
            var room = ResolveWaitRoom();
            if (room == null)
            {
                SetText(roomTitleText, roomTitleTmpText, "Wait Room");
                SetText(gameModeText, gameModeTmpText, "");
                SetText(teamBalanceText, teamBalanceTmpText, "");
                InitializePlayerInfoUi();
                return;
            }

            roomOwner = IsLocalPlayerOwner(room);
            ApplyRoomTitle(room.RoomName, false);
            ApplyRoomCapacity(room.Capacity, false);
            ApplyGameMode(room.GameMode, false);
            SetTeamBalanceText();
            UpdateReadyButtonVisual();
            UpdateActionButtons();
            RenderPlayerSlots(room.PlayerList);
        }

        private void InitializePlayerInfoUi()
        {
            var slot = mySlot != null
                ? mySlot
                : playerSlotTemplate != null
                    ? playerSlotTemplate.GetComponent<WaitRoomPlayerInfoController>()
                    : null;

            if (slot == null)
            {
                return;
            }

            var player = BuildLocalPlaceholderPlayer();
            slot.gameObject.SetActive(true);
            slot.Bind(player, 0, ResolveWaitRoom()?.OwnerId, ResolveLocalPlayerId());
        }

        private static PlayerInfo BuildLocalPlaceholderPlayer()
        {
            var playerName = AccountManager.Instance.CurrentProfile.DisplayName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Player";
            }

            return new PlayerInfo(
                id: ResolveLocalPlayerId(),
                name: playerName,
                currentIp: AccountManager.Instance.CurrentProfile.GlobalMyIP)
            {
                IsReady = false,
                Team = OpenGSCore.ETeam.NoTeam,
                IsBot = false
            };
        }

        private void UpdateReadyButtonVisual()
        {
            if (readyButtonGraphic == null || networkManager == null)
            {
                return;
            }

            readyButtonGraphic.color = networkManager.IsReady
                ? new Color(0.35f, 0.85f, 0.45f, 1f)
                : Color.white;
        }

        private void UpdateActionButtons()
        {
            if (startButton != null)
            {
                startButton.gameObject.SetActive(roomOwner);
                startButton.interactable = roomOwner;
            }

            if (plusButton != null)
            {
                plusButton.interactable = roomOwner;
            }

            if (minusButton != null)
            {
                minusButton.interactable = roomOwner;
            }

            if (gameModeButton != null)
            {
                gameModeButton.interactable = roomOwner;
            }

            if (roomNameApplyButton != null)
            {
                roomNameApplyButton.interactable = roomOwner;
            }

            if (roomNameLegacyInputField != null)
            {
                roomNameLegacyInputField.interactable = roomOwner;
            }

            if (roomNameTmpInputField != null)
            {
                roomNameTmpInputField.interactable = roomOwner;
            }
        }

        private void RenderPlayerSlots(List<PlayerInfo> players)
        {
            players ??= new List<PlayerInfo>();

            var firstSlotObject = mySlot != null ? mySlot.gameObject : playerSlotTemplate;
            var templateObject = playerSlotTemplate != null ? playerSlotTemplate : firstSlotObject;
            var slotsRoot = waitRoomPlayerSlotsRoot;

            if (slotsRoot == null && firstSlotObject != null)
            {
                slotsRoot = firstSlotObject.transform != null && firstSlotObject.transform.parent != null
                    ? firstSlotObject.transform.parent
                    : firstSlotObject.transform;
            }

            if (slotsRoot == null || firstSlotObject == null || templateObject == null)
            {
                return;
            }

            if (players.Count == 0)
            {
                ConfigurePlayerSlot(firstSlotObject, BuildLocalPlaceholderPlayer(), 0);
                firstSlotObject.SetActive(true);
                for (var i = 0; i < activePlayerSlotObjects.Count; i++)
                {
                    activePlayerSlotObjects[i].SetActive(false);
                }
                return;
            }

            firstSlotObject.SetActive(true);

            var primaryPlayerIndex = ResolvePrimaryPlayerIndex(players);
            var orderedPlayers = OrderPlayersForDisplay(players, primaryPlayerIndex);
            var extraSlotCount = Mathf.Max(0, orderedPlayers.Count - 1);
            while (activePlayerSlotObjects.Count < extraSlotCount)
            {
                var clone = Instantiate(templateObject, slotsRoot);
                clone.name = $"PlayerSlot_{activePlayerSlotObjects.Count + 1}";
                clone.SetActive(true);
                activePlayerSlotObjects.Add(clone);
            }

            for (var i = 0; i < activePlayerSlotObjects.Count; i++)
            {
                var slotObject = activePlayerSlotObjects[i];
                var shouldShow = i < extraSlotCount;
                slotObject.SetActive(shouldShow);
                if (shouldShow)
                {
                    ConfigurePlayerSlot(slotObject, orderedPlayers[i + 1], i + 1);
                }
            }

            if (orderedPlayers.Count > 0)
            {
                ConfigurePlayerSlot(firstSlotObject, orderedPlayers[0], 0);
            }
        }

        private void ConfigurePlayerSlot(GameObject slotObject, PlayerInfo player, int index)
        {
            if (slotObject == null || player == null)
            {
                return;
            }

            var controller = slotObject.GetComponent<WaitRoomPlayerInfoController>();
            if (controller != null)
            {
                controller.Bind(player, index, ResolveWaitRoom()?.OwnerId, ResolveLocalPlayerId());
                return;
            }

            var rectTransform = slotObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 12f - (index * 108f));
            }

            var legacyText = slotObject.GetComponentInChildren<Text>(true);
            if (legacyText != null)
            {
                legacyText.text = BuildPlayerLine(player);
            }

            var tmpText = slotObject.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                tmpText.text = BuildPlayerLine(player);
            }

            ToggleNamedChild(slotObject.transform, "Host", string.Equals(player.Id, ResolveWaitRoom()?.OwnerId, System.StringComparison.OrdinalIgnoreCase));
            ToggleNamedChild(slotObject.transform, "Ready", player.IsReady);
        }

        private int ResolvePrimaryPlayerIndex(List<PlayerInfo> players)
        {
            if (players == null || players.Count == 0)
            {
                return -1;
            }

            var localPlayerId = ResolveLocalPlayerId();
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null)
                {
                    continue;
                }

                if (string.Equals(player.Id, localPlayerId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return 0;
        }

        private static List<PlayerInfo> OrderPlayersForDisplay(List<PlayerInfo> players, int primaryPlayerIndex)
        {
            var orderedPlayers = new List<PlayerInfo>(players.Count);

            if (players == null || players.Count == 0)
            {
                return orderedPlayers;
            }

            if (primaryPlayerIndex >= 0 && primaryPlayerIndex < players.Count && players[primaryPlayerIndex] != null)
            {
                orderedPlayers.Add(players[primaryPlayerIndex]);
            }

            for (var i = 0; i < players.Count; i++)
            {
                if (i == primaryPlayerIndex)
                {
                    continue;
                }

                if (players[i] != null)
                {
                    orderedPlayers.Add(players[i]);
                }
            }

            return orderedPlayers;
        }

        private void ToggleReadyState()
        {
            if (networkManager == null)
            {
                return;
            }

            Ready(!networkManager.IsReady);
        }

        private void RequestGameStart()
        {
            if (networkManager == null || !roomOwner)
            {
                return;
            }

            SendGameStartRequest();
        }

        private void StartCountdown(int seconds)
        {
            CancelCountdown();
            startCountdownCoroutine = StartCoroutine(StartCountdownCoroutine(seconds));
        }

        private void CancelCountdown()
        {
            if (startCountdownCoroutine == null)
            {
                return;
            }

            StopCoroutine(startCountdownCoroutine);
            startCountdownCoroutine = null;
        }

        private IEnumerator StartCountdownCoroutine(int seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0, seconds));
            startCountdownCoroutine = null;
            LoadGameScene();
        }

        private ClientWaitRoom ResolveWaitRoom()
        {
            return DependencyInjectionConfig.Resolve<WaitRoomManager>()?.WaitRoom;
        }

        private bool IsLocalPlayerOwner(ClientWaitRoom room)
        {
            if (room == null)
            {
                return false;
            }

            return string.Equals(room.OwnerId, ResolveLocalPlayerId(), System.StringComparison.OrdinalIgnoreCase);
        }

        private string BuildRoomTitle()
        {
            var room = ResolveWaitRoom();
            if (room == null)
            {
                return "Wait Room";
            }

            return $"{room.RoomName}  {room.PlayerCount}/{room.Capacity}";
        }

        private static void SetText(Text legacyText, TMP_Text tmpText, string value)
        {
            if (legacyText != null)
            {
                legacyText.text = value;
            }

            if (tmpText != null)
            {
                tmpText.text = value;
            }
        }

        private void SetTeamBalanceText()
        {
            var room = ResolveWaitRoom();
            var value = room != null && room.TeamBalance ? "Team Balance: ON" : "Team Balance: OFF";
            SetText(teamBalanceText, teamBalanceTmpText, value);
        }

        private static void ToggleNamedChild(Transform root, string childName, bool active)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == childName)
                {
                    child.gameObject.SetActive(active);
                }
            }
        }

        private static string BuildPlayerLine(PlayerInfo player)
        {
            return player.IsReady ? $"{player.Name} [Ready]" : player.Name;
        }

        private static GameObject FindInactiveGameObject(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.name != objectName)
                {
                    continue;
                }

                if (!candidate.scene.IsValid())
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static T FindInactiveComponent<T>(string objectName) where T : Component
        {
            var gameObject = FindInactiveGameObject(objectName);
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        private static string ResolveLocalPlayerId()
        {
            var playerId = AccountManager.Instance.CurrentProfile.GlobalUserId;
            return string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
        }

        private void ShowInviteDialog()
        {
            AutoBindIfNeeded();

            if (inviteDialog == null)
            {
                Debug.LogWarning("[OnlineWaitRoomScene] InviteDialog was not found.");
                return;
            }

            var room = ResolveWaitRoom();
            var roomId = room != null ? room.RoomId : string.Empty;
            var roomName = room != null ? room.RoomName : BuildRoomTitle();
            inviteDialog.Show(roomId, roomName);
        }

        private void ApplyRoomNameFromInput()
        {
            if (!IsRoomOwner())
            {
                return;
            }

            var roomName = roomNameTmpInputField != null ? roomNameTmpInputField.text : null;
            if (string.IsNullOrWhiteSpace(roomName) && roomNameLegacyInputField != null)
            {
                roomName = roomNameLegacyInputField.text;
            }

            if (string.IsNullOrWhiteSpace(roomName) && inputField != null)
            {
                roomName = inputField.text;
            }

            if (string.IsNullOrWhiteSpace(roomName) && text != null)
            {
                roomName = text.text;
            }

            if (string.IsNullOrWhiteSpace(roomName))
            {
                return;
            }

            ChangeRoomTitle(roomName.Trim());
        }

        private static EGameMode ResolveNextGameMode(EGameMode current)
        {
            switch (current)
            {
                case EGameMode.DeathMatch:
                    return EGameMode.TeamDeathMatch;
                case EGameMode.TeamDeathMatch:
                    return EGameMode.CaptureTheFlag;
                case EGameMode.CaptureTheFlag:
                    return EGameMode.Survival;
                case EGameMode.Survival:
                    return EGameMode.TeamSurvival;
                case EGameMode.TeamSurvival:
                    return EGameMode.DeathMatch;
                default:
                    return EGameMode.DeathMatch;
            }
        }

        private void SendWaitRoomSettingsChange(JObject settings)
        {
            if (networkManager == null || settings == null)
            {
                return;
            }

            networkManager.SendWaitRoomSettingsChange(settings);
        }
    }
}
