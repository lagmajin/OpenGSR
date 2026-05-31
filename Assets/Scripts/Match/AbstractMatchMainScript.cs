using UnityEngine;
using OpenGSR.Audio;
//using Cinemachine;
using Newtonsoft.Json.Linq;
using System.Timers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

using OpenGSCore;
using Sirenix.OdinInspector;
using MessageType = OpenGSCore.MessageType;

//using Unity.Cinemachine;
using Zenject;
using UnityEditor;
using Unity.Cinemachine;
using UniRx;

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

        private MatchRUDPServerNetworkManager matchNetworkManager;
        private bool matchNetworkSubscribed;


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
                GoToTitle();
            }

            return true;
        }

        public List<GameObject> AllPlayers()
        {
            return GameObject.FindObjectsByType<AbstractPlayer>(FindObjectsSortMode.None)
                .Select(player => player != null ? player.gameObject : null)
                .Where(gameObject => gameObject != null)
                .ToList();
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
            return points.random();
        }

        public void Start()
        {
            OnStart();
            PlayStageBGM();
            BindMatchNetwork();

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

        protected void BindMatchNetwork()
        {
            if (matchNetworkSubscribed)
            {
                return;
            }

            try
            {
                matchNetworkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Failed to resolve MatchRUDPServerNetworkManager: {ex.Message}");
                matchNetworkManager = null;
                return;
            }

            if (matchNetworkManager == null)
            {
                return;
            }

            matchNetworkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Subscribe(OnNetworkDataRecved)
                .AddTo(this);

            matchNetworkManager.ConnectedStream
                .ObserveOnMainThread()
                .Subscribe(_ => OnMatchNetworkConnected())
                .AddTo(this);

            matchNetworkManager.DisconnectedStream
                .ObserveOnMainThread()
                .Subscribe(_ => OnMatchNetworkDisconnected())
                .AddTo(this);

            if (IsOnlineMatch() && !matchNetworkManager.IsConnected())
            {
                matchNetworkManager.ConnectToLocalServer(0);
            }

            matchNetworkSubscribed = true;
        }

        protected virtual void OnMatchNetworkConnected()
        {
            Debug.Log($"[{GetType().Name}] Match network connected");
        }

        protected virtual void OnMatchNetworkDisconnected()
        {
            Debug.Log($"[{GetType().Name}] Match network disconnected");
        }

        void OnEnable()
        {
            SubscribeEvent();
        }

        void OnDisable()
        {
            UnSubscribeEvent();
        }


        public void ShakeCamera()
        {
            impluseSource?.GenerateImpulse(new Vector3(10, 10));
        }

        public void PlayDefaultBGM()
        {
            PlayBGM(null);
        }

        protected void PlayGameStartVoice()
        {
            SoundManager.Instance.PlayGameSound(EMatchSound.GameStartVoice);
        }

        protected virtual void PlayStageBGM()
        {
            var map = ResolveCurrentStageMap();
            Debug.Log($"[{GetType().Name}] PlayStageBGM: {map}");
            SoundManager.Instance.PlayBGM(map);
        }

        protected virtual EMap ResolveCurrentStageMap()
        {
            try
            {
                var manager = matchRoomManager ?? MatchRoomManager();
                if (manager?.MapInfo != null && manager.MapInfo.Map != EMap.Unknown)
                {
                    return manager.MapInfo.Map;
                }

                if (manager?.WaitRoom != null && manager.WaitRoom.Map != EMap.Unknown)
                {
                    return manager.WaitRoom.Map;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] ResolveCurrentStageMap failed from room manager: {ex.Message}");
            }

            try
            {
                var offline = GameModeSelectManager.Instance?.OfflineGameSelect;
                if (offline != null && offline.Map != EMap.Unknown)
                {
                    return offline.Map;
                }

                var online = GameModeSelectManager.Instance?.OnlineGameSelect;
                if (online != null && online.Map != EMap.Unknown)
                {
                    return online.Map;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] ResolveCurrentStageMap failed from selection manager: {ex.Message}");
            }

            return EMap.DryDays;
        }

        public void PlayBGM(AudioClip bgm)
        {
            if (bgm != null)
            {
                SimpleAudioManager.Instance.PlayBGM(bgm, 1.0f, true);
            }
            else
            {
                SimpleAudioManager.Instance.PlayBGM("Default");
            }
        }

        public void StopBGM()
        {
            SimpleAudioManager.Instance.StopBGM();
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
            Debug.Log($"[{GetType().Name}] AddNewFieldItemInTheScene");
        }

        protected virtual void OnStart()
        {
            isStarted = true;
            endFlag = false;
        }

        protected virtual void OnEnd()
        {
            endFlag = true;
        }

        protected virtual void OnSomeoneDead()
        {
            Debug.Log($"[{GetType().Name}] Someone dead");
        }

        protected virtual void OnWin()
        {
            Debug.Log($"[{GetType().Name}] Win");
            endFlag = true;
        }

        protected virtual void OnLose()
        {
            Debug.Log($"[{GetType().Name}] Lose");
            endFlag = true;
        }

        protected virtual void OnSuddendeath()
        {
            Debug.Log($"[{GetType().Name}] Sudden death");
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
            Debug.Log($"[{GetType().Name}] Network disconnected");
        }

        protected virtual void OnNetworkDataRecved(JObject obj)
        {
            var messageType = MessageType.Normalize(obj["MessageType"]?.ToString());

            switch (messageType)
            {
                case RUDPMessageTypes.ItemUse:
                    HandleItemUse(obj);
                    break;
                case RUDPMessageTypes.PlayerBuff:
                    HandlePlayerBuff(obj);
                    break;
                case RUDPMessageTypes.PlayerDebuff:
                    HandlePlayerDebuff(obj);
                    break;
            }
        }

        protected virtual void HandleItemUse(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var itemId = json["ItemId"]?.ToString() ?? "";
            var itemType = json["ItemType"]?.ToString() ?? "";
            var effect = json["Effect"]?.ToString() ?? "";

            Debug.Log($"[{GetType().Name}] ItemUse received: player={playerId}, item={itemId}, type={itemType}, effect={effect}");
        }

        protected virtual void HandlePlayerBuff(JObject json)
        {
            var player = ResolveLocalPlayer();
            var buffType = json["BuffType"]?.ToString() ?? "";
            var duration = json["Duration"]?.ToObject<int>() ?? 0;
            var value = json["Value"]?.ToObject<float>() ?? 0f;

            if (player != null)
            {
                switch (buffType)
                {
                    case "HpRecovery":
                        player.Heal(value);
                        break;
                    case "BulletEnhance":
                    case "PowerUp":
                    case "PowerUpItem":
                    case "AttackUp":
                        player.IncreaseAttack(duration);
                        break;
                    case "DefenceUp":
                    case "DefenceUpItem":
                    case "DefenseUpItem":
                    case "DefenseUp":
                        player.IncreaseDefense(duration);
                        break;
                    case "SpeedUp":
                    case "SpeedUpItem":
                        player.SpeedUp(duration);
                        break;
                    case "Stealth":
                    case "StealthItem":
                    case "Invisible":
                        player.Invisible(duration);
                        break;
                    case "GrenadePack":
                    case "NormalGrenadePack":
                        player.RefillGrenade(EGrenadeType.Normal);
                        break;
                    case "PowerGrenadePack":
                        player.RefillGrenade(EGrenadeType.Power);
                        break;
                    case "ClusterGrenadePack":
                        player.RefillGrenade(EGrenadeType.Cluster);
                        break;
                    case "MagnetGrenadePack":
                        player.RefillGrenade(EGrenadeType.Magnetic);
                        break;
                    case "MineGrenadePack":
                        player.RefillGrenade(EGrenadeType.Mine);
                        break;
                }
            }

            Debug.Log($"[{GetType().Name}] PlayerBuff received: {json}");
        }

        protected virtual void HandlePlayerDebuff(JObject json)
        {
            var player = ResolveLocalPlayer();
            var debuffType = json["DebuffType"]?.ToString() ?? "";
            var duration = json["Duration"]?.ToObject<int>() ?? 0;

            if (player != null)
            {
                switch (debuffType)
                {
                    case "PoisonBullet":
                        player.PoisonBullet(duration);
                        break;
                }
            }

            Debug.Log($"[{GetType().Name}] PlayerDebuff received: {json}");
        }

        protected virtual AbstractPlayer ResolveLocalPlayer()
        {
            if (player == null)
            {
                return null;
            }

            return player.GetComponent<AbstractPlayer>();
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
            if (timer == null)
            {
                timer = GetComponent<MatchTimer>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (battleSceneMediateObject == null)
            {
                battleSceneMediateObject = FindFirstObjectByType<AbstractBattleSceneMediateObject>();
            }

            if (prefabMasterData == null)
            {
                prefabMasterData = FindFirstObjectByType<PlayerPrefabMasterData>();
            }

            if (uiCanvasMasterData == null)
            {
                uiCanvasMasterData = FindFirstObjectByType<CanvasMasterData>();
            }
        }


        [Button("リザルトテスト")]
        public void GoToResult()
        {

            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().ResultScene());

        }

        protected void RequestSceneTransition(string nextSceneName, string reason = "")
        {
            RequestSceneTransition(nextSceneName, null, reason);
        }

        protected void RequestSceneTransition(string nextSceneName, Action onApproved, string reason = "")
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning($"[{GetType().Name}] RequestSceneTransition skipped because nextSceneName is empty. reason={reason}");
                return;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.Log($"[{GetType().Name}] RequestSceneTransition -> {nextSceneName} reason={reason}");
            }

            onApproved?.Invoke();
            SceneManager.LoadSceneAsync(nextSceneName);
        }

        /// <summary>
        /// 現在のローカルプレイヤーの所属チーム名を返す。
        /// オフライン結果の MyTeam 記録に使う。
        /// </summary>
        protected string ResolveLocalTeamName()
        {
            if (player == null)
            {
                return "Draw";
            }

            var abstractPlayer = player.GetComponent<AbstractPlayer>();
            if (abstractPlayer == null)
            {
                return "Draw";
            }

            var team = abstractPlayer.Team();
            return team == ETeam.NoTeam ? "Draw" : team.ToString();
        }

        /// <summary>
        /// ローカル待機所の全プレイヤー情報を返す。
        /// オフライン結果のプレイヤー一覧に使う。
        /// </summary>
        protected List<OpenGSCore.PlayerInfo> ResolveLocalPlayers()
        {
            var manager = matchRoomManager ?? MatchRoomManager();
            if (manager?.WaitRoom == null)
            {
                return new List<OpenGSCore.PlayerInfo>();
            }

            return manager.WaitRoom.AllPlayers();
        }

        [Button("タイトルテスト")]
        public void GoToTitle()
        {
            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().TitleScene());
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

        public virtual void OnMyPlayerDead()
        {
            Debug.Log($"[{GetType().Name}] My player dead");
        }

        /*
        public MatchRoom MatchRoom()
        {
            return MatchRoomManager.Instance.MatchRoom;
        }

        */
    }
}
