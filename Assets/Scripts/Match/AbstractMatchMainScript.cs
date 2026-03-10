using UnityEngine;
using OpenGSR.Audio;
//using Cinemachine;
using Newtonsoft.Json.Linq;
using System.Timers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using OpenGSCore;
using Sirenix.OdinInspector;

//using Unity.Cinemachine;
using Zenject;
using UnityEditor;
using Unity.Cinemachine;

//using Unity.

namespace OpenGS
{





    [DisallowMultipleComponent]
    public abstract class AbstractMatchMainScript : MonoBehaviour, IAbstractMatchMainScript
    {
        private GameGeneralManager gameManager = GameGeneralManager.GetInstance;
        EGameMode gameMode = EGameMode.Unknown;

        [SerializeField] private MatchTimer timer;
        //public AudioClip bgm;
        //public AudioClip gameStartSound;
        //public AudioClip gameWonSound;
        //public AudioClip gameLostSound;
        //public AudioClip suddenDeathSound;

        public Camera mainCamera;
        public Camera BackgroundCamera;

        public CinemachineCamera vcamera;
        public CinemachineCamera playerCamera;
        public CinemachineCamera observerCamera;

        public CinemachineImpulseSource impluseSource;

        // public GameObject SESoundStorage;
        //public GameObject PlayerStorage;
        ////public GameObject PlayerSoundStorage;
        //public GameObject weaponSoundStorage;
        //public GameObject GrenadePrefabStorage;

        [SerializeField, Range(0f, 15f)]
        public float gotoResultSceneWaitTime = 4.0f;

        public GameObject itemSpawnPoints;

        public GameObject player;

        private GameObject[] otherPlayers;

        private Timer oneSecInvtervalTimer = new Timer(1000);
        private Timer oneMiniteIntervalTimer = new Timer(60000);


        public bool overrideGameTime = false;
        public float testGameTime = 1000f;
        [SerializeField, Range(0.1f, 1.0f)]
        public float gameEndTimeScale = 0.4f;
        protected bool endFlag = false;

        protected bool isStarted = false;

        public GameGeneralManager GameManager { get => gameManager; set => gameManager = value; }

        public PlayerPrefabMasterData prefabMasterData;

        public CanvasMasterData uiCanvasMasterData;

        public AbstractBattleSceneMediateObject battleSceneMediateObject;

        [Inject]
        [ShowInInspector]protected MatchRoomManager matchRoomManager;


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
            var targets = GameObject.FindObjectsByType<AbstractMatchMainScript>(FindObjectsSortMode.None);

            foreach (var t in targets)
            {
                // 各ターゲットのメソッドを呼び出し
                t.OnStartUnityEditor();
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Init(){


        }

        public virtual void OnStartUnityEditor()
        {
            Debug.Log($"[{this.GetType().FullName}] OnStartUnityEditor");

        }
        protected virtual void OnQuitUnityEditor()
        {


            Debug.Log($"[{this.GetType().FullName}] OnQuitUnityEditor");



        }

        protected virtual void OnStartFromEditorDirectly()
        {

            Debug.Log("★ このシーンから直接再生されたときだけ実行される！");

        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    var scenes = GameObject.FindObjectsByType<AbstractMatchMainScript>(FindObjectsSortMode.None);
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

        enum eEventProcecssType
        {
            Immediate,
            Delay
        }

        public bool IsOnlineMatch()
        {
            return gameManager != null ? gameManager.IsOnlineGameMode : GameGeneralManager.GetInstance.IsOnlineGameMode;
        }

        public bool IsOfflineMatch()
        {
            return !IsOnlineMatch();
        }

        public List<GameObject> AllPlayers()
        {
            return null;
        }

        public abstract void PostEvent(AbstractGameEvent e);

        protected void SubscribeEvent()
        {
            MatchManager.Instance.SubscribeEvent(this);
        }

        protected void UnSubscribeEvent()
        {
            MatchManager.Instance.UnSubscribeEvent(this);
        }

        // ─── プレイヤー生成・ライフサイクル ──────────────────────────

        /// <summary>
        /// 自プレイヤーを生成し、カメラや装備をセットアップする。
        /// </summary>
        protected virtual GameObject CreateMyPlayer(Vector3 position, ETeam team = ETeam.NoTeam)
        {
            // 装備データからキャラクターIDを取得（未設定ならMisty）
            string charId = UserSaveManager.GetEquippedId(EShopCategory.Character);
            if (string.IsNullOrEmpty(charId)) charId = EPlayerCharacter.Misty.ToString();

            // プレハブの検索
            var prefab = prefabMasterData.SearchPlayerPrefab(charId);
            if (prefab == null)
            {
                Debug.LogError($"Spawn failed: Prefab for {charId} not found.");
                return null;
            }

            // 生成
            var playerObj = Instantiate(prefab, position, Quaternion.identity);
            playerObj.name = "MyPlayer";

            var pAgent = playerObj.GetComponent<AbstractPlayer>();
            if (pAgent != null)
            {
                pAgent.SetPlayerType(EPlayerType.MyPlayer);
                pAgent.SetTeam(team);
                
                // 装備の反映（ブースターの色など）
                pAgent.OnSpawn();
            }

            // カメラのセットアップ
            SetupPlayerCamera(playerObj.transform);

            this.player = playerObj;
            return playerObj;
        }

        /// <summary>
        /// カメラをターゲットに追従させる。
        /// </summary>
        protected void SetupPlayerCamera(Transform target)
        {
            if (playerCamera != null)
            {
                playerCamera.Follow = target;
                playerCamera.Priority = 10;
            }
            if (vcamera != null)
            {
                vcamera.Priority = 0;
            }
        }

        /// <summary>
        /// ランダムなリスポーン地点を取得する。
        /// </summary>
        protected Vector3 GetRandomSpawnPoint(IReSpawnPoints points)
        {
            if (points == null) return Vector3.zero;
            return points.GetRandomPoint();
        }

        public void Start()
        {
            OnStart();

            Debug.Log("AbstracMainScript.Con");

            if (!impluseSource)
            {
                impluseSource = gameObject.GetComponent<CinemachineImpulseSource>();
            }

            /*

            oneSecInvtervalTimer.Elapsed += On1Sec;
            oneSecInvtervalTimer.Start();

            oneMiniteIntervalTimer.Elapsed += On1Min;
            oneMiniteIntervalTimer.Start();
            */

            StartCoroutine(OneSecCallback());
            StartCoroutine(OneMinCallback());
        }

        IEnumerator OneSecCallback()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1);
                OnOneSec();
            }
        }

        IEnumerator OneMinCallback()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(60);
                OnOneMin();
            }
        }

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }


        public void ShakeCamera()
        {
            impluseSource?.GenerateImpulse(new Vector3(10, 10));
        }

        public void PlayDefaultBGM()
        {
            //SimpleAudioManager.Instance.PlayBGM("Default");

        }

        public void PlayBGM(AudioClip bgm)
        {

        }

        public void StopBGM()
        {

        }


        protected void PlaySE(AudioClip se, bool isLoop = false)
        {
            if (se)
            {
                SimpleAudioManager.Instance.PlaySE(se, 1.0f);
            }
            else
            {

            }
        }

        public EGameMode GameMode()
        {
            return gameMode;
        }

        public void AddNewFieldItemInTheScene()
        {

        }

        protected virtual void OnStart()
        {

        }

        protected virtual void OnEnd()
        {

        }

        protected virtual void OnSomeoneDead()
        {

        }

        protected virtual void OnWin()
        {

        }

        protected virtual void OnLose()
        {

        }

        protected virtual void OnSuddendeath()
        {

        }

        protected virtual void OnOneSec()
        {
            Debug.Log("1Sec");

        }
        protected virtual void OnOneMin()
        {
            Debug.Log("1Min");
        }

        void OnDisconnectNetowrk()
        {

        }

        protected virtual void OnNetworkDataRecved(JObject obj)
        {

        }

        void OnDestory()
        {
            /*
            oneSecInvtervalTimer.Stop();
            oneSecInvtervalTimer.Dispose();

            oneMiniteIntervalTimer.Stop();
            oneMiniteIntervalTimer.Dispose();
            */
        }

        protected void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
      UnityEngine.Application.Quit();
#endif
        }

        [Button("自動セット")]
        public void AutoSet()
        {

        }


        [Button("リザルトテスト")]
        public void GoToResult()
        {

            SceneManager.LoadSceneAsync("ResultScene");

        }

        [Button("タイトルテスト")]
        public void GoToTitle()
        {
            SceneManager.LoadSceneAsync("NewTitleScene");
        }

        public MatchRoomManager MatchRoomManager()
        {
            return DependencyInjectionConfig.Resolve<MatchRoomManager>();
        }

        [Button("リスポーンUI表示")]
        public void ShowReSpawnUI(float time = 5.0f)
        {
            Instantiate(uiCanvasMasterData.ReSpawnUICanvas);


        }

        public virtual void OnMyPlayerDead() { }

        /*
        public MatchRoom MatchRoom()
        {
            return MatchRoomManager.Instance.MatchRoom;
        }

        */
    }
}
