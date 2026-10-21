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
#if UNITY_EDITOR
using UnityEditor;
#endif

using OpenGSCore;
using Sirenix.OdinInspector;
using MessageType = OpenGSCore.MessageType;

//using Unity.Cinemachine;
using Zenject;
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

        [SerializeField] protected MatchTimer timer;
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
        [SerializeField] private Transform initialCameraFollowTarget;

        public CinemachineImpulseSource impluseSource;

        // public GameObject SESoundStorage;
        //public GameObject PlayerStorage;
        ////public GameObject PlayerSoundStorage;
        //public GameObject weaponSoundStorage;
        //public GameObject GrenadePrefabStorage;

        [SerializeField, Range(0f, 15f)]
        public float gotoResultSceneWaitTime = 4.0f;

        [SerializeField, Min(0f)]
        protected float respawnDelaySeconds = 5.0f;

        public GameObject itemSpawnPoints;

        public GameObject player;

        private GameObject[] otherPlayers;

        private Timer oneSecInvtervalTimer = new Timer(1000);
        private Timer oneMiniteIntervalTimer = new Timer(60000);
        private Coroutine oneSecondRoutine;
        private Coroutine oneMinuteRoutine;


        public bool overrideGameTime = false;
        public float testGameTime = 1000f;
        [SerializeField, Range(0.1f, 1.0f)]
        public float gameEndTimeScale = 0.4f;
        protected bool endFlag = false;
        protected bool sceneTransitionRequested = false;

        protected bool isStarted = false;
        protected bool isMatchTerminating = false;

        public GameGeneralManager GameManager { get => gameManager; set => gameManager = value; }

        public PlayerPrefabMasterData prefabMasterData;

        public CanvasMasterData uiCanvasMasterData;

        public AbstractBattleSceneMediateObject battleSceneMediateObject;

        [Inject]
        [ShowInInspector]protected MatchRoomManager matchRoomManager;

        private MatchRUDPServerNetworkManager matchNetworkManager;
        private ClientNetworkManager clientNetworkManager;
        private bool matchNetworkSubscribed;
        private MatchConnectionStatusOverlay connectionStatusOverlay;


#if UNITY_EDITOR
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

        /// <summary>
        /// Every player in the scene, whichever component tree carries it.
        /// <para>
        /// This asked for one type, which no player prefab carries, so it always
        /// came back empty and a caller that spawns the other players from it
        /// spawned nobody.
        /// </para>
        /// </summary>
        public List<GameObject> AllPlayers()
        {
            return GameObject.FindObjectsByType<PlayerAgent>(FindObjectsSortMode.None)
                .Where(agent => agent != null)
                .Select(agent => agent.gameObject)
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
            EnsureMasterDataReferences();

            if (prefabMasterData == null)
            {
                Debug.LogError($"[{GetType().Name}] prefabMasterData is not assigned. Cannot create player.");
                return null;
            }

            // 装備データからキャラクターIDを取得（未設定ならAmi）
            string charId = UserSaveManager.GetEquippedId(EShopCategory.Character);
            if (string.IsNullOrEmpty(charId)) charId = EPlayerCharacter.Ami.ToString();

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

            // Whichever tree carries it. The player prefabs use the agent, and this used to
            // only look for the other one, so a spawned player was never told its
            // team and never told it had spawned at all.
            var pAgent = playerObj.GetComponent<IBattlePlayer>();
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
            if (observerCamera != null)
            {
                observerCamera.Priority = 0;
            }
        }
#endif

        protected void SetupInitialCameraFollow()
        {
            var target = FindInitialCameraFollowTarget();
            if (target == null)
            {
                return;
            }

            SetupPlayerCamera(target);
        }

        protected virtual Transform FindInitialCameraFollowTarget()
        {
            if (initialCameraFollowTarget != null)
            {
                return initialCameraFollowTarget;
            }

            var centralObject = GameObject.Find("CentralObject");
            if (centralObject != null)
            {
                return centralObject.transform;
            }

            var centralGameObject = GameObject.Find("CentralGameObject");
            if (centralGameObject != null)
            {
                return centralGameObject.transform;
            }

            return null;
        }

        public void EnterSpectatorMode(Transform excluded = null)
        {
            var target = ResolveSpectatorTarget(excluded);
            if (observerCamera != null)
            {
                observerCamera.Follow = target;
                observerCamera.Priority = 10;
            }

            if (playerCamera != null)
            {
                playerCamera.Priority = 0;
            }

            if (vcamera != null)
            {
                vcamera.Priority = 0;
            }
        }

        public void ExitSpectatorMode(Transform target)
        {
            if (target != null)
            {
                SetupPlayerCamera(target);
            }
            else if (playerCamera != null)
            {
                playerCamera.Priority = 10;
            }

            if (observerCamera != null)
            {
                observerCamera.Priority = 0;
            }
        }

        protected Transform ResolveSpectatorTarget(Transform excluded = null)
        {
            if (PlayerRegistry.Instance == null)
            {
                return null;
            }

            foreach (var player in PlayerRegistry.Instance.GetAllPlayers())
            {
                if (player == null || player.Transform == null)
                {
                    continue;
                }

                if (excluded != null && player.Transform == excluded)
                {
                    continue;
                }

                if (!player.IsDead())
                {
                    return player.Transform;
                }
            }

            return null;
        }

        /// <summary>
        /// ランダムなリスポーン地点を取得する。
        /// </summary>
        protected Vector3 GetRandomSpawnPoint(IRespawnPoints points, ETeam team = ETeam.NoTeam)
        {
            if (points == null) return Vector3.zero;
            return points.GetRandomSpawnPoint(team);
        }

        protected float ResolveRespawnDelaySeconds()
        {
            try
            {
                return SettingsManager.Instance?.GetGameplaySettings()?.RespawnDelaySeconds ?? respawnDelaySeconds;
            }
            catch
            {
                return respawnDelaySeconds;
            }
        }

        public void SetPlayerCameraZoom(float orthographicSizeScale)
        {
            if (playerCamera == null)
            {
                return;
            }

            orthographicSizeScale = Mathf.Max(0.1f, orthographicSizeScale);
            var lens = playerCamera.Lens;
            lens.OrthographicSize *= orthographicSizeScale;
            playerCamera.Lens = lens;
        }

        public void Start()
        {
            EnsureMasterDataReferences();
            EnsurePlayerStatusUI();
            EnsureRespawnCanvas();
            EnsureMatchOverlay();
            EnsureEnemyViews();
            OnStart();
            gameMode = MatchModeResolver.ResolveCurrentGameMode();
            PlayStageBGM();
            BindMatchNetwork();
            SetupInitialCameraFollow();

            Debug.Log("AbstractMainScript.Start");

            if (!impluseSource)
            {
                impluseSource = gameObject.GetComponent<CinemachineImpulseSource>();
            }

            // MatchTimer auto-start if timer is assigned and not overridden
            if (timer != null && !overrideGameTime)
            {
                timer.SetTime(ResolveMatchDuration());
                timer.timeupEvent.RemoveAllListeners();
                timer.timeupEvent.AddListener(OnTimeUp);
                timer.StartTimer();
                Debug.Log($"[{GetType().Name}] Timer started: {timer.GetRemainingTime()}s");
            }

            StopMatchIntervalRoutines();
            oneSecondRoutine = StartCoroutine(OneSecCallback());
            oneMinuteRoutine = StartCoroutine(OneMinCallback());
        }

        private void EnsurePlayerStatusUI()
        {
            if (FindFirstObjectByType<PlayerStatusUIManager>() != null)
            {
                return;
            }

            if (uiCanvasMasterData == null || uiCanvasMasterData.PlayerStatusUI == null)
            {
                Debug.LogWarning($"[{GetType().Name}] PlayerStatusUI prefab is not configured. Player HUD is disabled.", this);
                return;
            }

            Instantiate(uiCanvasMasterData.PlayerStatusUI);
        }

        /// <summary>
        /// Brings up the respawn countdown, once per match.
        /// <para>
        /// It used to be created by ShowReSpawnUI, which a mode only reached after
        /// a death, and which had nothing to instantiate: the prefab the master
        /// data named did not exist, so the call logged a warning and returned. The
        /// countdown is a thing you need to see while you are dead rather than
        /// after it, so it is brought up with the rest of the HUD instead.
        /// </para>
        /// </summary>
        private void EnsureRespawnCanvas()
        {
            if (FindFirstObjectByType<RespawnCanvas>() != null)
            {
                return;
            }

            if (uiCanvasMasterData == null || uiCanvasMasterData.ReSpawnUICanvas == null)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] ReSpawnUICanvas prefab is not configured. " +
                    "The respawn countdown will not be shown.",
                    this);
                return;
            }

            Instantiate(uiCanvasMasterData.ReSpawnUICanvas);
        }

        /// <summary>
        /// Brings up the kill feed and the scoreboard, once per match.
        /// <para>
        /// Both were fully written and wired to the event broker, and neither was
        /// on anything: no scene and no prefab carried them, so a match had a kill
        /// feed that could not appear and a scoreboard that could not be opened,
        /// and the result screen showed zero kills because nothing had ever told
        /// any of it that a kill happened.
        /// </para>
        /// </summary>
        private void EnsureMatchOverlay()
        {
            if (FindFirstObjectByType<ScoreboardUIManager>() != null)
            {
                return;
            }

            if (uiCanvasMasterData == null || uiCanvasMasterData.MatchOverlay == null)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] MatchOverlay prefab is not configured. " +
                    "The kill feed and scoreboard will not be shown.",
                    this);
                return;
            }

            Instantiate(uiCanvasMasterData.MatchOverlay);
        }

        /// <summary>
        /// Brings up the thing that draws a mission's enemies.
        /// <para>
        /// The enemies are the server's, so without something drawing what it
        /// says a mission is played on an empty map: the server places them, steps
        /// them and settles every shot against them, and the client shows a room
        /// with nothing in it. It is only brought up for a mode that has enemies,
        /// because a death match would otherwise be carrying a manager with
        /// nothing to draw.
        /// </para>
        /// </summary>
        private void EnsureEnemyViews()
        {
            if (MatchModeResolver.ResolveCurrentGameMode() != EGameMode.Mission)
            {
                return;
            }

            if (FindFirstObjectByType<MissionEnemyViewManager>() != null)
            {
                return;
            }

            var host = new GameObject(nameof(MissionEnemyViewManager));
            host.AddComponent<MissionEnemyViewManager>();
        }

        /// <summary>
        /// Match duration in seconds per mode. Override in derived classes. Default 10 minutes.
        /// </summary>
        /// <summary>
        /// The match length this client starts counting down from, before the
        /// server has said otherwise.
        /// <para>
        /// Four modes each answered this with the same ten minutes while the
        /// server played whatever the room was configured for, and a fifth mode
        /// answered it with the same number too. It is only the value the
        /// countdown holds between the match starting and the room's clock
        /// arriving, and the room's clock replaces it either way. So it is one
        /// number in one place rather than five that happen to agree.
        /// </para>
        /// </summary>
        protected virtual float ResolveMatchDuration()
        {
            return DefaultMatchDurationSeconds;
        }

        /// <summary>
        /// The guess this client counts down from until the room says what the
        /// match is really.
        /// </summary>
        public const float DefaultMatchDurationSeconds = 600f;

        /// <summary>
        /// The clock the server is running this match on, once the room has said.
        /// <para>
        /// Every mode used to resolve its own duration and every one of them
        /// answered ten minutes, whatever the server was actually playing. A client
        /// whose clock runs out early ends a match the server is still running, and
        /// one that runs out late waits through a match that is over. The room
        /// state is the server's own number, so it wins.
        /// </para>
        /// </summary>
        private float? serverMatchSeconds;

        /// <summary>
        /// Whether the server is playing this match on a different length of time
        /// than the client's own guess.
        /// </summary>
        private bool hasServerClock =>
            serverMatchSeconds.HasValue && serverMatchSeconds.Value > 0f;

        /// <summary>
        /// Called when the local clock reaches zero.
        /// <para>
        /// The gate on online play is here rather than in each mode, because it was
        /// in each mode and three of the four copies were missing it. The clock
        /// the rule runs on is the server's, so it is the server's to end the
        /// match: a client that ends it here as well leaves a match the server is
        /// still playing, and the two disagree about whether the game is running.
        /// </para>
        /// </summary>
        private void OnTimeUp()
        {
            if (endFlag)
            {
                return;
            }

            if (IsOnlineMatch() && hasServerClock)
            {
                Debug.Log(
                    $"[{GetType().Name}] Local clock reached zero; waiting for the server to end the match.");
                return;
            }

            OnLocalTimeUp();
        }

        /// <summary>
        /// What a mode does when the match is over.
        /// <para>
        /// Reached only when this client is the one deciding, which offline it
        /// always is and online it is not when the server has said what length of
        /// match it is playing. A mode that ends the match here in an online match
        /// is ending a match the server is still running.
        /// </para>
        /// </summary>
        protected virtual void OnLocalTimeUp()
        {
            Debug.Log($"[{GetType().Name}] Time up!");
        }

        /// <summary>
        /// 終了処理の二重実行を防ぐための共通ガード。
        /// すでに終了処理へ入っている場合は false を返す。
        /// </summary>
        protected bool TryBeginMatchEnd()
        {
            if (isMatchTerminating || endFlag)
            {
                return false;
            }

            isMatchTerminating = true;
            endFlag = true;
            CancelInvoke();

            // The match manager is a process wide singleton, so what this match
            // subscribed has to go when the match ends. Otherwise the next match
            // inherits this one's players and this one's main script, which has
            // already been unloaded by then.
            MatchManager.Instance.Clear();
            return true;
        }

        /// <summary>
        /// 終了処理後のシーン遷移予約を統一して行う。
        /// </summary>
        protected void ScheduleResultSceneTransition(float delaySeconds)
        {
            CancelInvoke(nameof(GoToResult));
            if (delaySeconds <= 0f)
            {
                GoToResult();
                return;
            }

            Invoke(nameof(GoToResult), delaySeconds);
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
            }

            EnsureConnectionStatusOverlay();

            // The realtime manager carries the UDP stream, and the lobby session
            // carries the rulings the server settles there, so both are needed.
            // The realtime manager was the branch that returned early, which
            // left the ruling subscription below it unreachable: a match script
            // then heard about nothing the server decided, because the server
            // sends its rulings over the lobby stream and not the realtime one.
            var lobby = FindFirstObjectByType<ClientNetworkManager>();
            if (lobby != null)
            {
                clientNetworkManager = lobby;
                clientNetworkManager.ServerMatchRulingReceived += OnNetworkDataRecved;
            }

            if (matchNetworkManager != null)
            {
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
                return;
            }

            if (clientNetworkManager == null)
            {
                return;
            }

            clientNetworkManager.UdpMessageReceived += OnNetworkDataRecved;
            clientNetworkManager.MatchUdpConnectionChanged += OnClientMatchUdpConnectionChanged;
            matchNetworkSubscribed = true;
        }

        private void OnClientMatchUdpConnectionChanged(bool connected, string reason)
        {
            if (connected)
            {
                OnMatchNetworkConnected();
            }
            else
            {
                OnMatchNetworkDisconnected();
            }
        }

        protected virtual void OnMatchNetworkConnected()
        {
            Debug.Log($"[{GetType().Name}] Match network connected");
            connectionStatusOverlay?.SetConnected();
        }

        protected virtual void OnMatchNetworkDisconnected()
        {
            Debug.Log($"[{GetType().Name}] Match network disconnected");
            EnsureConnectionStatusOverlay();
            connectionStatusOverlay?.SetDisconnected("接続を確認中");
        }

        private void EnsureConnectionStatusOverlay()
        {
            if (connectionStatusOverlay == null)
            {
                connectionStatusOverlay = GetComponent<MatchConnectionStatusOverlay>();
                if (connectionStatusOverlay == null)
                {
                    connectionStatusOverlay = gameObject.AddComponent<MatchConnectionStatusOverlay>();
                }
            }
        }

        protected virtual void OnEnable()
        {
            SubscribeEvent();
        }

        protected virtual void OnDisable()
        {
            UnSubscribeEvent();
            StopMatchIntervalRoutines();
            if (clientNetworkManager != null)
            {
                clientNetworkManager.UdpMessageReceived -= OnNetworkDataRecved;
                clientNetworkManager.MatchUdpConnectionChanged -= OnClientMatchUdpConnectionChanged;
                clientNetworkManager = null;
            }
        }

        private void StopMatchIntervalRoutines()
        {
            if (oneSecondRoutine != null)
            {
                StopCoroutine(oneSecondRoutine);
                oneSecondRoutine = null;
            }

            if (oneMinuteRoutine != null)
            {
                StopCoroutine(oneMinuteRoutine);
                oneMinuteRoutine = null;
            }
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
            SoundManager.Instance?.PlayGameSound(EMatchSound.GameStartVoice);
        }

        protected virtual void PlayStageBGM()
        {
            var map = ResolveCurrentStageMap();
            Debug.Log($"[{GetType().Name}] PlayStageBGM: {map}");
            SoundManager.Instance?.PlayBGM(map);
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
            return gameMode != EGameMode.Unknown ? gameMode : MatchModeResolver.ResolveCurrentGameMode();
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
                // The room's own state, and the settings the server is playing
                // under. Every mode needs them rather than only the ones that
                // happen to read them, so they are handled here and the modes are
                // left with what is theirs.
                case MessageType.Snapshot:
                    ApplyServerRoomSettings(obj);
                    break;
                case RUDPMessageTypes.WeaponReserve:
                    HandleWeaponReservation(obj, true);
                    break;
                case RUDPMessageTypes.WeaponRelease:
                    HandleWeaponReservation(obj, false);
                    break;

                // The server's ruling on a claim, rather than the claim itself.
                // The client only told the other clients about a claim before, so
                // who held a weapon was whatever each of them had been told. The
                // server holds it now, and this is the only thing that makes the
                // other clients honour what it accepted.
                case RUDPMessageTypes.WeaponReserved:
                    HandleWeaponReservation(obj, true);
                    break;
                case RUDPMessageTypes.WeaponReleased:
                    HandleWeaponReservation(obj, false);
                    break;
                case RUDPMessageTypes.WeaponDropped:
                    HandleWeaponDropped(obj);
                    break;
                case RUDPMessageTypes.ItemUsed:
                case RUDPMessageTypes.ItemUseRefused:
                    HandleInstantItemRuling(obj, messageType);
                    break;
                case RUDPMessageTypes.WeaponPickup:
                    HandleWeaponPickup(obj);
                    break;
                case RUDPMessageTypes.ItemSpawn:
                    HandleItemSpawn(obj);
                    break;
                case RUDPMessageTypes.ItemDespawn:
                    HandleItemDespawn(obj);
                    break;
                case RUDPMessageTypes.ItemStateSync:
                    HandleFieldItemStateSync(obj);
                    break;
                case RUDPMessageTypes.ItemUse:
                    HandleItemUse(obj);
                    break;
                case RUDPMessageTypes.ItemPickup:
                    HandleItemPickup(obj);
                    break;
                case RUDPMessageTypes.PlayerDeath:
                    HandleAuthoritativePlayerDeath(obj);
                    break;
                case RUDPMessageTypes.PlayerBuff:
                    HandlePlayerBuff(obj);
                    break;
                case RUDPMessageTypes.PlayerDebuff:
                    HandlePlayerDebuff(obj);
                    break;

                // The server saying the match is over.
                // <para>
                // This was in four of the five mode scripts, which means it was
                // in whichever ones somebody remembered and not in whichever they
                // did not, and a mode with no case ends its match on a guess
                // about its own clock. It is one message and one question, so it
                // is answered once here and a mode is left with how its result
                // looks.
                // </para>
                case MessageType.MatchEndNotification:
                    HandleServerMatchEnd(obj);
                    break;

                // The server's per second statement of the match state.
                // <para>
                // The network layer deliberately does nothing with this one, on the
                // understanding that the match scripts read it from the raw
                // message, and they had no case for it. So the message was dropped:
                // the clock was only ever set once from the room settings and then
                // counted down locally, which is a clock the client owns rather
                // than one the room is reporting.
                // </para>
                case RUDPMessageTypes.GameStateSync:
                    HandleGameStateSync(obj);
                    break;
            }
        }

        /// <summary>
        /// Takes the match clock the server is reporting.
        /// <para>
        /// The room settings carry the length and the time left once, when the
        /// client arrives. This is the message that keeps saying what is left, so
        /// a client whose countdown drifted gets put back on the room's number
        /// rather than ending the match on a clock of its own.
        /// </para>
        /// </summary>
        private void HandleGameStateSync(JObject json)
        {
            if (timer == null)
            {
                return;
            }

            var remaining = ReadInt(json, "RemainingTime", -1);
            if (remaining < 0)
            {
                return;
            }

            timer.SyncServerTime(remaining, 0L);
        }

        /// <summary>
        /// The server has ended the match, and it says who won.
        /// <para>
        /// The guard is here rather than in each mode, because it was in each mode
        /// and one of them was missing it. A match that ends twice on this client
        /// is a match that shows a result twice and leaves one behind.
        /// </para>
        /// </summary>
        private void HandleServerMatchEnd(JObject json)
        {
            if (!TryBeginMatchEnd())
            {
                return;
            }

            OnServerMatchEnd(json);
            ScheduleResultSceneTransition(gotoResultSceneWaitTime);
        }

        /// <summary>
        /// Ends the match on this client, for the decisions only this client
        /// makes.
        /// <para>
        /// Offline there is no server to ask, so the local clock and the local
        /// score decide. The guard is the base's, because it was in each mode's
        /// own copy of this and one of them was missing it.
        /// </para>
        /// </summary>
        protected void EndMatchLocally(JObject result, float transitionDelay = 0f)
        {
            if (!TryBeginMatchEnd())
            {
                return;
            }

            OnServerMatchEnd(result);
            ScheduleResultSceneTransition(transitionDelay);
        }

        /// <summary>
        /// What a mode does with a result the server has already decided.
        /// <para>
        /// The winner is the server's. A mode that worked it out from a score it
        /// had been keeping would be working it out from a copy, and the two would
        /// disagree about a result both are showing.
        /// </para>
        /// </summary>
        protected virtual void OnServerMatchEnd(JObject json)
        {
            // A solo mode has no winning team: the server reports "None" there and
            // names the winning player instead. Reading only the team is what made
            // this log say the winner was None for a match a player had won.
            var winningTeam = json?["WinningTeam"]?.ToString() ?? string.Empty;
            var winningPlayer = json?["WinningPlayerId"]?.ToString()
                ?? json?["Winner"]?.ToString()
                ?? string.Empty;
            var myTeam = json?["MyTeam"]?.ToString() ?? "Spectator";

            var winner = !string.IsNullOrEmpty(winningPlayer)
                ? winningPlayer
                : string.IsNullOrEmpty(winningTeam) || winningTeam == "None" ? "Draw" : winningTeam;

            Debug.Log($"[{GetType().Name}] The server ended the match: winner={winner}, myTeam={myTeam}");
        }

        /// <summary>
        /// Adopts the settings the server says this room is playing under.
        /// <para>
        /// A setting is not something a client guesses. Anything the server
        /// configured, and a client that keeps a value of its own for, is a
        /// number the two sides can disagree about and the player sees whichever
        /// one the client happens to be using.
        /// </para>
        /// </summary>
        protected virtual void ApplyServerRoomSettings(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var multiplier = json["HealthMultiplier"]?.ToObject<float>();
            if (multiplier.HasValue)
            {
                MatchModeResolver.AdoptServerHealthMultiplier(multiplier.Value);
            }

            var seconds = json["MatchTimeSeconds"]?.ToObject<float>();
            if (seconds.HasValue && seconds.Value > 0f && float.IsFinite(seconds.Value))
            {
                serverMatchSeconds = seconds.Value;

                // The room's clock is how long is left, not how long the match is,
                // and it moves every second. It goes to the timer as the time left
                // rather than as the match length: the length is set once, and the
                // time left is synced. Setting the length on every message put the
                // countdown back to the start about once a second, so the clock
                // never moved and the match never ended on the client at all.
                if (timer != null)
                {
                    timer.SyncServerTime(Mathf.CeilToInt(seconds.Value), 0L);
                }
            }

            // The length is a different number and it does not move, so it is set
            // when it differs rather than on every message. A client that joined
            // late was otherwise shown the time remaining as the whole match.
            var length = json["MatchLengthSeconds"]?.ToObject<float>();
            if (timer != null && length.HasValue && length.Value > 0f && float.IsFinite(length.Value) &&
                !Mathf.Approximately(timer.matchDurationSeconds, length.Value))
            {
                timer.SetMatchDuration(length.Value);
            }

            // The running team totals, for a mode that shows a score.
            var redKills = json["RedTeamKills"]?.ToObject<int>();
            var blueKills = json["BlueTeamKills"]?.ToObject<int>();
            if (redKills.HasValue || blueKills.HasValue)
            {
                ApplyServerTeamKills(redKills ?? 0, blueKills ?? 0);
            }

            // A mission's own numbers: how far the objective has got and how many
            // goes the party has left. The room is what holds them, and a client
            // showing an objective of its own making would not be showing this
            // match.
            var allowRespawn = json["AllowRespawn"]?.ToObject<bool>();
            if (allowRespawn.HasValue)
            {
                MatchModeResolver.AdoptMissionRespawn(allowRespawn.Value);
            }

            var objectiveProgress = json["ObjectiveProgress"]?.ToObject<int>();
            var objectiveCount = json["ObjectiveCount"]?.ToObject<int>();
            var lifeRemaining = json["LifeRemaining"]?.ToObject<int>();
            var lifeCount = json["LifeCount"]?.ToObject<int>();
            if (objectiveProgress.HasValue || objectiveCount.HasValue ||
                lifeRemaining.HasValue || lifeCount.HasValue)
            {
                ApplyServerMissionProgress(
                    objectiveProgress ?? 0,
                    objectiveCount ?? 0,
                    lifeRemaining ?? 0,
                    lifeCount ?? 0);
            }

            // A mode that reads one of the published limits does it here rather
            // than with its own case for this message, because a case here means
            // the base never sees the message for that mode at all.
            OnServerRoomSettings(json);
        }

/// <summary>
        /// The objective and the lives the room is publishing.
        /// <para>
        /// The numbers come from the room rather than from this client, so a
        /// mission readout is the mission that is being played and not one this
        /// client made up. They are also adopted onto the rule, because the rule is
        /// what this client judges the end of the match by and a rule that kept its
        /// own default objective would end a mission on a limit the room was never
        /// started under.
        /// </para>
        /// </summary>
        protected virtual void ApplyServerMissionProgress(
            int objectiveProgress,
            int objectiveCount,
            int lifeRemaining,
            int lifeCount)
        {
            missionObjectiveProgress = Mathf.Max(0, objectiveProgress);
            missionObjectiveCount = Mathf.Max(0, objectiveCount);
            missionLifeRemaining = Mathf.Max(0, lifeRemaining);
            missionLifeCount = Mathf.Max(0, lifeCount);

            AdoptMissionRule();
        }

        /// <summary>
        /// Puts the room's numbers onto this client's mission rule.
        /// <para>
        /// They are held on the base rather than on the mission mode, because a
        /// mission is also settled by the rule the result screen reads, and a copy
        /// kept on the mode script would be a second set of numbers that can
        /// disagree with the rule the match is judged by.
        /// </para>
        /// </summary>
        private void AdoptMissionRule()
        {
            var room = ResolveCurrentMatchRoom();
            if (room?.Rule is not MissionMatchRule rule)
            {
                return;
            }

            rule.ObjectiveCount = missionObjectiveCount;
            rule.ObjectiveProgress = missionObjectiveProgress;
            rule.LifeCount = missionLifeCount;
            rule.LifeRemaining = missionLifeRemaining;
        }

        /// <summary>How much of a mission's objective the room has published done.</summary>
        protected int missionObjectiveProgress;

        /// <summary>How much of a mission's objective the room was started with.</summary>
        protected int missionObjectiveCount;

        /// <summary>How many goes the room says the party has left.</summary>
        protected int missionLifeRemaining;

        /// <summary>How many goes the room was started with.</summary>
        protected int missionLifeCount;

        /// <summary>
        /// The published limits that belong to this mode.
        /// <para>
        /// The room publishes the kill limit its rule is running on, under a
        /// different key per mode, and only the capture flag mode was reading it.
        /// A client rule carries a hardcoded limit of its own, so every other mode
        /// judged the end of the match on a number the room was never started
        /// under: a room configured for first to three ended the match at twenty.
        /// </para>
        /// </summary>
        protected virtual void OnServerRoomSettings(JObject json)
        {
            var room = ResolveCurrentMatchRoom();
            var rule = room?.Rule;
            if (rule == null)
            {
                return;
            }

            // The keys are alternatives rather than a preference order: a room
            // publishes the one its mode uses, and an older server may publish
            // none of them, in which case the local limit stands.
            var limit = ReadInt(json, "KillLimit", 0);
            if (limit <= 0)
            {
                limit = ReadInt(json, "TeamKillLimit", 0);
            }

            if (limit <= 0)
            {
                limit = ReadInt(json, "WinConditionKill", 0);
            }

            if (limit <= 0)
            {
                return;
            }

            switch (rule)
            {
                case DeathMatchRule deathMatch:
                    ApplyKillLimit(deathMatch.KillCount, limit, () => deathMatch.KillCount = limit, "DeathMatch");
                    break;
                case TeamDeathMatchRule teamDeathMatch:
                    ApplyKillLimit(teamDeathMatch.KillCount, limit, () => teamDeathMatch.KillCount = limit, "TeamDeathMatch");
                    break;
                case ArmsMatchRaceRule armsRace:
                    ApplyKillLimit(armsRace.KillCount, limit, () => armsRace.KillCount = limit, "ArmsRace");
                    break;
            }
        }

        private static void ApplyKillLimit(int current, int adopted, Action apply, string modeName)
        {
            if (current == adopted)
            {
                return;
            }

            apply();
            Debug.Log($"[{modeName}] Took the kill limit from the server: {adopted}");
        }

        /// <summary>
        /// The room's own tally of what each team has scored.
        /// <para>
        /// A client counts its own kills and hopes, and sends its own claims, so a
        /// score it worked out for itself is a score of its own making for the
        /// whole match. The room keeps the real one.
        /// <para>
        /// The tally is written into the room's MatchData here rather than in each
        /// mode, because MatchData is what the rules read to decide a match is
        /// over. A mode that did its own counting left every mode but team death
        /// match judging the end of its match on a number it made up.
        /// </para>
        /// </summary>
        protected virtual void ApplyServerTeamKills(int red, int blue)
        {
            var room = ResolveCurrentMatchRoom();
            room?.MatchData?.AdoptServerTeamKills(red, blue);

            OnServerTeamKillsApplied(red, blue);
        }

        /// <summary>
        /// What a mode does with the room's tally once it is in MatchData.
        /// <para>
        /// The data is shared by every mode, so the mode that shows it a score is
        /// the one that has to be told. The base does nothing because a solo mode
        /// has no team score to show.
        /// </para>
        /// </summary>
        protected virtual void OnServerTeamKillsApplied(int red, int blue)
        {
        }

        /// <summary>
        /// Tells the server this player is back.
        /// <para>
        /// A respawn was entirely local. The client rebuilt its player and the
        /// server kept the health it had already been reduced to, which is zero, so
        /// from then on the player could not be hit at all: the hit path refuses a
        /// target whose health is not above zero. Only capture the flag ever said
        /// so, which left death match, team death match and team survival with a
        /// player who was untouchable for the rest of the match.
        /// </para>
        /// <para>
        /// No position is sent. Where a player comes back is the server's call, and
        /// it is the one thing about a respawn a client must not be believed on: a
        /// client that chose would be able to choose anywhere.
        /// </para>
        /// </summary>
        protected void ReportRespawnToServer()
        {
            var local = ResolveLocalPlayer();
            if (local != null)
            {
                // Tell the room as well as the server. Every consumer that reacts
                // to a respawn, the health bar and the countdown canvas among them,
                // listens on the registry rather than on the network, so a respawn
                // that only went to the server left the player looking dead on
                // their own screen while they were standing there alive.
                PlayerRegistry.Instance?.PublishPlayerRespawned(local);
            }

            var manager = matchNetworkManager;
            if (!IsOnlineMatch() || manager == null || !manager.IsConnected())
            {
                return;
            }

            var playerId = AccountManager.Instance?.CurrentProfile?.GlobalUserId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var room = ResolveCurrentMatchRoom();
            manager.SendToServer(RUDPMessageBuilder.CreatePlayerRespawn(
                playerId, Vector2.zero, room?.Id));
        }

        /// <summary>
        /// The match room this script is running, online or offline.
        /// </summary>
        protected MatchRoom ResolveCurrentMatchRoom()        {
            var manager = MatchRoomManager();
            if (manager == null)
            {
                return null;
            }

            if (IsOnlineMatch() && manager.OnlineMatchRoom != null)
            {
                return manager.OnlineMatchRoom;
            }

            if (manager.OfflineMatchRoom != null)
            {
                return manager.OfflineMatchRoom;
            }

            return manager.OnlineMatchRoom ?? manager.OfflineMatchRoom;
        }

        protected virtual void HandleItemUse(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var itemId = json["ItemId"]?.ToString() ?? "";
            var itemType = json["ItemType"]?.ToString() ?? "";
            var effect = json["Effect"]?.ToString() ?? "";

            Debug.Log($"[{GetType().Name}] ItemUse received: player={playerId}, item={itemId}, type={itemType}, effect={effect}");
        }

        protected virtual void HandleItemPickup(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var itemType = json["ItemType"]?.ToString() ?? "";
            var spawnPointId = ReadInt(json, "SpawnPointId", -1);
            var value = ReadFloat(json, "Value");
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = 0f;
            }
            var player = ResolvePlayerById(playerId);

            if (player != null && value > 0f && (string.Equals(itemType, nameof(HealItem), StringComparison.OrdinalIgnoreCase) ||
                                                  string.Equals(itemType, "HealthKit", StringComparison.OrdinalIgnoreCase)))
            {
                player.Heal(value);
                Debug.Log($"[{GetType().Name}] Heal item applied: player={playerId}, value={value}, type={itemType}");
            }

            if (spawnPointId >= 0 && ItemSpawnPoints.TryGetPoint(spawnPointId, out var spawnPoint))
            {
                spawnPoint.DespawnItem();
                Debug.Log($"[{GetType().Name}] ItemPickup received: player={playerId}, type={itemType}, spawnPoint={spawnPointId}");
                return;
            }

            if (itemSpawnPoints != null)
            {
                var fallbackSpawnPoints = itemSpawnPoints.GetComponent<ItemSpawnPoints>();
                if (fallbackSpawnPoints != null)
                {
                    fallbackSpawnPoints.DespawnAllItems();
                }
            }

            Debug.Log($"[{GetType().Name}] ItemPickup received but spawn point was not found: player={playerId}, type={itemType}, spawnPoint={spawnPointId}");
        }

        protected virtual void HandleItemSpawn(JObject json)
        {
            var itemTypeStr = json["ItemType"]?.ToString() ?? "";
            var spawnPointId = ReadInt(json, "SpawnPointId", -1);

            if (!FieldItemTypeNames.TryParse(itemTypeStr, out var itemType))
            {
                Debug.LogWarning($"[{GetType().Name}] ItemSpawn ignored because item type was invalid: {itemTypeStr}");
                return;
            }

            // The server names the item it is about, so the network manager is
            // told about it under that name. It used to be spawned only through a
            // spawn point, which the server's message does not have to name, so a
            // spawn the server put into the world was something only the client
            // that had created it could see.
            var itemId = json["ItemId"]?.ToString() ?? string.Empty;
            var networkItems = WorldItemNetworkManager.Instance;

            if (ItemSpawnPoints.TryGetPoint(spawnPointId, out var point))
            {
                point.SpawnItem(itemType);
            }
            else
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] ItemSpawn had no spawn point, spawning by position: " +
                    $"type={itemType}, spawnPoint={spawnPointId}");
            }

            if (networkItems != null)
            {
                var position = new Vector3(
                    ReadFloat(json, "PositionX", ReadFloat(json, "PosX")),
                    ReadFloat(json, "PositionY", ReadFloat(json, "PosY")),
                    0f);
                networkItems.AdoptServerSpawn(itemId, itemType, position);
            }

            Debug.Log($"[{GetType().Name}] ItemSpawn received: type={itemType}, spawnPoint={spawnPointId}, item={itemId}");
        }

        protected virtual void HandleItemDespawn(JObject json)
        {
            var spawnPointId = ReadInt(json, "SpawnPointId", -1);
            var itemId = json["ItemId"]?.ToString() ?? string.Empty;

            // The server names the item that went, so it is removed by that name.
            // Falling back to clearing every spawn point when the message named
            // none would take items off the map that the server still had, which
            // is the opposite of what a despawn is for.
            var removedById = false;
            var networkItems = WorldItemNetworkManager.Instance;
            if (networkItems != null && !string.IsNullOrWhiteSpace(itemId))
            {
                removedById = networkItems.DespawnItemByServer(itemId);
            }

            if (spawnPointId >= 0 && ItemSpawnPoints.TryGetPoint(spawnPointId, out var point))
            {
                point.DespawnItem();
            }

            Debug.Log(
                $"[{GetType().Name}] ItemDespawn received: item={itemId}, " +
                $"spawnPoint={spawnPointId}, removedById={removedById}");
        }

        /// <summary>
        /// Takes the whole set of field items the server holds as the truth.
        /// <para>
        /// A client that joins part way through a match, or whose own list has
        /// drifted, has no way to know what is on the ground. The server is the
        /// only side that knows, so the state it reports is adopted wholesale
        /// rather than merged: merging would leave items the server has removed
        /// sitting on this client's map.
        /// </para>
        /// </summary>
        protected virtual void HandleFieldItemStateSync(JObject json)
        {
            var items = json["Items"] as JArray;
            var networkItems = WorldItemNetworkManager.Instance;
            if (items == null || networkItems == null)
            {
                Debug.LogWarning($"[{GetType().Name}] FieldItemStateSync carried no items or no network manager.");
                return;
            }

            networkItems.AdoptServerState(items);
            Debug.Log($"[{GetType().Name}] FieldItemStateSync adopted {items.Count} items from the server.");
        }

        protected virtual void HandleWeaponReservation(JObject json, bool reserved)
        {
            var weaponType = json["WeaponType"]?.ToString() ?? "";
            var playerId = json["ReservedByPlayerId"]?.ToString() ?? json["PlayerId"]?.ToString() ?? "";
            var position = new Vector2(
                ReadFloat(json, "PosX"),
                ReadFloat(json, "PosY")
            );

            if (!FieldWeaponController.TryFindMatchingWeapon(weaponType, position, out var controller))
            {
                Debug.Log($"[{GetType().Name}] Weapon reservation ignored because no matching weapon was found: type={weaponType}, pos={position}");
                return;
            }

            if (reserved)
            {
                controller.ApplyNetworkReservation(playerId);
            }
            else
            {
                controller.ApplyNetworkRelease(playerId);
            }
        }

        /// <summary>
        /// Puts a weapon on the ground for everybody else.
        /// <para>
        /// A dropped weapon used to be something the dropping client drew for
        /// itself, so the weapon was on the ground for one player out of the
        /// room. The server holds it now and this is what puts it in front of
        /// the others, which is what makes it a weapon anybody can pick up.
        /// </para>
        /// </summary>
        protected virtual void HandleWeaponDropped(JObject json)
        {
            var weaponType = json["WeaponType"]?.ToString() ?? "";
            var position = new Vector2(
                ReadFloat(json, "PosX"),
                ReadFloat(json, "PosY"));

            if (string.IsNullOrWhiteSpace(weaponType))
            {
                Debug.LogWarning($"[{GetType().Name}] WeaponDropped ignored because it named no weapon type.");
                return;
            }

            if (!FieldWeaponController.TryFindMatchingWeapon(weaponType, position, out var existing))
            {
                Debug.Log($"[{GetType().Name}] WeaponDropped has no weapon to describe: type={weaponType}, pos={position}");
                return;
            }

            // The dropper already removed its own copy when it dropped, so the
            // weapon is only put back for the players who did not draw it.
            Debug.Log($"[{GetType().Name}] WeaponDropped: type={weaponType}, pos={position}");
        }

        /// <summary>
        /// Applies what the server decided a spent instant item was worth.
        /// <para>
        /// The client spent the item on its own screen, so it has already shown
        /// the effect it guessed at. The ruling carries the health the server
        /// holds, which is the number that decides, and adopting it is what stops
        /// the two disagreeing. A refusal is the same message with nothing
        /// restored, so the client falls back to what the server says it has.
        /// </para>
        /// </summary>
        protected virtual void HandleInstantItemRuling(JObject json, string messageType)
        {
            var granted = !string.Equals(messageType, RUDPMessageTypes.ItemUseRefused, StringComparison.Ordinal);
            var playerId = json["PlayerId"]?.ToString() ?? json["PlayerID"]?.ToString() ?? string.Empty;
            var player = ResolvePlayerById(playerId);

            if (player == null || !IsLocalPlayer(player))
            {
                // The ruling is about the player who spent the item, and that is
                // only ever the local one. Everybody else already saw the item
                // leave their hands.
                return;
            }

            var health = json["Health"]?.Value<int>();
            if (!health.HasValue)
            {
                Debug.LogWarning($"[{GetType().Name}] Instant item ruling carried no health; leaving the local value alone.");
                return;
            }

            if (player is IDamageable damageable)
            {
                damageable.ApplyServerHealth(
                    health.Value,
                    json["MaxHealth"]?.Value<int>() ?? health.Value);
            }

            Debug.Log(
                $"[{GetType().Name}] Instant item ruling {(granted ? "granted" : "refused")}: " +
                $"item={json["ItemType"]}, health={health.Value}");
        }

        protected virtual void HandleAuthoritativePlayerDeath(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? json["PlayerID"]?.ToString();
            var target = ResolvePlayerById(playerId);
            if (target == null || !IsLocalPlayer(target))
            {
                return;
            }

            // The server says whether the player did this to themselves, and the
            // reason is what the death is shown as. It was read as Unknown every
            // time, so a player who killed themselves was presented as a casualty
            // and a self kill looked the same as being shot, which is the one case
            // where a player needs to be able to tell.
            var reason = ReadBool(json, "IsSelfInflicted")
                ? EDeadReason.Suicide
                : EDeadReason.Unknown;

            var agent = target as PlayerAgent;
            agent?.ApplyServerDeath(reason);
        }

        protected virtual void HandleWeaponPickup(JObject json)
        {
            var weaponType = json["WeaponType"]?.ToString() ?? "";
            var playerId = json["ReservedByPlayerId"]?.ToString() ?? json["PlayerId"]?.ToString() ?? "";
            var position = new Vector2(
                ReadFloat(json, "PosX"),
                ReadFloat(json, "PosY")
            );

            if (!FieldWeaponController.TryFindMatchingWeapon(weaponType, position, out var controller))
            {
                Debug.Log($"[{GetType().Name}] Weapon pickup ignored because no matching weapon was found: type={weaponType}, pos={position}");
                return;
            }

            controller.ApplyNetworkPickup(playerId, weaponType, position);
        }

        protected virtual void HandlePlayerBuff(JObject json)
        {
            var targetPlayerId = json["PlayerId"]?.ToString() ?? json["TargetPlayerId"]?.ToString() ?? "";
            var player = ResolvePlayerById(targetPlayerId);
            var buffType = json["BuffType"]?.ToString() ?? "";
            var duration = Math.Max(0, ReadInt(json, "Duration"));
            var value = ReadFloat(json, "Value");
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = 0f;
            }

            if (player != null)
            {
                if (IsLocalPlayer(player))
                {
                    Debug.Log($"[{GetType().Name}] PlayerBuff ignored for local player echo: {json}");
                    return;
                }

                // The timed buffs live on the powerup contract, which both player
                // trees already carry. Casting to one tree here meant a buff the
                // server sent for a real player was dropped on the floor.
                var powerup = player as IPowerupable;
                if (powerup == null)
                {
                    Debug.Log($"[{GetType().Name}] PlayerBuff had no receiver for {targetPlayerId}");
                    return;
                }

                switch (buffType)
                {
                    case "HpRecovery":
                        player.Heal(value);
                        break;
                    case "BulletEnhance":
                    case "PowerUp":
                    case "PowerUpItem":
                    case "AttackUp":
                        powerup.IncreaseAttack(duration);
                        break;
                    case "DefenceUp":
                    case "DefenceUpItem":
                    case "DefenseUpItem":
                    case "DefenseUp":
                        powerup.IncreaseDefense(duration);
                        break;
                    case "SpeedUp":
                    case "SpeedUpItem":
                        powerup.SpeedUp(duration);
                        break;
                    case "Stealth":
                    case "StealthItem":
                    case "Invisible":
                        powerup.Invisible(duration);
                        break;
                    case "GrenadePack":
                    case "NormalGrenadePack":
                        powerup.RefillGrenade(EGrenadeType.Normal);
                        break;
                    case "PowerGrenadePack":
                        powerup.RefillGrenade(EGrenadeType.Power);
                        break;
                    case "ClusterGrenadePack":
                        powerup.RefillGrenade(EGrenadeType.Cluster);
                        break;
                    case "MagnetGrenadePack":
                        powerup.RefillGrenade(EGrenadeType.Magnetic);
                        break;
                    case "MineGrenadePack":
                        powerup.RefillGrenade(EGrenadeType.Mine);
                        break;
                }
            }

            Debug.Log($"[{GetType().Name}] PlayerBuff received: {json}");
        }

        protected virtual void HandlePlayerDebuff(JObject json)
        {
            var targetPlayerId = json["PlayerId"]?.ToString() ?? json["TargetPlayerId"]?.ToString() ?? "";
            var player = ResolvePlayerById(targetPlayerId);
            var debuffType = json["DebuffType"]?.ToString() ?? "";
            var duration = Math.Max(0, ReadInt(json, "Duration"));

            if (player != null)
            {
                if (IsLocalPlayer(player))
                {
                    Debug.Log($"[{GetType().Name}] PlayerDebuff ignored for local player echo: {json}");
                    return;
                }

                switch (debuffType)
                {
                    case "PoisonBullet":
                        // Only the player tree carries the poisoned bullet. It is
                        // reached through the root object rather than a cast to
                        // that type, so a player built from the other tree is
                        // skipped rather than being the thing that throws.
                        (player?.Root != null ? player.Root.GetComponent<AbstractPlayer>() : null)
                            ?.PoisonBullet(duration);
                        break;
                }
            }

            Debug.Log($"[{GetType().Name}] PlayerDebuff received: {json}");
        }

        /// <summary>
        /// The local player, whichever component tree carries it.
        /// <para>
        /// It used to ask for one specific type, which no player prefab carries,
        /// so this returned null on a live match: the local player was never the
        /// local player, and everything keyed off it, the kill feed's "you killed"
        /// colouring and the respawn countdown among them, had nothing to work on.
        /// </para>
        /// </summary>
        protected virtual IBattlePlayer ResolveLocalPlayer()
        {
            if (player == null)
            {
                return null;
            }

            return player.GetComponent<IBattlePlayer>();
        }

        protected virtual bool IsLocalPlayer(IBattlePlayer target)
        {
            var local = ResolveLocalPlayer();
            return local != null && target != null && ReferenceEquals(local, target);
        }

        protected virtual void OnDestroy()
        {
            oneSecInvtervalTimer.Stop();
            oneSecInvtervalTimer.Dispose();

            oneMiniteIntervalTimer.Stop();
            oneMiniteIntervalTimer.Dispose();

            if (clientNetworkManager != null)
            {
                clientNetworkManager.UdpMessageReceived -= OnNetworkDataRecved;
                clientNetworkManager.ServerMatchRulingReceived -= OnNetworkDataRecved;
                clientNetworkManager.MatchUdpConnectionChanged -= OnClientMatchUdpConnectionChanged;
            }

            matchNetworkManager?.Disconnect();

            matchNetworkSubscribed = false;
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

        protected virtual IBattlePlayer ResolvePlayerById(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return ResolveLocalPlayer();
            }

            if (ResolveLocalPlayer() is IBattlePlayer localPlayer &&
                string.Equals(localPlayer.UniqueID().ToString(), playerId, StringComparison.OrdinalIgnoreCase))
            {
                return localPlayer;
            }

            if (PlayerRegistry.Instance != null &&
                Guid.TryParse(playerId, out var playerGuid) &&
                PlayerRegistry.Instance.TryGetPlayer(playerGuid, out var registryPlayer))
            {
                return registryPlayer;
            }

            return null;
        }

        private void EnsureMasterDataReferences()
        {
            if (prefabMasterData == null)
            {
                prefabMasterData = FindFirstObjectByType<PlayerPrefabMasterData>();
                if (prefabMasterData == null)
                {
                    prefabMasterData = Resources.Load<PlayerPrefabMasterData>("MasterData/Player/PlayerPrefabMasterData");

                    // Some battle scenes override the serialized reference with
                    // an empty value. Keep player creation independent from that
                    // scene override and recover from the Resources folder.
                    if (prefabMasterData == null)
                    {
                        var candidates = Resources.LoadAll<PlayerPrefabMasterData>("MasterData/Player");
                        if (candidates != null && candidates.Length > 0)
                        {
                            prefabMasterData = candidates[0];
                        }
                    }
                }
            }

            if (uiCanvasMasterData == null)
            {
                uiCanvasMasterData = FindFirstObjectByType<CanvasMasterData>();
                if (uiCanvasMasterData == null)
                {
                    uiCanvasMasterData = Resources.Load<CanvasMasterData>("MasterData/UI/CanvasMasterData");
                }
            }
        }


        [Button("リザルトテスト")]
        public void GoToResult()
        {
            if (sceneTransitionRequested)
            {
                return;
            }

            var nextSceneName = GeneralSceneMasterData.Instance().ResultScene();
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning($"[{GetType().Name}] Result scene is not configured.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[{GetType().Name}] Result scene is not in build settings: {nextSceneName}");
                return;
            }

            sceneTransitionRequested = true;
            SceneManager.LoadSceneAsync(nextSceneName);

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

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[{GetType().Name}] Scene is not in build settings: {nextSceneName}. reason={reason}");
                return;
            }

            if (sceneTransitionRequested)
            {
                Debug.Log($"[{GetType().Name}] Scene transition already requested. reason={reason}");
                return;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.Log($"[{GetType().Name}] RequestSceneTransition -> {nextSceneName} reason={reason}");
            }

            sceneTransitionRequested = true;
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

            var battlePlayer = player.GetComponent<IBattlePlayer>();
            if (battlePlayer == null)
            {
                return "Draw";
            }

            var team = battlePlayer.Team();
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
            if (sceneTransitionRequested)
            {
                return;
            }

            var nextSceneName = GeneralSceneMasterData.Instance().TitleScene();
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning($"[{GetType().Name}] Title scene is not configured.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[{GetType().Name}] Title scene is not in build settings: {nextSceneName}");
                return;
            }

            sceneTransitionRequested = true;
            SceneManager.LoadSceneAsync(nextSceneName);
        }

        public MatchRoomManager MatchRoomManager()
        {
            try
            {
                return DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] MatchRoomManager is not available: {ex.Message}");
                return null;
            }
        }

        [Button("リスポーンUI表示")]
        public void ShowReSpawnUI(float time = 5.0f)
        {
            EnsureRespawnCanvas();
        }

        public virtual void OnMyPlayerDead()
        {
            Debug.Log($"[{GetType().Name}] My player dead");
        }

        protected static float ReadFloat(JObject json, string key, float fallback = 0f)
        {
            if (json == null || string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            try
            {
                var value = json[key]?.ToObject<float>();
                return value.HasValue && float.IsFinite(value.Value) ? value.Value : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        protected static int ReadInt(JObject json, string key, int fallback = 0)
        {
            if (json == null || string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            try
            {
                return json[key]?.ToObject<int>() ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        protected static bool ReadBool(JObject json, string key, bool fallback = false)
        {
            if (json == null || string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            try
            {
                return json[key]?.ToObject<bool>() ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        /*
        public MatchRoom MatchRoom()
        {
            return MatchRoomManager.Instance.MatchRoom;
        }

        */
    }
}
