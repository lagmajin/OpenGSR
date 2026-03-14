using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
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
        private AsyncOperation currentSceneOperation;
        private bool isSceneTransitionInProgress;

        [SerializeField] [Required] public SystemSoundMasterData systemSoundMasterData;
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
            return null;
        }

        public void SaveScreenShot()
        {
            string date = DateTime.Now.ToString("yy-MM-dd_HH-mm-ss");
            string fileName = Application.dataPath + date + ".png";
            ScreenCapture.CaptureScreenshot(fileName);
        }

        protected virtual void EventProcess(string eventName)
        {
        }

        public void SendEvent(string str)
        {
        }

        protected CancellationToken SceneLifetimeToken =>
            sceneLifetimeCts != null ? sceneLifetimeCts.Token : CancellationToken.None;

        protected Coroutine StartManagedCoroutine(IEnumerator routine)
        {
            if (routine == null)
            {
                return null;
            }

            var c = StartCoroutine(routine);
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
        }

        public void GoToSplashScreen()
        {
        }

        public void GoToTitleScene()
        {
            var titleScene = generalSceneMasterData.TitleScene();
            Debug.Log("タイトルシーンへ移動");
            GoToScene(titleScene);
        }

        public void GoToTitleSceneWithErrorSound()
        {
            var titleScene = generalSceneMasterData.TitleScene();
            Debug.Log("タイトルシーンへ移動（エラー警告音付き）");

            shouldPlayWarningSound = true;
            SceneManager.sceneLoaded += OnTitleSceneLoadedForWarning;
            GoToScene(titleScene);
        }

        public void GoToShopScene()
        {
            var shopScene = generalSceneMasterData.ShopScene();
            GoToScene(shopScene);
        }

        private void OnTitleSceneLoadedForWarning(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == generalSceneMasterData.TitleScene() && shouldPlayWarningSound)
            {
                Debug.Log("タイトルシーンに到達、警告音を再生");
                shouldPlayWarningSound = false;
                SceneManager.sceneLoaded -= OnTitleSceneLoadedForWarning;
            }
        }

        private void OnApplicationFocus(bool focus)
        {
        }

        private void OnApplicationPause(bool pause)
        {
        }

        public void GoToOfflineWaitRoom()
        {
        }

        public void KeyPress()
        {
        }

        public virtual void GoToLobby()
        {
        }

        protected virtual void OnDestroy()
        {
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
