using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameTimer))]
    public abstract class AbstractScene : SerializedMonoBehaviour, IAbstractScene, ISceneInputReceiver, ISceneLoadReceiver
    {
        private static bool shouldPlayWarningSound = false;
        private SynchronizationContext currentThread;

        private CancellationTokenSource sceneLifetimeCts;
        private readonly HashSet<Coroutine> managedCoroutines = new HashSet<Coroutine>();
        private readonly HashSet<string> pendingSceneTransitions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private AsyncOperation currentSceneOperation;
        private bool isSceneTransitionInProgress;

        [SerializeField] [Required] public SystemSoundMasterData systemSoundMasterData;
        [SerializeField] [Required] public SoundMasterData soundMasterData;
        [SerializeField] [Required] public BGMMasterData bgmMasterData;
        [SerializeField] [Required] public GeneralSceneMasterData generalSceneMasterData;
        [SerializeField] [Required] protected GameTimer timer;
        [SerializeField] protected AbstractSceneController sceneController;
        [SerializeField] protected AbstractMediateObject sceneMediateObject;

        protected GameGeneralManager _gameGeneralManager;

        [ShowInInspector]
        public bool IsOnlineMode
        {
            get => _gameGeneralManager != null ? _gameGeneralManager.IsOnlineGameMode : GameGeneralManager.GetInstance.IsOnlineGameMode;
            set
            {
                if (_gameGeneralManager != null) _gameGeneralManager.IsOnlineGameMode = value;
                else GameGeneralManager.GetInstance.IsOnlineGameMode = value;
            }
        }

        public bool IsOfflineMode => !IsOnlineMode;
        public bool IsSceneTransitionInProgress => isSceneTransitionInProgress;

        public void SetOnlineMode(bool value)
        {
            IsOnlineMode = value;
        }

        protected virtual void Awake()
        {
            RestartSceneLifetimeToken();

            try
            {
                _gameGeneralManager = DependencyInjectionConfig.Resolve<GameGeneralManager>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"AbstractScene.Awake: Failed to resolve GameGeneralManager: {ex.Message}");
            }

            ValidateSceneComposition(true);
        }

        protected virtual void Update()
        {
            if (Input.anyKeyDown)
            {
                KeyPress();
            }
        }

        [Button("スクリーンショット")]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InvokeIfDirectPlay()
        {
#if UNITY_EDITOR
            string startingScene = EditorSceneManager.GetActiveScene().path;
            string currentScene = SceneManager.GetActiveScene().path;

            if (startingScene == currentScene)
            {
                var targets = GameObject.FindObjectsByType<AbstractScene>(FindObjectsSortMode.None);
                foreach (var t in targets)
                {
                    t.OnStartFromEditorDirectly();
                }
            }
#endif
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        private static void HandleEditorRegistry()
        {
            EditorApplication.delayCall -= HandleDelayCall;
            EditorApplication.delayCall += HandleDelayCall;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void HandleDelayCall()
        {
            var targets = GameObject.FindObjectsByType<AbstractScene>(FindObjectsSortMode.None);
            foreach (var t in targets)
            {
                t.OnStartUnityEditor();
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    var scenes = GameObject.FindObjectsByType<AbstractScene>(FindObjectsSortMode.None);
                    foreach (var scene in scenes)
                    {
                        scene.OnQuitUnityEditor();
                    }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    break;
            }
        }
#endif

        protected virtual void OnStartUnityEditor()
        {
        }

        protected virtual void OnQuitUnityEditor()
        {
        }

        protected virtual void OnStartFromEditorDirectly()
        {
        }

        public abstract SynchronizationContext MainThread();

        public SynchronizationContext MainThread2()
        {
            return currentThread;
        }

        public void SaveScreenShot()
        {
            string date = DateTime.Now.ToString("yy-MM-dd_HH-mm-ss");
            string fileName = Path.Combine(Application.dataPath, $"screenshot_{date}.png");
            ScreenCapture.CaptureScreenshot(fileName);
        }

        protected virtual void EventProcess(string eventName)
        {
            Debug.Log($"[{GetType().Name}] EventProcess: {eventName}");
        }

        public void SendEvent(string str)
        {
            EventProcess(str);
        }

        protected CancellationToken SceneLifetimeToken =>
            sceneLifetimeCts != null ? sceneLifetimeCts.Token : CancellationToken.None;

        protected Coroutine StartManagedCoroutine(IEnumerator routine)
        {
            if (routine == null)
            {
                Debug.LogWarning($"{GetType().Name}: StartManagedCoroutine received null routine.");
                return null;
            }

            Coroutine c = null;
            IEnumerator RunManagedRoutine()
            {
                try
                {
                    yield return routine;
                }
                finally
                {
                    if (c != null)
                    {
                        managedCoroutines.Remove(c);
                    }
                }
            }

            c = StartCoroutine(RunManagedRoutine());
            if (c != null)
            {
                managedCoroutines.Add(c);
            }
            return c;
        }

        protected void StopManagedCoroutine(Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            if (managedCoroutines.Contains(coroutine))
            {
                StopCoroutine(coroutine);
                managedCoroutines.Remove(coroutine);
            }
        }

        protected virtual void OnBeforeSceneChange(string nextSceneName)
        {
        }

        protected virtual void OnAfterSceneLoaded(string loadedSceneName, LoadSceneMode mode)
        {
        }

        protected virtual void OnSceneTransitionBlocked(string nextSceneName)
        {
            Debug.LogWarning($"{GetType().Name}: scene transition blocked while another transition is in progress. next={nextSceneName}");
        }

        protected AsyncOperation GoToScene(string nextSceneName, bool additive = false)
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning($"{GetType().Name}: next scene name is empty.");
                return null;
            }

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"{GetType().Name}: scene is not in build settings: {nextSceneName}");
                return null;
            }

            if (isSceneTransitionInProgress)
            {
                OnSceneTransitionBlocked(nextSceneName);
                return currentSceneOperation;
            }

            isSceneTransitionInProgress = true;
            RestartSceneLifetimeToken();
            OnBeforeSceneChange(nextSceneName);

            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            currentSceneOperation = SceneManager.LoadSceneAsync(nextSceneName, mode);
            if (currentSceneOperation != null)
            {
                currentSceneOperation.completed += _ =>
                {
                    isSceneTransitionInProgress = false;
                    currentSceneOperation = null;
                    OnAfterSceneLoaded(nextSceneName, mode);
                };
            }
            else
            {
                isSceneTransitionInProgress = false;
            }

            return currentSceneOperation;
        }

        protected bool HandleEscapeToBackScene(Action onBack = null, KeyCode key = KeyCode.Escape)
        {
            if (!Input.GetKeyDown(key))
            {
                return false;
            }

            if (onBack != null)
            {
                onBack.Invoke();
            }
            else
            {
                GoToTitleScene();
            }
            return true;
        }

        protected void ResetIdleTimer()
        {
            timer?.ReStartTimer();
        }

        public GameGeneralManager GameManager()
        {
            return _gameGeneralManager ?? GameGeneralManager.GetInstance;
        }

        protected TController SceneController<TController>() where TController : class
        {
            return sceneController as TController;
        }

        protected TMediate SceneMediate<TMediate>() where TMediate : class
        {
            return sceneMediateObject as TMediate;
        }

        protected bool ValidateSceneComposition(bool logWarnings = true)
        {
            bool hasController = sceneController != null;
            bool hasMediate = sceneMediateObject != null;
            bool valid = hasController || hasMediate;

            if (logWarnings && !valid)
            {
                Debug.LogWarning($"{GetType().Name}: both sceneController and sceneMediateObject are not assigned. Scene responsibilities may be mixed.");
            }

            return valid;
        }

        public bool IsMatchMode()
        {
            return false;
        }

        public bool IsOnlineModeOld()
        {
            return IsOnlineMode;
        }

        public bool IsOfflineModeOld()
        {
            return !IsOnlineModeOld();
        }

        public void DisconnectFromServer()
        {
            try
            {
                DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>()?.Disconnect();
            }
            catch
            {
            }
        }

        public void GoToSplashScreen()
        {
            var splashScene = generalSceneMasterData != null ? generalSceneMasterData.SplashScene() : GeneralSceneMasterData.Instance().SplashScene();
            GoToScene(splashScene);
        }

        public void GoToTitleScene()
        {
            var titleScene = generalSceneMasterData != null ? generalSceneMasterData.TitleScene() : GeneralSceneMasterData.Instance().TitleScene();
            Debug.Log("タイトルシーンへ移動");
            GoToScene(titleScene);
        }

        public void GoToTitleSceneWithErrorSound()
        {
            var titleScene = generalSceneMasterData != null ? generalSceneMasterData.TitleScene() : GeneralSceneMasterData.Instance().TitleScene();
            Debug.Log("タイトルシーンへ移動（エラー警告音付き）");

            shouldPlayWarningSound = true;
            SceneManager.sceneLoaded += OnTitleSceneLoadedForWarning;
            GoToScene(titleScene);
        }

        public void GoToShopScene()
        {
            var shopScene = generalSceneMasterData != null ? generalSceneMasterData.ShopScene() : GeneralSceneMasterData.Instance().ShopScene();
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            GoToScene(shopScene);
        }

        private void OnTitleSceneLoadedForWarning(Scene scene, LoadSceneMode mode)
        {
            var titleScene = generalSceneMasterData != null ? generalSceneMasterData.TitleScene() : GeneralSceneMasterData.Instance().TitleScene();
            if (scene.name == titleScene && shouldPlayWarningSound)
            {
                Debug.Log("タイトルシーンに到達、警告音を再生");
                shouldPlayWarningSound = false;
                SceneManager.sceneLoaded -= OnTitleSceneLoadedForWarning;
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            Debug.Log($"[{GetType().Name}] Application focus: {focus}");
        }

        private void OnApplicationPause(bool pause)
        {
            Debug.Log($"[{GetType().Name}] Application pause: {pause}");
        }

        public void GoToOfflineWaitRoom()
        {
            var offlineWaitRoom = generalSceneMasterData != null
                ? generalSceneMasterData.OfflineWaitRoomScene()
                : GeneralSceneMasterData.Instance().OfflineWaitRoomScene();
            GoToScene(offlineWaitRoom);
        }

        public void KeyPress()
        {
            ResetIdleTimer();
        }

        public virtual void GoToLobby()
        {
            var lobbyScene = generalSceneMasterData != null
                ? generalSceneMasterData.LobbyScene()
                : GeneralSceneMasterData.Instance().LobbyScene();

            RequestSceneTransition(lobbyScene, "GoToLobby");
        }

        protected void RequestSceneTransition(string nextSceneName, string reason = "")
        {
            RequestSceneTransition(nextSceneName, null, reason);
        }

        protected void RequestSceneTransition(string nextSceneName, Action onApproved, string reason = "")
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning($"{GetType().Name}: scene transition target is empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"{GetType().Name}: scene is not in build settings: {nextSceneName}");
                return;
            }

            GeneralServerNetworkManager networkManager;
            try
            {
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch
            {
                networkManager = null;
            }

            if (networkManager == null)
            {
                Debug.LogWarning($"{GetType().Name}: GeneralServerNetworkManager not available, loading {nextSceneName} directly.");
                GoToScene(nextSceneName);
                return;
            }

            var fromSceneName = SceneManager.GetActiveScene().name;

            var transitionKey = $"{fromSceneName}\n{nextSceneName}";
            if (!pendingSceneTransitions.Add(transitionKey))
            {
                Debug.LogWarning($"{GetType().Name}: scene transition request already pending. next={nextSceneName}");
                return;
            }

            GameFlagsManager.GetInstance().BeforeSceneName = fromSceneName;
            var playerId = AccountManager.Instance?.CurrentProfile?.GlobalUserId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = "local_player";
            }

            var request = new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.SceneTransitionRequest,
                ["FromScene"] = fromSceneName,
                ["ToScene"] = nextSceneName,
                ["Reason"] = reason ?? string.Empty,
                ["PlayerID"] = playerId
            };

            networkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Where(json =>
                {
                    var messageType = OpenGSCore.MessageType.Normalize(json?["MessageType"]?.ToString());
                    if (!string.Equals(messageType, OpenGSCore.MessageType.SceneTransitionResponse, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    var responseFromScene = json?["FromScene"]?.ToString() ?? string.Empty;
                    var responseToScene = json?["ToScene"]?.ToString() ?? string.Empty;
                    return string.Equals(responseFromScene, fromSceneName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(responseToScene, nextSceneName, StringComparison.OrdinalIgnoreCase);
                })
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(5))
                .Subscribe(json =>
                {
                    pendingSceneTransitions.Remove(transitionKey);
                    var approved = json?["Approved"]?.ToObject<bool>() ?? false;
                    if (approved)
                    {
                        if (onApproved != null)
                        {
                            try
                            {
                                onApproved();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[{GetType().Name}] Scene transition approval callback failed: {ex}");
                            }
                        }
                        GoToScene(nextSceneName);
                        return;
                    }

                    var denialReason = json?["Reason"]?.ToString() ?? "Denied by server";
                    OnSceneTransitionDenied(nextSceneName, denialReason);
                }, ex =>
                {
                    pendingSceneTransitions.Remove(transitionKey);
                    Debug.LogWarning($"[{GetType().Name}] Scene transition response timed out or failed. target={nextSceneName}, error={ex.Message}");
                    OnSceneTransitionDenied(nextSceneName, "Transition response timed out.");
                })
                .AddTo(this);

            try
            {
                networkManager.SendMessage(request);
            }
            catch (Exception ex)
            {
                pendingSceneTransitions.Remove(transitionKey);
                Debug.LogError($"[{GetType().Name}] Scene transition request failed: {ex.Message}");
                OnSceneTransitionDenied(nextSceneName, "Failed to send transition request.");
            }
        }

        protected virtual void OnSceneTransitionDenied(string nextSceneName, string reason)
        {
            Debug.LogWarning($"{GetType().Name}: transition to {nextSceneName} denied. reason={reason}");
        }

        protected virtual void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnTitleSceneLoadedForWarning;
            shouldPlayWarningSound = false;

            foreach (var coroutine in managedCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            managedCoroutines.Clear();

            sceneLifetimeCts?.Cancel();
            sceneLifetimeCts?.Dispose();
            sceneLifetimeCts = null;
            pendingSceneTransitions.Clear();
            currentSceneOperation = null;
            isSceneTransitionInProgress = false;
        }

        private void RestartSceneLifetimeToken()
        {
            sceneLifetimeCts?.Cancel();
            sceneLifetimeCts?.Dispose();
            sceneLifetimeCts = new CancellationTokenSource();
        }

#if UNITY_EDITOR
        [Button("自動セット")]
        public void AutoSet()
        {
            if (timer == null)
            {
                timer = GetComponent<GameTimer>();
            }

            if (sceneController == null)
            {
                sceneController = GetComponent<AbstractSceneController>();
            }

            if (sceneMediateObject == null)
            {
                sceneMediateObject = GetComponent<AbstractMediateObject>();
            }

            if (generalSceneMasterData == null)
            {
                generalSceneMasterData = FindFirstObjectByType<GeneralSceneMasterData>();
                if (generalSceneMasterData == null)
                {
                    generalSceneMasterData = Resources.Load<GeneralSceneMasterData>("MasterData/GeneralSceneMasterData")
                        ?? Resources.Load<GeneralSceneMasterData>("MasterData/Scene/GeneralSceneMasterData");
                }
            }

            if (bgmMasterData == null)
            {
                bgmMasterData = FindFirstObjectByType<BGMMasterData>();
                if (bgmMasterData == null)
                {
                    bgmMasterData = Resources.Load<BGMMasterData>("MasterData/BGMMasterData");
                }
            }

            if (soundMasterData == null)
            {
                soundMasterData = FindFirstObjectByType<SoundMasterData>();
                if (soundMasterData == null)
                {
                    soundMasterData = Resources.Load<SoundMasterData>("MasterData/SoundMasterData");
                }
            }

            if (systemSoundMasterData == null)
            {
                systemSoundMasterData = FindFirstObjectByType<SystemSoundMasterData>();
                if (systemSoundMasterData == null)
                {
                    systemSoundMasterData = Resources.Load<SystemSoundMasterData>("MasterData/Sound/System/SystemSoundMasterData")
                        ?? Resources.Load<SystemSoundMasterData>("MasterData/SystemSoundMasterData");
                }
            }
        }
#endif
    }

    interface IAbstractBattleScene
    {
    }

    public abstract class AbstractBattleScene : AbstractScene
    {
    }

    public abstract class AbstractNonBattleScene : AbstractScene
    {
    }
}
