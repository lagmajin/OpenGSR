using Sirenix.OdinInspector;
using System;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
//using PimDeWitte.UnityMainThreadDispatcher;


namespace OpenGS
{

  

    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameTimer))]
    public abstract  class AbstractScene : SerializedMonoBehaviour, IAbstractScene, ISceneInputReceiver,ISceneLoadReceiver
    {
        private static bool shouldPlayWarningSound = false;
        private SynchronizationContext currentThread;

        //[SerializeField] [Required] public BGMManager soundManager;

        [SerializeField] [Required] public SystemSoundMasterData systemSoundMasterData;
        [SerializeField] [Required] public GeneralSceneMasterData generalSceneMasterData;

        [SerializeField] [Required] protected GameTimer timer;

        // Do not resolve DI services during field initialization; resolve in Awake to avoid initialization order issues.
        protected GameGeneralManager _gameGeneralManager;

        // 外部からは読み取り専用で公開。GameGeneralManager を唯一の真実（Single Source of Truth）とする。
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

        // 状態を変更するメソッド（後方互換性のため残すが、プロパティへの代入を推奨）
        public void SetOnlineMode(bool value)
        {
            IsOnlineMode = value;
        }

        protected virtual void Awake()
        {
            // Resolve DI services in Awake to ensure the DI container has been initialized by RuntimeInitializeOnLoadMethod.
            try
            {
                _gameGeneralManager = DependencyInjectionConfig.Resolve<GameGeneralManager>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"AbstractScene.Awake: Failed to resolve GameGeneralManager: {ex.Message}");
                // Leave _gameGeneralManager null as a safe fallback; callers should handle null accordingly.
            }
        }

        [Button("スクリーンショット")]

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InvokeIfDirectPlay()
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

        [InitializeOnEnterPlayMode]
        private static void HandleEditorRegistry()
        {
            // このメソッドが呼ばれるたびに何度も登録されないようにするため、
            // まずイベントを解除してから再登録する
            EditorApplication.delayCall -= HandleDelayCall;  // 事前に解除
            EditorApplication.delayCall += HandleDelayCall;  // 登録

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void HandleDelayCall()
        {
            // オブジェクトが必要なときにのみ検索
            var targets = GameObject.FindObjectsByType<AbstractScene>(FindObjectsSortMode.None);

            foreach (var t in targets)
            {
                // 各ターゲットのメソッドを呼び出し
                t.OnStartUnityEditor();
            }
        }

        protected virtual void OnStartUnityEditor()
        {

        }

        protected virtual void OnQuitUnityEditor()
        {

        }

        protected virtual void OnStartFromEditorDirectly()
        {


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

                    //Debug.Log("編集モードに戻ったよ！");
                    break;
            }
        }
        public abstract SynchronizationContext MainThread();

        public SynchronizationContext MainThread2()
        {
            //UnityMainthreadDispacher.
            
            return null;
        }
        public void SaveScreenShot()
        {
            //if(!directory==null)

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
            //GameManager().



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
            //var splashScene=generalSceneMasterData.
            
        }

        public void GoToTitleScene()
        {
            var titleScene = generalSceneMasterData.TitleScene();

            Debug.Log("タイトルシーンへ移動");

            SceneManager.LoadSceneAsync(titleScene);


        }



        public void GoToTitleSceneWithErrorSound()
        {
            var titleScene = generalSceneMasterData.TitleScene();

            Debug.Log("タイトルシーンへ移動（エラー警告音付き）");

            shouldPlayWarningSound = true;  // 警告音を鳴らすフラグをセット
            SceneManager.sceneLoaded += OnSceneLoaded;  // シーンロード時の処理を登録
            SceneManager.LoadSceneAsync(titleScene);
        }

        public void GoToShopScene()
        {
            var shopScene = generalSceneMasterData.ShopScene();

            SceneManager.LoadScene(shopScene);

        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == generalSceneMasterData.TitleScene() && shouldPlayWarningSound)
            {
                Debug.Log("タイトルシーンに到達、警告音を再生");

                shouldPlayWarningSound = false;  // フラグリセット

                
                
                

                SceneManager.sceneLoaded -= OnSceneLoaded;  // イベント登録解除
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if(!focus)
            {
                //KanKikuchi.AudioManager.BGMManager.Instance.ChangeBaseVolume(0);
            }
            else
            {

            }


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
