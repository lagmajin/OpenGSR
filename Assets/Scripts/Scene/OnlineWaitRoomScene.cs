#pragma warning disable 0219
#pragma warning disable 0105

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
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
        private GeneralServerNetworkManager generalNetworkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
        public Button chara;
        public Button map;

        [SerializeField] public InputField inputField;
        [SerializeField] public Text text;

        [Required] public WaitRoomNetworkManager networkManager;

        [SerializeField] [Required] private WaitRoomMediateObject mediateObject;
        [SerializeField] private GameObject weaponLimitDialog;
        [SerializeField] private Button weaponLimitButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Graphic readyButtonGraphic;
        [SerializeField] private Text roomTitleText;
        [SerializeField] private TMP_Text roomTitleTmpText;
        [SerializeField] private Text gameModeText;
        [SerializeField] private TMP_Text gameModeTmpText;
        [SerializeField] private Transform waitRoomPlayerSlotsRoot;
        [SerializeField] private GameObject playerSlotTemplate;

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
            AutoBindIfNeeded();
            SetupListeners();
        }

        void Start()
        {
            PlayWaitRoomBgm();

            timer.timeupEvent.AddListener(TimeUp);

            LoadRoomSetting();
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
        }

        public void ChangeGameMode(EGameMode mode)
        {
            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.GameMode = mode;
            }

            SetText(gameModeText, gameModeTmpText, mode.ToString());
        }

        public void ChangeMap(EMap map)
        {
            Debug.Log("Map " + map);
        }

        public void ChangeTeamBalance(bool balance)
        {
            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.TeamBalance = balance;
            }
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
            if (networkManager == null)
            {
                return;
            }

            if (ready)
            {
                networkManager.SendReady();
            }
            else
            {
                networkManager.SendUnready();
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
        }

        public void Minus()
        {
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
            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.RoomName = roomTitle ?? "";
            }

            SetText(roomTitleText, roomTitleTmpText, BuildRoomTitle());
        }

        public void ChangeRoomCapacity(int capacity)
        {
            var room = ResolveWaitRoom();
            if (room != null)
            {
                room.Capacity = capacity;
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

            uiManager.ChangeRoomTitle(room.RoomName);
            uiManager.ChangeRoomCapacity(room.Capacity);
            uiManager.ChangeGameMode(room.GameMode);
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

            if (readyButtonGraphic == null && readyButton != null)
            {
                readyButtonGraphic = readyButton.targetGraphic;
            }

            roomTitleText ??= FindInactiveComponent<Text>("RoomTitle");
            roomTitleTmpText ??= FindInactiveComponent<TMP_Text>("RoomTitle");
            gameModeText ??= FindInactiveComponent<Text>("GameModeText");
            gameModeTmpText ??= FindInactiveComponent<TMP_Text>("GameModeText");

            if (waitRoomPlayerSlotsRoot == null)
            {
                var rootObject = FindInactiveGameObject("WaitroomPlayerSlots");
                waitRoomPlayerSlotsRoot = rootObject != null ? rootObject.transform : null;
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
        }

        private void RefreshWaitRoomUi()
        {
            var room = ResolveWaitRoom();
            if (room == null)
            {
                SetText(roomTitleText, roomTitleTmpText, "Wait Room");
                SetText(gameModeText, gameModeTmpText, "");
                return;
            }

            roomOwner = IsLocalPlayerOwner(room);
            ChangeRoomTitle(room.RoomName);
            ChangeRoomCapacity(room.Capacity);
            ChangeGameMode(room.GameMode);
            UpdateReadyButtonVisual();
            UpdateActionButtons();
            RenderPlayerSlots(room.PlayerList);
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
        }

        private void RenderPlayerSlots(List<PlayerInfo> players)
        {
            if (waitRoomPlayerSlotsRoot == null || playerSlotTemplate == null)
            {
                return;
            }

            playerSlotTemplate.SetActive(players.Count > 0);

            var extraSlotCount = Mathf.Max(0, players.Count - 1);
            while (activePlayerSlotObjects.Count < extraSlotCount)
            {
                var clone = Instantiate(playerSlotTemplate, waitRoomPlayerSlotsRoot);
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
                    ConfigurePlayerSlot(slotObject, players[i + 1], i + 1);
                }
            }

            if (players.Count > 0)
            {
                ConfigurePlayerSlot(playerSlotTemplate, players[0], 0);
            }
        }

        private void ConfigurePlayerSlot(GameObject slotObject, PlayerInfo player, int index)
        {
            if (slotObject == null || player == null)
            {
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

            networkManager.SendGameStart();
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
    }
}
