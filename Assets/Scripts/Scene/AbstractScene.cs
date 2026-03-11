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

        [SerializeField] [Required] public SystemSoundMasterData systemSoundMasterData;
        [SerializeField] [Required] public GeneralSceneMasterData generalSceneMasterData;
        [SerializeField] [Required] protected GameTimer timer;

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

        public void SetOnlineMode(bool value)
        {
            IsOnlineMode = value;
        }

        protected virtual void Awake()
        {
            sceneLifetimeCts?.Cancel();
            sceneLifetimeCts?.Dispose();
            sceneLifetimeCts = new CancellationTokenSource();

            try
            {
                _gameGeneralManager = DependencyInjectionConfig.Resolve<GameGeneralManager>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"AbstractScene.Awake: Failed to resolve GameGeneralManager: {ex.Message}");
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

        protected AsyncOperation GoToScene(string nextSceneName, bool additive = false)
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning($"{GetType().Name}: next scene name is empty.");
                return null;
            }

            OnBeforeSceneChange(nextSceneName);
            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var op = SceneManager.LoadSceneAsync(nextSceneName, mode);
            if (op != null)
            {
                op.completed += _ => OnAfterSceneLoaded(nextSceneName, mode);
            }
            return op;
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
