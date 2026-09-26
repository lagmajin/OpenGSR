using OpenGSCore;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

#pragma warning disable 0414
#pragma warning disable 0219

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OnlineLoadingScene : AbstractLoadingScene, IOnlineLoadingScene
    {
        private bool loadImmediately = true;
        private float count = 0.0f;
        private float timeout = 10.0f;
        static bool loadingErrorFlag = false;
        public LoadingSpriteBGMasterData bgMasterData;

        [SerializeField] [Required] [SceneObjectsOnly] public OnlineLoadingSceneMediateObject mediateObject;

        [Inject] private OnlineLoadingManager onlineLoadingManager;

        [SerializeField] public OnlineLoadingSceneNetworkManager networkManager;

        private readonly ReactiveProperty<float> progress = new ReactiveProperty<float>(0f);
        public IReadOnlyReactiveProperty<float> Progress => progress;

        private AsyncOperation _sceneLoadOp;
        private Coroutine loadingCoroutine;
        private readonly HashSet<string> completedPlayerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool localLoadingCompleted;
        private bool enterMapAllowed;
        private bool loadingStarted;
        private bool returningToWaitRoom;

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            Application.targetFrameRate = SettingsManager.Instance.GetGraphicsSettings().TargetFrameRate;
            AutoBindIfNeeded();
        }

        private void Start()
        {
            if (onlineLoadingManager == null)
            {
                onlineLoadingManager = OnlineLoadingManager.Instance;
                Debug.LogWarning("[OnlineLoadingScene] OnlineLoadingManager was not injected; using shared fallback instance.");
            }

            loadingErrorFlag = false;
            completedPlayerIds.Clear();
            localLoadingCompleted = false;
            enterMapAllowed = false;
            loadingStarted = false;
            returningToWaitRoom = false;
            count = 0f;
            EnsureLoadingBgm();

            onlineLoadingManager?.Clear();
            networkManager?.BeginLoadingSession(GetExpectedPlayerCount());
            networkManager?.SendLoadingSceneEntered();
            TryConnectToMatchServer();

            StartLoadingCoroutine();
        }

        protected override void OnDestroy()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
            progress.Dispose();
            base.OnDestroy();
        }

        void Reset()
        {
            AutoBindIfNeeded();
        }

        protected override void Update()
        {
            base.Update();
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            count = (float.IsFinite(count) ? count : 0f) + deltaTime;
            // The pre-load timeout protects against a stalled connection or room setup.
            // Once Unity owns an AsyncOperation, map loading may legitimately exceed
            // the short handshake timeout on slower devices.
            if (!returningToWaitRoom && _sceneLoadOp == null && count >= timeout)
            {
                BackToWaitRoom();
            }
        }

        private void EnsureLoadingBgm()
        {
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning("[OnlineLoadingScene] SoundManager is not ready; skipping loading BGM.");
                return;
            }

            if (SoundManager.Instance.IsBgmPlaying(EBgm.WaitRoom))
            {
                return;
            }

            if (!SoundManager.Instance.IsBgmPlaying())
            {
                SoundManager.Instance.EnsureBgm(EBgm.WaitRoom, 0f);
            }
        }

        private IEnumerator Loading()
        {
            PrettyLogger.Bold("Network", "LoadingStart");

            var roomManager = MatchRoomManager();
            if (roomManager == null)
            {
                OnLoadingFailed();
                yield break;
            }

            roomManager.CreateNewOnlineMatchRoom();
            yield return new WaitForSecondsRealtime(1);

            var onlineSelection = ResolveOnlineSelection();
            var selectedMap = ResolveSelectedMap(onlineSelection);
            var selectedMode = ResolveSelectedGameMode(onlineSelection);
            var mapInfo = ResolveMapInfo(selectedMap);
            if (mapInfo == null)
            {
                Debug.LogWarning($"[OnlineLoadingScene] Map info not found for {selectedMap}");
                OnLoadingFailed();
                yield break;
            }

            var mapSceneName = mapInfo.MapScene();
            if (string.IsNullOrWhiteSpace(mapSceneName) || !Application.CanStreamedLevelBeLoaded(mapSceneName))
            {
                Debug.LogError($"[OnlineLoadingScene] Map scene is not available in build settings: {mapSceneName}");
                OnLoadingFailed();
                yield break;
            }

            if (onlineLoadingManager == null)
            {
                Debug.LogError("[OnlineLoadingScene] OnlineLoadingManager is not available.");
                OnLoadingFailed();
                yield break;
            }

            onlineLoadingManager.SetLoadingInfo(mapSceneName, selectedMode);
            if (GameModeSelectManager.Instance != null)
            {
                GameModeSelectManager.Instance.OnlineGameSelect = new OnlineGameModeSelect
                {
                    GameMode = selectedMode,
                    Map = selectedMap,
                    TeamBalance = onlineSelection?.TeamBalance ?? true
                };
            }

            _sceneLoadOp = SceneManager.LoadSceneAsync(mapSceneName, LoadSceneMode.Single);
            if (_sceneLoadOp == null)
            {
                OnLoadingFailed();
                yield break;
            }

            _sceneLoadOp.allowSceneActivation = false;
            networkManager?.SendLoadingStart();

            while (_sceneLoadOp.progress < 0.9f)
            {
                var loadProgress = Mathf.Clamp01(_sceneLoadOp.progress / 0.9f);
                progress.Value = loadProgress;
                networkManager?.SendLoadingProgress(loadProgress);
                yield return null;
            }

            progress.Value = 1f;
            networkManager?.SendLoadingProgress(1f);
            localLoadingCompleted = true;
            networkManager?.SendLoadingComplete();
            TryEnterMap();

            yield return new WaitUntil(() => _sceneLoadOp.allowSceneActivation);
            yield return new WaitUntil(() => _sceneLoadOp.isDone);
        }

        public void TryConnectToMatchServer()
        {
            if (networkManager == null)
            {
                return;
            }

            if (OnlineManager.Instance == null)
            {
                Debug.LogWarning("[OnlineLoadingScene] OnlineManager is not ready; skipping match server request.");
                return;
            }

            if (!OnlineManager.Instance.MatchServerInfo.HasEndpoint())
            {
                networkManager.SendMatchServerInfoRequest();
            }
        }

        public void OnMatchLoadingCompleted()
        {
            TryEnterMap();
        }

        public void OnMatchLoadingCompleted(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                completedPlayerIds.Add(playerId);
            }

            TryEnterMap();
        }

        public void OnMatchServerConnected()
        {
            TryConnectToMatchServer();
        }

        public void OnLoadingFailed()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
            loadingStarted = false;
            BackToWaitRoom();
        }

        void GoToBattleScene()
        {
            TryEnterMap();
        }

        void BackToWaitRoom()
        {
            if (returningToWaitRoom)
            {
                return;
            }

            returningToWaitRoom = true;
            loadingErrorFlag = true;
            var sceneName = GeneralSceneMasterData.Instance().OnlineWaitRoomScene();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                returningToWaitRoom = false;
                Debug.LogError("[OnlineLoadingScene] Online wait room scene is not configured.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                returningToWaitRoom = false;
                Debug.LogError($"[OnlineLoadingScene] Online wait room scene is not in build settings: {sceneName}");
                return;
            }

            if (SceneManager.LoadSceneAsync(sceneName) == null)
            {
                returningToWaitRoom = false;
                Debug.LogError($"[OnlineLoadingScene] Failed to load wait room scene: {sceneName}");
            }
        }

        void SendChat(string message)
        {
            networkManager?.SendLoadingMessage(message);
        }

        public void ParseServerMessage()
        {
            Debug.Log("[OnlineLoadingScene] ParseServerMessage invoked.");
        }

        [Button("ローディング")]
        public void LoadingScene(EGameMode mode = EGameMode.DeathMatch, EMap map = EMap.DryDays)
        {
            var select = new OnlineGameModeSelect
            {
                GameMode = mode,
                Map = map
            };

            if (GameModeSelectManager.Instance == null)
            {
                Debug.LogWarning("[OnlineLoadingScene] GameModeSelectManager is not ready.");
                return;
            }

            GameModeSelectManager.Instance.OnlineGameSelect = select;
            StartLoadingCoroutine();
        }

        private void StartLoadingCoroutine()
        {
            if (loadingStarted)
            {
                return;
            }

            loadingStarted = true;
            loadingCoroutine = StartCoroutine(Loading());
        }

        [Button("デバッグ選択")]
        public void CreateDebugSelect(EGameMode mode, EMap map)
        {
            var select = new OnlineGameModeSelect
            {
                GameMode = mode,
                Map = map
            };

            var instance = GameModeSelectManager.Instance;
            instance.OnlineGameSelect = select;
            instance.SaveDebugOnlineSelectToFile();
        }

        [Button("デバッグステージへ")]
        public void GoToTestScene()
        {
            var mapInfo = ResolveMapInfo(ResolveSelectedMap());
            if (mapInfo != null)
            {
                var sceneName = mapInfo.MapScene();
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    Debug.LogError("[OnlineLoadingScene] Test map scene is not configured.");
                    return;
                }

                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.LogError($"[OnlineLoadingScene] Test map scene is not in build settings: {sceneName}");
                    return;
                }

                SceneManager.LoadScene(sceneName);
            }
        }

        protected override void OnStartUnityEditor()
        {
            AutoBindIfNeeded();
            EnsureLoadingBgm();
        }

        protected override void OnStartFromEditorDirectly()
        {
            PrettyLogger.Log("System", "Test");
            IsOnlineMode = true;
            AutoBindIfNeeded();
        }

        public void OnEnterMapAllowed()
        {
            enterMapAllowed = true;
            TryEnterMap();
        }

        private void TryEnterMap()
        {
            if (!localLoadingCompleted || !enterMapAllowed || _sceneLoadOp == null)
            {
                return;
            }

            var expectedPlayers = GetExpectedPlayerCount();
            if (completedPlayerIds.Count < expectedPlayers)
            {
                Debug.Log($"[OnlineLoadingScene] Waiting for players to finish loading: {completedPlayerIds.Count}/{expectedPlayers}");
                return;
            }

            _sceneLoadOp.allowSceneActivation = true;
        }

        private int GetExpectedPlayerCount()
        {
            try
            {
                var waitRoomManager = DependencyInjectionConfig.Resolve<WaitRoomManager>();
                var waitRoom = waitRoomManager?.WaitRoom;
                if (waitRoom != null && waitRoom.PlayerCount > 0)
                {
                    return waitRoom.PlayerCount;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnlineLoadingScene] Failed to resolve expected player count: {ex.Message}");
            }

            return 1;
        }

        private void AutoBindIfNeeded()
        {
            if (!mediateObject)
            {
                mediateObject = FindFirstObjectByType<OnlineLoadingSceneMediateObject>();
            }

            if (!networkManager)
            {
                networkManager = FindFirstObjectByType<OnlineLoadingSceneNetworkManager>();
            }
        }

        private OnlineGameModeSelect ResolveOnlineSelection()
        {
            try
            {
                var waitRoom = DependencyInjectionConfig.Resolve<WaitRoomManager>()?.WaitRoom;
                if (waitRoom != null)
                {
                    return new OnlineGameModeSelect
                    {
                        GameMode = waitRoom.GameMode,
                        Map = waitRoom.Map,
                        TeamBalance = waitRoom.TeamBalance
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnlineLoadingScene] Failed to resolve online selection from wait room: {ex.Message}");
            }

            return GameModeSelectManager.Instance?.OnlineGameSelect;
        }

        private EMap ResolveSelectedMap(OnlineGameModeSelect selected = null)
        {
            if (selected != null && selected.Map != EMap.Unknown)
            {
                return selected.Map;
            }

            var room = ResolveWaitRoom();
            if (room != null && room.Map != EMap.Unknown)
            {
                return room.Map;
            }

            return EMap.DryDays;
        }

        private EGameMode ResolveSelectedGameMode(OnlineGameModeSelect selected = null)
        {
            if (selected != null && selected.GameMode != EGameMode.Unknown)
            {
                return selected.GameMode;
            }

            var room = ResolveWaitRoom();
            if (room != null && room.GameMode != EGameMode.Unknown)
            {
                return room.GameMode;
            }

            return EGameMode.DeathMatch;
        }

        private ClientWaitRoom ResolveWaitRoom()
        {
            try
            {
                return DependencyInjectionConfig.Resolve<WaitRoomManager>()?.WaitRoom;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnlineLoadingScene] Failed to resolve wait room: {ex.Message}");
                return null;
            }
        }

        private MapInfoMasterData ResolveMapInfo(EMap map)
        {
            if (mediateObject != null && mediateObject.MapSceneMasterData() != null)
            {
                return mediateObject.MapSceneMasterData().Map(map);
            }

            if (mapSelectMasterData != null)
            {
                return mapSelectMasterData.Map(map);
            }

            Debug.LogWarning($"[OnlineLoadingScene] Map info could not be resolved for {map}.");
            return null;
        }
    }
}
