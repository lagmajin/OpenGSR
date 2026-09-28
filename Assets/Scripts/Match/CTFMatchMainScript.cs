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
        private bool respawnPending;

        public new void Start()
        {
            base.Start();
            // Singleton設定
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CTF] Duplicate main script found; destroying duplicate.");
                Destroy(gameObject);
                return;
            }
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

            // The server holds where each flag is, so it has to be told. This was
            // raised as a local event and sent nowhere, so the server never knew a
            // flag had been picked up and every capture it was asked to judge was
            // refused for want of a carrier.
            ReportFlagTransitionToServer(MessageType.FlagPickup, flagController.team, player);
        }

        private void HandleFlagReturnedToBase(FlagController flagController, AbstractPlayer player, EFlagReturnReason reason)
        {
            if (flagController == null)
            {
                return;
            }

            if (reason == EFlagReturnReason.CapturedAtBase)
            {
                // A delivery is not a return. The server decides when a flag is
                // captured and says so; hearing it back as a return would undo the
                // score it just gave.
                return;
            }

            PlayFlagEffect(effectPrefabMasterData != null ? effectPrefabMasterData.flagReturnEffect : null, flagController.transform.position);
            PlayerFlagReturned(flagController.team, false);

            ReportFlagTransitionToServer(MessageType.FlagReturn, flagController.team, player, reason.ToString());
        }

        private void HandleFlagDropped(FlagController flagController)
        {
            if (flagController == null)
            {
                return;
            }

            PlayFlagEffect(effectPrefabMasterData != null ? effectPrefabMasterData.HitEffect : null, flagController.transform.position);
            PlayerFlagLost(flagController.team, false);

            ReportFlagTransitionToServer(MessageType.FlagLost, flagController.team, LocalFlagCarrier());
        }

        private void HandleFlagCapturedFromStand(FlagStand stand, AbstractPlayer player)
        {
            if (stand == null)
            {
                return;
            }

            PlayFlagEffect(effectPrefabMasterData != null ? effectPrefabMasterData.flagReturnEffect : null, stand.transform.position);

            if (player != null)
            {
                player.EnemyFlagReturnedToBase(true);
            }

            // The flag that reached a stand belonged to the team that stand is
            // not, so the stand names the flag and the claim is about carrying it
            // home. The server judges it against the flags it holds.
            ReportFlagTransitionToServer(MessageType.FlagCaptured, stand.Team, player);
        }

        private AbstractPlayer LocalFlagCarrier()
        {
            return player != null ? player.GetComponent<AbstractPlayer>() : null;
        }

        /// <summary>
        /// Where a team's flag is lying in the world, if it is lying anywhere.
        /// <para>
        /// A drop has to carry its position so the server can refuse a pickup from
        /// somebody standing on the other side of the map. Without it the server
        /// knows the flag is loose and not where, so the reach check it now
        /// performs has nothing to measure against.
        /// </para>
        /// </summary>
        private bool TryGetFlagWorldPosition(ETeam flagTeam, out Vector2 position)
        {
            position = Vector2.zero;

            foreach (var flag in FindObjectsByType<FlagController>(FindObjectsSortMode.None))
            {
                if (flag != null && flag.team == flagTeam)
                {
                    position = (Vector2)flag.transform.position;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tells the server a flag moved, naming which flag rather than which
        /// side the carrier is on.
        /// <para>
        /// A flag belongs to a team, and a red player picks up the blue flag, so
        /// the carrier's own team is not the fact the server needs. Without the
        /// flag's team the server has to guess which flag somebody is holding,
        /// and guessing that is how a team ends up recorded as carrying its own.
        /// </para>
        /// </summary>
        private void ReportFlagTransitionToServer(
            string messageType,
            ETeam flagTeam,
            AbstractPlayer carrier,
            string returnReason = null)
        {
            if (networkManager == null || !networkManager.IsConnected())
            {
                return;
            }

            var carrierId = carrier != null ? carrier.UniqueID().ToString() : string.Empty;
            var isReturnByItself = string.Equals(returnReason, nameof(EFlagReturnReason.AutoReturn), StringComparison.Ordinal);
            if (string.IsNullOrWhiteSpace(carrierId) && !isReturnByItself)
            {
                // A claim is a statement about a player, so without one there is
                // nothing being claimed. The one exception is a flag that went
                // home by itself, which belongs to nobody.
                Debug.LogWarning($"[CTF] {messageType} was not reported: the flag's carrier is not a local player.");
                return;
            }

            var position = carrier != null ? (Vector2)carrier.transform.position : Vector2.zero;
            var eventKey = CreateFlagEventKey(messageType, flagTeam.ToString(), carrierId, position.x, position.y);

            JObject json = messageType switch
            {
                MessageType.FlagPickup => RUDPMessageBuilder.CreateFlagPickup(
                    carrierId, flagTeam.ToString(), position, eventKey),
                MessageType.FlagLost => RUDPMessageBuilder.CreateFlagLost(
                    carrierId, flagTeam.ToString(), position, eventKey),
                MessageType.FlagReturn => RUDPMessageBuilder.CreateFlagReturn(
                    flagTeam.ToString(), carrierId, eventKey),
                MessageType.FlagCaptured => RUDPMessageBuilder.CreateFlagCaptured(
                    carrierId, flagTeam.ToString(), position, eventKey),
                _ => null
            };

            if (json == null)
            {
                return;
            }

            // The flag's own team, which the builders do not carry because they
            // were written before the server needed it.
            json["FlagTeam"] = flagTeam.ToString();
            if (!string.IsNullOrWhiteSpace(returnReason))
            {
                json["ReturnReason"] = returnReason;
            }

            // A drop carries where the flag landed, and a pickup has to be refused
            // unless the player is actually next to it. Without the position the
            // server knows the flag is loose and not where, so the reach check
            // has nothing to measure against and a claim is believed on its own.
            if (messageType == MessageType.FlagLost && TryGetFlagWorldPosition(flagTeam, out var where))
            {
                json["PosX"] = where.x;
                json["PosY"] = where.y;
            }

            AttachRoomIdentifiers(json, ResolveCurrentMatchRoom());
            networkManager.SendToServer(json);
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

            var spawnedEffect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(spawnedEffect, 5f);
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

        public override void OnMyPlayerDead()
        {
            if (endFlag || respawnPending)
            {
                return;
            }

            respawnPending = true;
            var delay = ResolveRespawnDelaySeconds();
            CancelInvoke(nameof(HandleMyPlayerRespawn));
            Invoke(nameof(HandleMyPlayerRespawn), delay);

            if (battleSceneMediateObject?.uiManager != null)
            {
                battleSceneMediateObject.uiManager.ShowRespawnGauge(delay);
            }

            Debug.Log($"[CTF] Local player will respawn in {delay:0.##} seconds.");
        }

        private void HandleMyPlayerRespawn()
        {
            respawnPending = false;
            if (endFlag)
            {
                return;
            }

            var oldPlayerId = Guid.Empty;
            if (player != null)
            {
                oldPlayerId = player.GetComponent<AbstractPlayer>()?.UniqueID() ?? Guid.Empty;
                Destroy(player);
                player = null;
            }

            var team = ResolveLocalTeam();
            var spawnPos = ResolveSpawnPoint(team);
            CreateNewMyPlayer();

            if (player != null)
            {
                var playerComponent = player.GetComponent<AbstractPlayer>();
                if (playerComponent != null && oldPlayerId != Guid.Empty)
                {
                    playerComponent.SetUniqueID(oldPlayerId);
                }

                AttachPlayerLink(player, ResolveLocalPlayerId());
            }

            ReportRespawnToServer();

            Debug.Log($"[CTF] My player respawned for {team} at {spawnPos}.");
        }

        /// <summary>
        /// Tells the server this player is back.
        /// <para>
        /// A respawn used to be entirely local: the client rebuilt its player and
        /// the server kept the health it had already been reduced to, which is
        /// zero. The hit path refuses a target whose health is not above zero, so
        /// from then on nobody could shoot this player at all and they were
        /// untouchable for the rest of the match while walking around the map.
        /// </para>
        /// <para>
        /// The position is not sent. A respawn position is the server's to decide,
        /// and it is the one thing about a respawn the client must not be believed
        /// on: a client that chose where it comes back could come back anywhere.
        /// </para>
        /// </summary>
        private void ReportRespawnToServer()
        {
            if (!IsOnlineMatch() || networkManager == null || !networkManager.IsConnected())
            {
                return;
            }

            var message = RUDPMessageBuilder.CreatePlayerRespawn(ResolveLocalPlayerId(), Vector2.zero);
            AttachRoomIdentifiers(message, ResolveCurrentMatchRoom());
            networkManager.SendToServer(message);
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

        protected override void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            UnbindFlagStand(redTeamFlagStand);
            UnbindFlagStand(blueTeamFlagStand);
            UnSubscribeEvent();
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override float ResolveMatchDuration()
        {
            return 600f;
        }

        protected override void OnTimeUp()
        {
            Debug.Log("[CTF] Time up!");

            if (IsOnlineMatch())
            {
                // The clock the rule runs on is the server's, and it decides when
                // a match is over. A client that ended it on its own timer would
                // leave a match the server was still running, and would work out
                // the winner from a score it had been keeping rather than from
                // the one the rule decided. The server's end notification carries
                // the result, so waiting for it is also how this client learns
                // who won.
                Debug.Log("[CTF] Waiting for the server to end the match.");
                return;
            }

            var room = ResolveCurrentMatchRoom();
            var redScore = room?.MatchData?.RedTeamFlagScore ?? 0;
            var blueScore = room?.MatchData?.BlueTeamFlagScore ?? 0;
            HandleMatchEndFromScores(redScore, blueScore);
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
            if (e is FlagReturnSuccessEvent)
            {
                // A successful return is a state/UI event even in offline play.
                // The concrete team is applied by the flag controller path; keep
                // the event visible here for match-level observers.
                Debug.Log("[CTF] Flag return success event processed.");
            }
        }

        private void OnlineEventParser(AbstractMatchEvent e)
        {
            if (e == null)
            {
                return;
            }

            var eventName = e.EventName;

            if (RUDPMessageTypes.FlagReturn == eventName)
            {
                Debug.Log("[CTF] Flag return event processed online.");
            }

            if (RUDPMessageTypes.FlagLost == eventName)
            {
                Debug.Log("[CTF] Flag lost event processed online.");
            }
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            // オフライン/オンライン両方のイベントを処理
            OfflineEventParser(e);
            OnlineEventParser(e as AbstractMatchEvent);

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
                    EFlagEventType.Pickup => RUDPMessageBuilder.CreateFlagPickup(playerId, teamStr, pos, eventKey),

                    // A burst is not claimed. A flag going is the rule's outcome
                    // and the server says whose flag it was, so a client that
                    // could assert it could destroy a flag sitting safely on its
                    // own stand. Sending it used to be dropped on arrival, and the
                    // flag the server destroyed was a flag nobody heard about.
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
                    // The server holds where each flag is, so its ruling is what
                    // the indicators show. The local flag state was a guess the
                    // client made about its own copy of a flag the server owns.
                    ApplyServerFlagState(obj, EFlagEventType.Pickup);
                    break;
                case RUDPMessageTypes.FlagCaptureRefused:
                    HandleFlagCaptureRefused(obj);
                    break;
                case RUDPMessageTypes.FlagScoreUpdate:
                    HandleFlagScoreUpdate(obj);
                    break;
                case MessageType.Snapshot:
                    HandleRoomState(obj);
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
        /// <summary>
        /// Applies what the server said a flag is doing, to the indicator that
        /// shows it.
        /// <para>
        /// The server holds the flags, so a ruling is the only thing that can say
        /// where one is. The client kept its own copy and the indicator showed
        /// that, which meant the two sides could disagree about whether a flag
        /// was safe with nothing to correct it.
        /// </para>
        /// </summary>
        private void ApplyServerFlagState(JObject json, EFlagEventType eventType)
        {
            var team = ReadTeam(json);
            if (team == ETeam.NoTeam)
            {
                return;
            }

            var state = eventType switch
            {
                EFlagEventType.Pickup => EFlagState.FlagCapturedPlayer,
                EFlagEventType.Lost => EFlagState.FlagOnGround,
                EFlagEventType.Returned => EFlagState.FlagOnStand,
                EFlagEventType.Captured => EFlagState.FlagOnGround,
                _ => EFlagState.FlagOnStand
            };

            CTFScoreUIManager.Instance?.UpdateFlagStateFromServer(team, state);
        }

        /// <summary>
        /// The server ruled that a delivery did not score.
        /// <para>
        /// A refusal used to be logged and dropped. The player had walked the flag
        /// to the enemy stand and got nothing back, which is indistinguishable
        /// from the message being lost, and the flag stayed in their hands
        /// looking like it had been delivered. The reason travels with the
        /// refusal because the rule has more than one reason: a team whose own
        /// flag is not home cannot score, and that is worth saying out loud
        /// because it is the whole point of the mode.
        /// </para>
        /// </summary>
        private void HandleFlagCaptureRefused(JObject json)
        {
            // The server answers a refusal to the player whose delivery it was, so
            // this is about the local player. Checking anyway means a refusal that
            // arrives for somebody else cannot put a notice on this screen that
            // has nothing to do with the player watching it.
            var subject = json["PlayerId"]?.ToString() ?? json["PlayerID"]?.ToString() ?? string.Empty;
            var localPlayerId = ResolveLocalPlayerId();
            if (!string.IsNullOrWhiteSpace(subject) && !IsLocalPlayerId(subject))
            {
                return;
            }

            var team = ReadTeam(json);
            var reason = json["Reason"]?.ToString() ?? string.Empty;

            Debug.LogWarning($"[CTF] The {team} delivery by {localPlayerId} did not score: {reason}");

            if (CTFScoreUIManager.Instance != null &&
                Enum.TryParse(reason, ignoreCase: true, out EFlagRefusal refusal))
            {
                CTFScoreUIManager.Instance.ShowCaptureRefused(team, refusal);
            }
        }

        /// <summary>
        /// Whether an id names the player this client is.
        /// </summary>
        private bool IsLocalPlayerId(string playerId)
        {
            var localPlayerId = ResolveLocalPlayerId();
            return string.Equals(playerId, localPlayerId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Reads the team a flag message is about, accepting both spellings the
        /// server and the older client used.
        /// </summary>
        private static ETeam ReadTeam(JObject json)
        {
            var named = json["FlagTeam"]?.ToString() ?? json["Team"]?.ToString() ?? string.Empty;
            return Enum.TryParse(named, ignoreCase: true, out ETeam team) ? team : ETeam.NoTeam;
        }

        /// <summary>
        /// Takes the match settings the server is running from, out of the room
        /// state it publishes.
        /// <para>
        /// The client carried its own flag limit and the server carried another,
        /// so the scoreboard could say "first to five" on a match that ended at
        /// three. A limit is a rule, and the rule is the server's: it ends the
        /// match. The client adopting the number is what makes the two agree about
        /// what they are playing, rather than the scoreboard being decoration
        /// that is sometimes right.
        /// </para>
        /// </summary>
        private void HandleRoomState(JObject json)
        {
            var limit = json["WinConditionPoint"]?.ToObject<int>() ?? 0;
            if (limit <= 0)
            {
                return;
            }

            var room = ResolveCurrentMatchRoom();
            if (room?.Rule is not CTFMatchRule rule || rule.FlagCaptureCount == limit)
            {
                return;
            }

            rule.FlagCaptureCount = limit;
            CTFScoreUIManager.Instance?.SetCaptureLimit(limit);
            Debug.Log($"[CTF] Took the flag limit from the server: {limit}");
        }

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

            // Ending the match is the server's call in an online match: it owns the
            // rule and the timer. A client that ended it from its own score would
            // leave the match before the server did, and the two would disagree
            // about whether the game was still running.
            if (!endFlag && room?.Rule is CTFMatchRule rule && room.MatchData != null && rule.D(room.MatchData))
            {
                if (IsOnlineMatch())
                {
                    Debug.Log("[CTF] Flag limit reached; waiting for the server to end the match.");
                }
                else
                {
                    endFlag = true;
                    HandleMatchEndFromScores(redScore, blueScore);
                }
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
            InvokeSafely(OnFlagCaptured, team, nameof(OnFlagCaptured));

            if (fromNetwork)
            {
                // The server already ruled and already told the room through the
                // score update that travels with it. Counting it again here is
                // how a client ends up showing a score the server never gave.
                return;
            }

            if (IsOnlineMatch())
            {
                // An online capture is the server's to judge. The claim has
                // already been sent by the flag reaching a stand, so the score
                // arrives as a ruling rather than being made here.
                RegisterFlagCapture(team);
                return;
            }

            // Offline there is no server to ask, so the local match is the whole
            // authority and has to keep its own score.
            var room = ResolveCurrentMatchRoom();
            if (room?.MatchData == null)
            {
                ShowScoreLocally(team == ETeam.Red ? 1 : 0, team == ETeam.Blue ? 1 : 0);
                return;
            }

            var scores = room.AddFlagScore(team, 1);
            ShowScoreLocally(scores.RedScore, scores.BlueScore);
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
            InvokeSafely(OnFlagLost, team, nameof(OnFlagLost));
        }

        [Button("フラッグ帰還テスト")]
        public void PlayerFlagReturned(ETeam team, bool fromNetwork = false)
        {
            Debug.Log("FlagReturned: " + team);
            InvokeSafely(OnFlagReturned, team, nameof(OnFlagReturned));
        }

        [Button("フラッグピックテスト")]
        public void PlayerFlagPickedUp(ETeam team, string playerName, bool fromNetwork = false)
        {
            Debug.Log("FlagPickedUp: " + team + " by " + playerName);
            InvokeSafely(OnFlagPickedUp, team, playerName, nameof(OnFlagPickedUp));
        }

        private static void InvokeSafely(Action<ETeam> handlers, ETeam team, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<ETeam> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(team);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CTFMatchMainScript] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<ETeam, string> handlers, ETeam team, string playerName, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<ETeam, string> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(team, playerName);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CTFMatchMainScript] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private void RegisterFlagCapture(ETeam scoringTeam)
        {
            // The server scores. The client used to count the capture itself and
            // push the number, so the two sides each kept their own score: the
            // server recomputed from state it had never been told about and told
            // the room zero, while this client showed itself a point. Nothing is
            // added here; the room applies the score the server sends.
            Debug.Log($"[CTF] Waiting for the server to rule on a {scoringTeam} capture.");
        }

        /// <summary>
        /// Shows a score on this client without telling the server about it.
        /// <para>
        /// A score is the server's to keep, so nothing here sends one. This only
        /// exists for offline play, where there is no server to ask and the local
        /// match is the whole authority.
        /// </para>
        /// </summary>
        private void ShowScoreLocally(int redScore, int blueScore)
        {
            if (CTFScoreUIManager.Instance != null)
            {
                CTFScoreUIManager.Instance.UpdateScoreFromServer(redScore, blueScore);
            }
        }

        /// <summary>
        /// Adopts the final score the server's rule produced, when the message
        /// carries one.
        /// <para>
        /// The score on this client is a copy the client has been keeping, and the
        /// server's is the one the match was decided on. A capture the client
        /// missed, or one it counted and the server did not, leaves the two
        /// disagreeing, and the result screen is the last place that shows. The
        /// deltas are applied rather than the totals set, because the live score
        /// path is the same and the two must not behave differently.
        /// </para>
        /// </summary>
        private void ApplyServerFinalScore(JObject json)
        {
            if (json["RedTeamScore"] == null && json["BlueTeamScore"] == null)
            {
                // An offline result carries no score fields, and there is nothing
                // to adopt over the score this client already has.
                return;
            }

            var redScore = ReadScore(json, "RedTeamScore", "RedTeamFlagScore");
            var blueScore = ReadScore(json, "BlueTeamScore", "BlueTeamFlagScore");
            var room = ResolveCurrentMatchRoom();
            if (room?.MatchData == null)
            {
                return;
            }

            var redDelta = redScore - room.MatchData.RedTeamFlagScore;
            var blueDelta = blueScore - room.MatchData.BlueTeamFlagScore;
            if (redDelta != 0) room.MatchData.AddFlagScore(ETeam.Red, redDelta);
            if (blueDelta != 0) room.MatchData.AddFlagScore(ETeam.Blue, blueDelta);

            Debug.Log($"[CTF] Adopted the server's final score: Red={redScore}, Blue={blueScore}");
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

            var posX = ReadFloat(json, "PosX");
            var posY = ReadFloat(json, "PosY");
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
                    return Math.Max(0, value);
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
            if (!TryBeginMatchEnd())
            {
                return;
            }

            // The server owns the result, so it says who won and what the score
            // was. The notification used to carry neither, which left this reading
            // a winner out of a message that had none and defaulting to a draw: an
            // online match finished as a draw whatever happened in it. The score
            // the server sends is applied before it is read back, so the numbers
            // on the victory screen are the ones the rule decided rather than this
            // client's copy of them.
            ApplyServerFinalScore(json);

            var winningTeam = json["WinningTeam"]?.ToString() ?? "Draw";
            var myTeam = json["MyTeam"]?.ToString() ?? "Spectator";

            Debug.Log($"[CTF] Match ended: winner={winningTeam}, myTeam={myTeam}");
            if (CTFScoreUIManager.Instance != null && Enum.TryParse(winningTeam, out ETeam winning))
            {
                var redScore = ReadScore(json, "RedTeamScore", "RedTeamFlagScore");
                var blueScore = ReadScore(json, "BlueTeamScore", "BlueTeamFlagScore");
                CTFScoreUIManager.Instance.ShowVictory(winning, redScore, blueScore);
            }
            if (IsOfflineMatch())
            {
                StoreOfflineMatchResult(winningTeam, myTeam);
            }
            ScheduleResultSceneTransition(gotoResultSceneWaitTime);
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

