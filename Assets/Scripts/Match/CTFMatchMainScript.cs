using System;
using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;

//using KanKikuchi.AudioManager;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Sirenix.Serialization;
using Zenject;
using Newtonsoft.Json.Linq;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class CTFMatchMainScript : AbstractMatchMainScript, ICTFMatchMainScript
    {
        // Singleton instance for UI to access
        public static CTFMatchMainScript Instance { get; private set; }

        // CTF Events for UI
        public event Action<ETeam> OnFlagCaptured;
        public event Action<ETeam> OnFlagReturned;
        public event Action<ETeam> OnFlagLost;
        public event Action<ETeam, string> OnFlagPickedUp; // team, playerName

        [InjectOptional] private IEffectService effectService;
        [InjectOptional] private EffectPrefabMasterData effectPrefabMasterData;

        // ネットワークマネージャー
        private MatchRUDPServerNetworkManager networkManager;

        //[SerializeField][OdinSerialize][Inject] ClientSessionData data;
        private MatchRoom matchRoom;
        private readonly HashSet<string> processedFlagEventKeys = new HashSet<string>();
        private readonly Queue<string> recentFlagEventKeys = new Queue<string>();
        private const int MaxRecentFlagEventKeys = 128;

        //public AudioClip captureFlagSound;
        //public AudioClip returnFlagSound;

        [SerializeField]
        public GameObject BlueTeamReSpawnPoints;
        [SerializeField]
        public GameObject RedTeamReSpawnPoints;

        [SerializeField] [Required] public FlagStand redTeamFlagStand, blueTeamFlagStand;
        private readonly Dictionary<FlagStand, FlagController> boundFlagControllers = new Dictionary<FlagStand, FlagController>();

        public new void Start()
        {
            base.Start();
            // Singleton設定
            Instance = this;

            // ネットワークマネージャーを取得
            try
            {
                networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CTFMatchMainScript: Failed to resolve MatchRUDPServerNetworkManager: {ex.Message}");
                networkManager = null;
            }

            Invoke("GameSetup", 0.1f);
        }

        public void CreateDebugRoom()
        {
            Debug.Log("[CTF] CreateDebugRoom");
        }

        void GameSetup()
        {
            PlayGameStartVoice();

            SubscribeEvent();
            BindFlagStand(redTeamFlagStand);
            BindFlagStand(blueTeamFlagStand);

            CreateNewMyPlayer();
            CreateOtherPlayers();
            SetUpUI();
            ApplyRuleSettings();
            CTFScoreUIManager.Instance?.StartMatch();

            redTeamFlagStand.SetFlag();
            blueTeamFlagStand.SetFlag();

            Debug.Log("GameStarteted");
        }

        private void SetUpUI()
        {
            if (CTFScoreUIManager.Instance != null)
            {
                CTFScoreUIManager.Instance.PrepareMatch();
            }
        }

        private void BindFlagStand(FlagStand stand)
        {
            if (stand == null)
            {
                return;
            }

            stand.FlagSpawned -= HandleFlagSpawned;
            stand.FlagCaptured -= HandleFlagCapturedFromStand;
            stand.FlagSpawned += HandleFlagSpawned;
            stand.FlagCaptured += HandleFlagCapturedFromStand;
        }

        private void UnbindFlagStand(FlagStand stand)
        {
            if (stand == null)
            {
                return;
            }

            stand.FlagSpawned -= HandleFlagSpawned;
            stand.FlagCaptured -= HandleFlagCapturedFromStand;

            if (boundFlagControllers.TryGetValue(stand, out var controller))
            {
                UnbindFlagController(controller);
                boundFlagControllers.Remove(stand);
            }
        }

        private void HandleFlagSpawned(FlagStand stand, FlagController controller)
        {
            if (stand == null || controller == null)
            {
                return;
            }

            if (boundFlagControllers.TryGetValue(stand, out var previousController) && previousController != null && previousController != controller)
            {
                UnbindFlagController(previousController);
            }

            boundFlagControllers[stand] = controller;
            BindFlagController(controller);
        }

        private void BindFlagController(FlagController controller)
        {
            if (controller == null)
            {
                return;
            }

            controller.EnemyPickedUp -= HandleFlagEnemyPickedUp;
            controller.ReturnedToBase -= HandleFlagReturnedToBase;
            controller.Dropped -= HandleFlagDropped;

            controller.EnemyPickedUp += HandleFlagEnemyPickedUp;
            controller.ReturnedToBase += HandleFlagReturnedToBase;
            controller.Dropped += HandleFlagDropped;
        }

        private void UnbindFlagController(FlagController controller)
        {
            if (controller == null)
            {
                return;
            }

            controller.EnemyPickedUp -= HandleFlagEnemyPickedUp;
            controller.ReturnedToBase -= HandleFlagReturnedToBase;
            controller.Dropped -= HandleFlagDropped;
        }

        private void HandleFlagEnemyPickedUp(FlagController flagController, AbstractPlayer player)
        {
            if (flagController == null)
            {
                return;
            }

            PlayerFlagPickedUp(flagController.team, player != null ? player.gameObject.name : string.Empty, false);
        }

        private void HandleFlagReturnedToBase(FlagController flagController, AbstractPlayer player, FlagController.EFlagReturnReason reason)
        {
            if (flagController == null)
            {
                return;
            }

            if (reason == FlagController.EFlagReturnReason.CapturedAtBase)
            {
                return;
            }

            PlayFlagEffect(effectPrefabMasterData != null ? effectPrefabMasterData.flagReturnEffect : null, flagController.transform.position);
            PlayerFlagReturned(flagController.team, false);
        }

        private void HandleFlagDropped(FlagController flagController)
        {
            if (flagController == null)
            {
                return;
            }

            PlayFlagEffect(effectPrefabMasterData != null ? effectPrefabMasterData.HitEffect : null, flagController.transform.position);
            PlayerFlagLost(flagController.team, false);
        }

        private void HandleFlagCapturedFromStand(FlagStand stand, AbstractPlayer player)
        {
            if (stand == null)
            {
                return;
            }

            PlayFlagEffect(effectPrefabMasterData != null ? effectPrefabMasterData.flagReturnEffect : null, stand.transform.position);
            PlayerFlagCaptured(stand.Team, false);
            if (player != null)
            {
                player.EnemyFlagReturnedToBase(true);
            }
        }

        private static bool TryResolveTeam(in TeamEventPlayerInfo info, out ETeam team)
        {
            team = ETeam.NoTeam;
            if (info == null)
            {
                return false;
            }

            var teamText = info.Team?.ToString();
            return !string.IsNullOrWhiteSpace(teamText) && Enum.TryParse(teamText, out team);
        }

        private FlagStand GetFlagStand(ETeam team)
        {
            return team == ETeam.Red ? redTeamFlagStand : blueTeamFlagStand;
        }

        private void PlayFlagEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null)
            {
                return;
            }

            if (effectService != null)
            {
                effectService.PlayOneShotEffect(effectPrefab, position, Quaternion.identity);
                return;
            }

            Instantiate(effectPrefab, position, Quaternion.identity);
        }

        private void ApplyRuleSettings()
        {
            var room = ResolveCurrentMatchRoom();
            if (room?.Rule is CTFMatchRule rule && CTFScoreUIManager.Instance != null)
            {
                CTFScoreUIManager.Instance.SetCaptureLimit(rule.FlagCaptureCount);
                CTFScoreUIManager.Instance.SetMatchDuration(rule.TimeLimitSeconds);
            }

            room?.ResetCaptureTheFlagState();
        }

        private void CreateNewMyPlayer()
        {
            var spawnTeam = ResolveLocalTeam();
            var spawnSource = spawnTeam == ETeam.Red ? RedTeamReSpawnPoints : BlueTeamReSpawnPoints;
            var spawnPos = ResolveSpawnPoint(spawnTeam);
            var prefab = GetCharacterPrefabForLocalPlayer();

            if (prefab == null)
            {
                Debug.LogWarning("[CTF] Local player prefab could not be resolved.");
                return;
            }

            var player = Instantiate(prefab, spawnPos, Quaternion.identity);
            player.name = "MyPlayer";

            var iPlayer = player.GetComponent<AbstractPlayer>();
            if (iPlayer != null)
            {
                iPlayer.SetPlayerType(EPlayerType.MyPlayer);
                iPlayer.SetTeam(spawnTeam);
                AttachPlayerLink(player, ResolveLocalPlayerId());
                iPlayer.OnSpawn();
            }

            playerCamera.Follow = player.transform;
            vcamera.Priority = 0;
            playerCamera.Priority = 10;

            this.player = player;
        }

        private void CreateOtherPlayers()
        {
            var room = matchRoomManager != null ? matchRoomManager.WaitRoom : null;
            if (room == null)
            {
                Debug.Log("[CTF] No wait room found. Skipping other player spawn.");
                return;
            }

            var players = room.AllPlayers();
            if (players == null || players.Count == 0)
            {
                Debug.Log("[CTF] Wait room has no players to spawn.");
                return;
            }

            var localId = ResolveLocalPlayerId();
            var localTeam = ResolveLocalTeam();
            var spawned = 0;

            foreach (var info in players)
            {
                if (info == null || string.IsNullOrWhiteSpace(info.Id))
                {
                    continue;
                }

                if (string.Equals(info.Id, localId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var team = ETeam.Blue;
                if (Enum.TryParse(info.Team.ToString(), out ETeam parsedTeam) && parsedTeam != ETeam.NoTeam)
                {
                    team = parsedTeam;
                }
                else if (info.Id == localId)
                {
                    team = localTeam;
                }
                var spawnSource = team == ETeam.Red ? RedTeamReSpawnPoints : BlueTeamReSpawnPoints;
                var spawnPos = ResolveSpawnPoint(team);
                var prefab = prefabMasterData != null ? prefabMasterData.SearchPlayerPrefab(info.playerCharacter) : null;

                if (prefab == null)
                {
                    prefab = GetCharacterPrefabForLocalPlayer();
                }

                if (prefab == null)
                {
                    continue;
                }

                var playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
                playerObj.name = $"OtherPlayer_{info.Name}";

                var playerComponent = playerObj.GetComponent<AbstractPlayer>();
                if (playerComponent != null)
                {
                    playerComponent.SetPlayerType(EPlayerType.OtherPlayer);
                    playerComponent.SetTeam(team);
                    AttachPlayerLink(playerObj, info.Id);
                    playerComponent.OnSpawn();
                }

                spawned++;
            }

            Debug.Log($"[CTF] Spawned {spawned} other players.");
        }

        private GameObject GetCharacterPrefabForLocalPlayer()
        {
            var localId = ResolveLocalPlayerId();
            var room = matchRoomManager != null ? matchRoomManager.WaitRoom : null;
            if (room != null)
            {
                var me = room.AllPlayers()?.Find(p => p != null && string.Equals(p.Id, localId, StringComparison.OrdinalIgnoreCase));
                if (me != null && prefabMasterData != null)
                {
                    var prefab = prefabMasterData.SearchPlayerPrefab(me.playerCharacter);
                    if (prefab != null)
                    {
                        return prefab;
                    }
                }
            }

            return prefabMasterData != null ? prefabMasterData.SearchPlayerPrefab(OpenGSCore.EPlayerCharacter.Misty) : null;
        }

        private ETeam ResolveLocalTeam()
        {
            var room = matchRoomManager != null ? matchRoomManager.WaitRoom : null;
            var localId = ResolveLocalPlayerId();
            if (room == null || string.IsNullOrWhiteSpace(localId))
            {
                return ETeam.Blue;
            }

            var local = room.AllPlayers()?.Find(p => p != null && string.Equals(p.Id, localId, StringComparison.OrdinalIgnoreCase));
            if (local == null)
            {
                return ETeam.Blue;
            }

            return Enum.TryParse(local.Team.ToString(), out ETeam parsedTeam) && parsedTeam != ETeam.NoTeam
                ? parsedTeam
                : ETeam.Blue;
        }

        private Vector3 ResolveSpawnPoint(ETeam team)
        {
            var spawnPoints = team == ETeam.Red ? RedTeamReSpawnPoints : BlueTeamReSpawnPoints;
            if (spawnPoints == null)
            {
                return Vector3.zero;
            }

            if (spawnPoints.transform.childCount > 0)
            {
                return spawnPoints.transform.GetChild(0).position;
            }

            return spawnPoints.transform.position;
        }

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? string.Empty : profile.GlobalUserId;
        }

        private static void AttachPlayerLink(GameObject playerObj, string playerId)
        {
            if (playerObj == null)
            {
                return;
            }

            var linker = playerObj.GetComponent<PlayerDataLinker>();
            if (linker == null)
            {
                linker = playerObj.AddComponent<PlayerDataLinker>();
            }

            linker.SetPlayerId(playerId ?? string.Empty);
        }

        void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void OnDestroy()
        {
            UnbindFlagStand(redTeamFlagStand);
            UnbindFlagStand(blueTeamFlagStand);
            UnSubscribeEvent();
            if (Instance == this) Instance = null;
        }

        // Update is called once per frame
        void Update()
        {
            if (GameManager != null && GameManager.IsOnlineGameMode && networkManager == null)
            {
                try
                {
                    networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                }
                catch
                {
                    networkManager = null;
                }
            }
        }

        void FlagCaptured(in TeamEventPlayerInfo capturedPlayerInfo)
        {
            if (TryResolveTeam(capturedPlayerInfo, out var team))
            {
                PlayerFlagCaptured(team);
            }
        }

        void FlagReturn(in TeamEventPlayerInfo flagReturnInfo)
        {
            if (TryResolveTeam(flagReturnInfo, out var team))
            {
                PlayerFlagReturned(team);
            }
        }

        void FlagLost(in TeamEventPlayerInfo team)
        {
            if (TryResolveTeam(team, out var resolvedTeam))
            {
                PlayerFlagLost(resolvedTeam);
            }
        }

        void FlagBurst(ETeam team)
        {
            var stand = GetFlagStand(team);
            var position = stand != null ? stand.transform.position : Vector3.zero;
            var effect = effectPrefabMasterData != null
                ? (effectPrefabMasterData.flagBurstEffect != null
                    ? effectPrefabMasterData.flagBurstEffect
                    : (effectPrefabMasterData.flagReturnEffect != null
                        ? effectPrefabMasterData.flagReturnEffect
                        : effectPrefabMasterData.HitEffect))
                : null;

            PlayFlagEffect(effect, position);
            Debug.Log($"[CTF] FlagBurst: {team}");
        }

        void RecoveryRedFlag()
        {
            redTeamFlagStand?.SetFlag();
        }

        void RecoveryBlueFlag()
        {
            blueTeamFlagStand?.SetFlag();
        }

        void GoToResultScene()
        {
            GoToResult();
        }

        private void OfflineEventParser(AbstractGameEvent e)
        {
            if (e.GetType() == typeof(FlagReturnSuccessEvent))
            {
                var ev = e as FlagReturnSuccessEvent;
                //matchRoom.Data;
            }
        }

        private void OnlineEventParser(AbstractMatchEvent e)
        {
            var eventName = e.EventName;

            if (RUDPMessageTypes.FlagReturn == eventName)
            {
            }

            if (RUDPMessageTypes.FlagLost == eventName)
            {
                //PlaySound.PlayBGM()
            }
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            // オフライン/オンライン両方のイベントを処理
            OfflineEventParser(e);

            // オンラインの場合、サーバーに送信
            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                SendFlagEventToServer(e);
            }
        }

        /// <summary>
        /// フラッグイベントをサーバーに送信
        /// </summary>
        private void SendFlagEventToServer(AbstractGameEvent e)
        {
            if (networkManager == null || !networkManager.IsConnected()) return;

            if (e is FlagEvent flagEvent)
            {
                var room = ResolveCurrentMatchRoom();
                var teamStr = flagEvent.Team().ToString();
                var playerId = flagEvent.PlayerID();
                var pos = flagEvent.Position();
                var eventKey = CreateFlagEventKey(flagEvent.FlagEventType().ToString(), teamStr, playerId, pos.x, pos.y);

                JObject json = flagEvent.FlagEventType() switch
                {
                    EFlagEventType.Captured => RUDPMessageBuilder.CreateFlagCaptured(playerId, teamStr, pos, eventKey),
                    EFlagEventType.Lost => RUDPMessageBuilder.CreateFlagLost(playerId, teamStr, pos, eventKey),
                    EFlagEventType.Returned => RUDPMessageBuilder.CreateFlagReturn(teamStr, playerId, eventKey),
                    EFlagEventType.Burst => RUDPMessageBuilder.CreateFlagBurst(teamStr, pos, playerId, eventKey),
                    EFlagEventType.Pickup => RUDPMessageBuilder.CreateFlagPickup(playerId, teamStr, pos, eventKey),
                    _ => null
                };

                if (json != null)
                {
                    AttachRoomIdentifiers(json, room);
                    networkManager.SendToServer(json);
                    Debug.Log($"[CTF] Sent flag event to server: {flagEvent.FlagEventType()}");
                }
            }
        }

        /// <summary>
        /// サーバーからのネットワークデータ受信処理
        /// </summary>
        protected override void OnNetworkDataRecved(JObject obj)
        {
            var messageType = MessageType.Normalize(obj["MessageType"]?.ToString());

            switch (messageType)
            {
                case RUDPMessageTypes.FlagCaptured:
                    HandleFlagEvent(obj, EFlagEventType.Captured);
                    break;
                case RUDPMessageTypes.FlagLost:
                    HandleFlagEvent(obj, EFlagEventType.Lost);
                    break;
                case RUDPMessageTypes.FlagReturn:
                    HandleFlagEvent(obj, EFlagEventType.Returned);
                    break;
                case RUDPMessageTypes.FlagBurst:
                    HandleFlagEvent(obj, EFlagEventType.Burst);
                    break;
                case RUDPMessageTypes.FlagPickup:
                    HandleFlagEvent(obj, EFlagEventType.Pickup);
                    break;
                case RUDPMessageTypes.FlagScoreUpdate:
                    HandleFlagScoreUpdate(obj);
                    break;
                case MessageType.MatchEndNotification:
                    HandleMatchEnd(obj);
                    break;
                default:
                    base.OnNetworkDataRecved(obj);
                    break;
            }
        }

        /// <summary>
        /// サーバーからのフラッグイベントを処理
        /// </summary>
        private void HandleFlagEvent(JObject json, EFlagEventType eventType)
        {
            var playerId = json["PlayerId"]?.ToString() ?? json["PlayerID"]?.ToString() ?? "";
            var teamStr = json["Team"]?.ToString() ?? "Red";
            Enum.TryParse<ETeam>(teamStr, out var team);
            var eventKey = ResolveFlagEventKey(json, eventType, team, playerId);

            if (!TryRememberFlagEvent(eventKey))
            {
                Debug.Log($"[CTF] Ignored duplicate flag event: {eventType} key={eventKey}");
                return;
            }

            // UIイベントを発火
            switch (eventType)
            {
                case EFlagEventType.Captured:
                    PlayerFlagCaptured(team, true);
                    break;
                case EFlagEventType.Lost:
                    PlayerFlagLost(team, true);
                    break;
                case EFlagEventType.Returned:
                    PlayerFlagReturned(team, true);
                    break;
                case EFlagEventType.Pickup:
                    PlayerFlagPickedUp(team, playerId, true);
                    break;
                case EFlagEventType.Burst:
                    FlagBurst(team);
                    break;
            }

            Debug.Log($"[CTF] Received flag event from server: {eventType} for team {team}, key={eventKey}");
        }

        /// <summary>
        /// フラッグスコア更新を処理
        /// </summary>
        private void HandleFlagScoreUpdate(JObject json)
        {
            var eventKey = json["EventKey"]?.ToString();
            if (!TryRememberFlagEvent(string.IsNullOrWhiteSpace(eventKey)
                ? $"FlagScoreUpdate|{ReadScore(json, "RedTeamScore", "RedTeamFlagScore")}|{ReadScore(json, "BlueTeamScore", "BlueTeamFlagScore")}"
                : $"FlagScoreUpdate|{eventKey}"))
            {
                Debug.Log($"[CTF] Ignored duplicate flag score update: {eventKey}");
                return;
            }

            var redScore = ReadScore(json, "RedTeamScore", "RedTeamFlagScore");
            var blueScore = ReadScore(json, "BlueTeamScore", "BlueTeamFlagScore");

            var room = ResolveCurrentMatchRoom();
            if (room?.MatchData != null)
            {
                var redDelta = redScore - room.MatchData.RedTeamFlagScore;
                var blueDelta = blueScore - room.MatchData.BlueTeamFlagScore;
                if (redDelta != 0) room.MatchData.AddFlagScore(ETeam.Red, redDelta);
                if (blueDelta != 0) room.MatchData.AddFlagScore(ETeam.Blue, blueDelta);
            }

            // CTFScoreUIManagerにスコアを通知
            if (CTFScoreUIManager.Instance != null)
            {
                CTFScoreUIManager.Instance.UpdateScore(redScore, blueScore);
            }

            if (!endFlag && room?.Rule is CTFMatchRule rule && room.MatchData != null && rule.D(room.MatchData))
            {
                endFlag = true;
                HandleMatchEndFromScores(redScore, blueScore);
            }

            Debug.Log($"[CTF] Score update: Red={redScore}, Blue={blueScore}");
        }

        public List<IFlagStand> AllFlagStands()
        {
            var result = new List<IFlagStand>();

            if (redTeamFlagStand != null)
            {
                result.Add(redTeamFlagStand);
            }

            if (blueTeamFlagStand != null)
            {
                result.Add(blueTeamFlagStand);
            }

            return result;
        }

        [Button("フラッグキャプチャーテスト")]
        public void PlayerFlagCaptured(ETeam team)
        {
            PlayerFlagCaptured(team, false);
        }

        [Button("フラッグキャプチャーテスト")]
        public void PlayerFlagCaptured(ETeam team, bool fromNetwork = false)
        {
            Debug.Log("FlagCaptured: " + team);
            OnFlagCaptured?.Invoke(team);
            if (!fromNetwork)
            {
                RegisterFlagCapture(team);
            }
        }

        [Button("フラッグロストテスト")]
        public void PlayerFlagLost(ETeam team)
        {
            PlayerFlagLost(team, false);
        }

        [Button("フラッグロストテスト")]
        public void PlayerFlagLost(ETeam team, bool fromNetwork = false)
        {
            Debug.Log("FlagLost: " + team);
            OnFlagLost?.Invoke(team);
        }

        [Button("フラッグ帰還テスト")]
        public void PlayerFlagReturned(ETeam team, bool fromNetwork = false)
        {
            Debug.Log("FlagReturned: " + team);
            OnFlagReturned?.Invoke(team);
        }

        [Button("フラッグピックテスト")]
        public void PlayerFlagPickedUp(ETeam team, string playerName, bool fromNetwork = false)
        {
            Debug.Log("FlagPickedUp: " + team + " by " + playerName);
            OnFlagPickedUp?.Invoke(team, playerName);
        }

        private void RegisterFlagCapture(ETeam scoringTeam)
        {
            var room = ResolveCurrentMatchRoom();
            if (room?.MatchData == null)
            {
                PushFlagScoreUpdate(scoringTeam == ETeam.Red ? 1 : 0, scoringTeam == ETeam.Blue ? 1 : 0);
                return;
            }

            var scores = room.AddFlagScore(scoringTeam, 1);
            PushFlagScoreUpdate(scores.RedScore, scores.BlueScore);

            if (!endFlag && room.Rule is CTFMatchRule rule && rule.D(room.MatchData))
            {
                endFlag = true;
                HandleMatchEndFromScores(scores.RedScore, scores.BlueScore);
            }
        }

        private void PushFlagScoreUpdate(int redScore, int blueScore)
        {
            var room = ResolveCurrentMatchRoom();
            var scoreUpdate = RUDPMessageBuilder.CreateFlagScoreUpdate(redScore, blueScore, 0, 0, CreateFlagEventKey("score", string.Empty, string.Empty));
            AttachRoomIdentifiers(scoreUpdate, room);

            if (networkManager != null && networkManager.IsConnected())
            {
                if (CTFScoreUIManager.Instance != null)
                {
                    CTFScoreUIManager.Instance.UpdateScoreFromServer(redScore, blueScore);
                }

                networkManager.SendToServer(scoreUpdate);
                return;
            }

            if (CTFScoreUIManager.Instance != null)
            {
                CTFScoreUIManager.Instance.UpdateScoreFromServer(redScore, blueScore);
            }
        }

        private void HandleMatchEndFromScores(int redScore, int blueScore)
        {
            var winningTeam = redScore == blueScore
                ? "Draw"
                : (redScore > blueScore ? ETeam.Red.ToString() : ETeam.Blue.ToString());

            var myTeam = ResolveLocalTeamName();
            HandleMatchEnd(new JObject
            {
                ["WinningTeam"] = winningTeam,
                ["MyTeam"] = myTeam
            });
        }

        private MatchRoom ResolveCurrentMatchRoom()
        {
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

        private static string ResolveFlagEventKey(JObject json, EFlagEventType eventType, ETeam team, string playerId)
        {
            var jsonKey = json?["EventKey"]?.ToString();
            if (!string.IsNullOrWhiteSpace(jsonKey))
            {
                return jsonKey;
            }

            var posX = json?["PosX"]?.ToObject<float>() ?? 0f;
            var posY = json?["PosY"]?.ToObject<float>() ?? 0f;
            return $"{eventType}|{team}|{playerId}|{posX:0.###}|{posY:0.###}";
        }

        private string CreateFlagEventKey(string eventType, string team, string playerId, float posX = 0f, float posY = 0f)
        {
            return $"{eventType}|{team}|{playerId}|{posX:0.###}|{posY:0.###}|{Guid.NewGuid():N}";
        }

        private static int ReadScore(JObject json, params string[] keys)
        {
            foreach (var key in keys)
            {
                var token = json?[key];
                if (token != null && int.TryParse(token.ToString(), out var value))
                {
                    return value;
                }
            }

            return 0;
        }

        private static void AttachRoomIdentifiers(JObject json, MatchRoom room)
        {
            if (json == null || room == null)
            {
                return;
            }

            json["RoomID"] = room.Id;
            json["RoomId"] = room.Id;
        }

        private bool TryRememberFlagEvent(string eventKey)
        {
            if (string.IsNullOrWhiteSpace(eventKey))
            {
                return true;
            }

            lock (processedFlagEventKeys)
            {
                if (!processedFlagEventKeys.Add(eventKey))
                {
                    return false;
                }

                recentFlagEventKeys.Enqueue(eventKey);
                while (recentFlagEventKeys.Count > MaxRecentFlagEventKeys)
                {
                    var oldest = recentFlagEventKeys.Dequeue();
                    processedFlagEventKeys.Remove(oldest);
                }
            }

            return true;
        }

        private void HandleMatchEnd(JObject json)
        {
            var winningTeam = json["WinningTeam"]?.ToString() ?? "Draw";
            var myTeam = json["MyTeam"]?.ToString() ?? "Spectator";

            Debug.Log($"[CTF] Match ended: winner={winningTeam}, myTeam={myTeam}");
            if (CTFScoreUIManager.Instance != null && Enum.TryParse(winningTeam, out ETeam winning))
            {
                var room = ResolveCurrentMatchRoom();
                var redScore = room?.MatchData?.RedTeamFlagScore ?? 0;
                var blueScore = room?.MatchData?.BlueTeamFlagScore ?? 0;
                CTFScoreUIManager.Instance.ShowVictory(winning, redScore, blueScore);
            }
            if (IsOfflineMatch())
            {
                StoreOfflineMatchResult(winningTeam, myTeam);
            }
            CancelInvoke(nameof(GoToResultScene));
            Invoke(nameof(GoToResultScene), gotoResultSceneWaitTime);
        }

        private void StoreOfflineMatchResult(string winningTeam, string myTeam)
        {
            var players = ResolveLocalPlayers();
            var room = ResolveCurrentMatchRoom();
            var redScore = room?.MatchData?.RedTeamFlagScore ?? 0;
            var blueScore = room?.MatchData?.BlueTeamFlagScore ?? 0;
            var result = new JObject
            {
                ["MessageType"] = MessageType.MatchEndNotification,
                ["WinningTeam"] = winningTeam,
                ["MyTeam"] = string.IsNullOrWhiteSpace(myTeam) ? ResolveLocalTeamName() : myTeam,
                ["RedTeamScore"] = redScore,
                ["BlueTeamScore"] = blueScore,
                ["RedTeamFlagScore"] = redScore,
                ["BlueTeamFlagScore"] = blueScore,
                ["RedTeamKills"] = 0,
                ["BlueTeamKills"] = 0,
                ["Players"] = new JArray(players.ConvertAll(p => p?.ToJson()))
            };

            matchRoomManager?.StoreOfflineMatchResult(result);
        }
    }
}
