using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using KanKikuchi.AudioManager;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;
using Zenject;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class TDMMatchMainScript : AbstractMatchMainScript, ITDMMatchMainScript
    {
        public static TDMMatchMainScript Instance { get; private set; }

        public event Action<ETeam> OnPlayerKilled;
        public event Action<ETeam, ETeam> OnTeamKill;
        public event Action<ETeam> OnMatchEnded;

        private MatchRUDPServerNetworkManager networkManager;

        [SerializeField] private TDMScoreUIManager scoreUIManager;

        [SerializeField] [Required] private TeamReSpawnPoints redTeamRespawnPoints;
        [SerializeField] [Required] private TeamReSpawnPoints blueTeamRespawnPoint;
        private int redTeamKills = 0;
        private int blueTeamKills = 0;
        private ETeam localTeam = ETeam.Blue; private const int OfflineKillLimit = 50;
        [SerializeField] private bool suddenDeathOnDraw = true;
        [SerializeField] private float suddenDeathDuration = 120f;
        private bool suddenDeathActive;

        private new void Start()
        {
            base.Start();
            Application.targetFrameRate = SettingsManager.Instance.GetGraphicsSettings().TargetFrameRate;
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[TDM] Duplicate main script found; destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            try
            {
                networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TDMMatchMainScript: Failed to resolve MatchRUDPServerNetworkManager: {ex.Message}");
                networkManager = null;
            }

            Debug.Log("TDM GameStart");
            Invoke("GameSetup", 0.1f);
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void GameSetup()
        {
            PlayGameStartVoice();
            localTeam = ResolveLocalTeam(matchRoomManager != null ? matchRoomManager.WaitRoom : null);
            CreateMyPlayerLocally();
            CreateOtherPlayers();

            if (scoreUIManager == null)
            {
                scoreUIManager = FindObjectOfType<TDMScoreUIManager>();
            }

            if (scoreUIManager != null)
            {
                scoreUIManager.StartMatch();
            }
            else
            {
                Debug.LogWarning("[TDM] TDMScoreUIManager is not present in the active scene. Team score HUD is disabled.", this);
            }
        }


        protected override void OnLocalTimeUp()
        {
            Debug.Log("[TDM] Time up!");
            if (redTeamKills == blueTeamKills && suddenDeathOnDraw)
            {
                StartSuddenDeath();
                return;
            }

            FinishByScore();
        }

        private void StartSuddenDeath()
        {
            suddenDeathActive = true;
            Debug.Log($"[TDM] Sudden death started ({suddenDeathDuration:0}s).");
            if (timer == null)
            {
                return;
            }

            timer.SetTime(Mathf.Max(1f, suddenDeathDuration));
            timer.timeupEvent.RemoveAllListeners();
            timer.timeupEvent.AddListener(OnSuddenDeathTimeUp);
            timer.StartTimer();
        }

        private void OnSuddenDeathTimeUp()
        {
            Debug.Log("[TDM] Sudden death time up; ending as draw.");
            FinishByScore();
        }

        private void FinishByScore()
        {
            var winningTeam = redTeamKills == blueTeamKills
                ? "Draw"
                : (redTeamKills > blueTeamKills ? "Red" : "Blue");
            EndMatchLocally(new Newtonsoft.Json.Linq.JObject
            {
                ["WinningTeam"] = winningTeam,
                ["MyTeam"] = ResolveLocalTeamName(),
                ["RedTeamKills"] = redTeamKills,
                ["BlueTeamKills"] = blueTeamKills
            });
        }

        private void CreateMyPlayerLocally()
        {
            var spawnSource = localTeam == ETeam.Blue ? blueTeamRespawnPoint : redTeamRespawnPoints;
            Vector3 spawnPos = GetRandomSpawnPoint(spawnSource, localTeam);
            var myPlayer = CreateMyPlayer(spawnPos, localTeam);
            AttachPlayerId(myPlayer, ResolveLocalPlayerId());
        }

        private void CreateOtherPlayers()
        {
            var room = matchRoomManager != null ? matchRoomManager.WaitRoom : null;
            if (room == null)
            {
                Debug.Log("[TDM] No wait room found. Skipping other player spawn.");
                return;
            }

            var players = room.AllPlayers();
            if (players == null || players.Count == 0)
            {
                Debug.Log("[TDM] Wait room has no players to spawn.");
                return;
            }

            var localPlayerId = ResolveLocalPlayerId();
            var spawnCount = 0;

            foreach (var info in players)
            {
                if (info == null || string.IsNullOrWhiteSpace(info.Id))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(localPlayerId) &&
                    string.Equals(info.Id, localPlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var team = Enum.TryParse<ETeam>(info.Team.ToString(), out var parsedTeam) && parsedTeam != ETeam.NoTeam
                    ? parsedTeam
                    : (info.Id == localPlayerId ? localTeam : ETeam.Blue);

                if (team == ETeam.NoTeam)
                {
                    team = ETeam.Blue;
                }

                var spawnSource = team == ETeam.Red ? redTeamRespawnPoints : blueTeamRespawnPoint;
                var spawnPos = GetRandomSpawnPoint(spawnSource, team);

                // Use the character selected by this room player.  The old code
                // always spawned Misty, which hid missing character-prefab
                // registrations and made the scene-placed Ami look necessary.
                var prefab = prefabMasterData != null
                    ? prefabMasterData.SearchPlayerPrefab(info.playerCharacter)
                    : null;

                if (prefab == null)
                {
                    continue;
                }

                var playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
                playerObj.name = $"OtherPlayer_{info.Name}";

                var player = playerObj.GetComponent<AbstractPlayer>();
                if (player != null)
                {
                    player.SetPlayerType(string.Equals(info.Id, localPlayerId, StringComparison.OrdinalIgnoreCase)
                        ? EPlayerType.MyPlayer
                        : EPlayerType.OtherPlayer);
                    player.SetTeam(team);
                    AttachPlayerId(playerObj, info.Id);
                    player.OnSpawn();
                }
                else if (playerObj.TryGetComponent<PlayerAgent>(out var playableAgent))
                {
                    // Playable character prefabs use PlayerAgent rather than
                    // the legacy AbstractPlayer hierarchy. Keep remote spawn
                    // identity and lifecycle wiring for that path as well.
                    playableAgent.SetPlayerType(EPlayerType.OtherPlayer);
                    AttachPlayerId(playerObj, info.Id);
                }

                spawnCount++;
            }

            Debug.Log($"[TDM] Spawned {spawnCount} other players.");
        }

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? string.Empty : profile.GlobalUserId;
        }

        private static void AttachPlayerId(GameObject playerObj, string playerId)
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
            playerObj.GetComponent<PlayerAgent>()?.SetPlayerID(playerId ?? string.Empty);
        }

        private static ETeam ResolveLocalTeam(OpenGSCore.WaitRoom room)
        {
            if (room == null)
            {
                return ETeam.Blue;
            }

            var localId = ResolveLocalPlayerId();
            var players = room.AllPlayers();
            if (players == null)
            {
                return ETeam.Blue;
            }

            foreach (var player in players)
            {
                if (player == null || string.IsNullOrWhiteSpace(player.Id))
                {
                    continue;
                }

                if (string.Equals(player.Id, localId, StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse<ETeam>(player.Team.ToString(), out var parsedTeam) &&
                    parsedTeam != ETeam.NoTeam)
                {
                    return parsedTeam;
                }
            }

            return ETeam.Blue;
        }

        private void Update()
        {
            if (endFlag) return;
            if (HandleEscapeToBackScene())
                return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                EndMatchLocally(new JObject
                {
                    ["WinningTeam"] = "Red",
                    ["MyTeam"] = ResolveLocalTeamName(),
                    ["RedTeamKills"] = redTeamKills,
                    ["BlueTeamKills"] = blueTeamKills
                });
            }

            // Offline kill limit check
            if (IsOfflineMatch() && (redTeamKills >= OfflineKillLimit || blueTeamKills >= OfflineKillLimit))
            {
                var winningTeam = redTeamKills == blueTeamKills
                    ? "Draw"
                    : (redTeamKills > blueTeamKills ? "Red" : "Blue");
                EndMatchLocally(new JObject { ["WinningTeam"] = winningTeam, ["MyTeam"] = ResolveLocalTeamName(), ["RedTeamKills"] = redTeamKills, ["BlueTeamKills"] = blueTeamKills });
                return;
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                EndMatchLocally(new JObject
                {
                    ["WinningTeam"] = "Blue",
                    ["MyTeam"] = ResolveLocalTeamName(),
                    ["RedTeamKills"] = redTeamKills,
                    ["BlueTeamKills"] = blueTeamKills
                });
            }
        }

        void GoToResultScene()
        {
            GoToResult();
        }

        public void OnPlayerDead(ETeam victimTeam, ETeam killerTeam)
        {
            if (killerTeam == ETeam.Red)
            {
                redTeamKills++;
                scoreUIManager?.AddRedKill();
            }
            else if (killerTeam == ETeam.Blue)
            {
                blueTeamKills++;
                scoreUIManager?.AddBlueKill();
            }

            if (suddenDeathActive && redTeamKills != blueTeamKills)
            {
                EndMatchLocally(new JObject
                {
                    ["WinningTeam"] = redTeamKills > blueTeamKills ? "Red" : "Blue",
                    ["MyTeam"] = ResolveLocalTeamName(),
                    ["RedTeamKills"] = redTeamKills,
                    ["BlueTeamKills"] = blueTeamKills
                });
            }

            InvokeSafely(OnPlayerKilled, victimTeam, nameof(OnPlayerKilled));
            InvokeSafely(OnTeamKill, killerTeam, victimTeam, nameof(OnTeamKill));

            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                SendKillEventToServer(killerTeam, victimTeam);
            }
        }

        public override void OnMyPlayerDead()
        {
            if (!MatchModeResolver.CanRespawnCurrentMatch())
            {
                return;
            }

            var delay = ResolveRespawnDelaySeconds();
            Invoke(nameof(HandleMyPlayerRespawn), delay);

            if (battleSceneMediateObject != null && battleSceneMediateObject.uiManager != null)
            {
                battleSceneMediateObject.uiManager.ShowRespawnGauge(delay);
            }
        }

        private void HandleMyPlayerRespawn()
        {
            if (endFlag)
            {
                return;
            }

            Guid oldPlayerId = Guid.Empty;
            if (player != null)
            {
                var oldPlayerComponent = player.GetComponent<AbstractPlayer>();
                if (oldPlayerComponent != null)
                {
                    oldPlayerId = oldPlayerComponent.UniqueID();
                }
            }

            if (player != null)
            {
                Destroy(player);
                player = null;
            }

            var spawnSource = localTeam == ETeam.Red ? redTeamRespawnPoints : blueTeamRespawnPoint;
            var spawnPos = GetRandomSpawnPoint(spawnSource, localTeam);
            var myPlayer = CreateMyPlayer(spawnPos, localTeam);
            if (myPlayer != null)
            {
                var playerComponent = myPlayer.GetComponent<AbstractPlayer>();
                if (oldPlayerId != Guid.Empty)
                {
                    playerComponent?.SetUniqueID(oldPlayerId);
                }
                AttachPlayerId(myPlayer, ResolveLocalPlayerId());
            }
            Debug.Log($"[TDM] My player respawned at {spawnPos}");
        }

        private void SendKillEventToServer(ETeam killerTeam, ETeam victimTeam)
        {
            if (networkManager == null || !networkManager.IsConnected()) return;

            var json = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.TeamKill,
                ["KillerTeam"] = killerTeam.ToString(),
                ["VictimTeam"] = victimTeam.ToString()
            };

            networkManager.SendToServer(json);
            Debug.Log($"[TDM] Sent kill event to server: {killerTeam} killed {victimTeam}");
        }

        private void OnlineEventParser(AbstractMatchEvent e)
        {
            if (e == null)
            {
                return;
            }

            var eventName = e.EventName;

            if ("FlagReturnEvent" == eventName)
            {
            }

            if ("FlagLostEvent" == eventName)
            {
            }
        }

        private void OfflineEventParser(AbstractGameEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (e is PlayerKillEvent killEvent)
            {
                ProcessKillEvent(killEvent);
            }
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e == null)
            {
                return;
            }

            OfflineEventParser(e);
            OnlineEventParser(e as AbstractMatchEvent);

            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                SendEventToServer(e);
            }
        }

        private void ProcessKillEvent(PlayerKillEvent e)
        {
            Debug.Log($"[TDM] Kill event processed: {e.KillerID()} killed {e.VictimID()}");
        }

        private void SendEventToServer(AbstractGameEvent e)
        {
            if (networkManager == null || !networkManager.IsConnected()) return;

            JObject json = null;

            if (e is PlayerKillEvent killEvent)
            {
                json = new JObject
                {
                    ["MessageType"] = "PlayerKill",
                    ["KillerId"] = killEvent.KillerID(),
                    ["VictimId"] = killEvent.VictimID(),
                    ["WeaponType"] = killEvent.WeaponType(),
                    ["Headshot"] = killEvent.IsHeadshot()
                };
            }
            else if (e is PlayerDeadEvent deadEvent)
            {
                json = RUDPMessageBuilder.CreatePlayerDeath(deadEvent.PlayerID(), deadEvent.KillerID());
            }

            if (json != null)
            {
                networkManager.SendToServer(json);
            }
        }

        protected override void OnNetworkDataRecved(JObject obj)
        {
            var messageType = OpenGSCore.MessageType.Normalize(obj["MessageType"]?.ToString());

            switch (messageType)
            {
                case RUDPMessageTypes.TeamKill:
                    HandleTeamKill(obj);
                    break;
                case RUDPMessageTypes.KillScoreUpdate:
                    HandleScoreUpdate(obj);
                    break;
                case RUDPMessageTypes.PlayerKill:
                    HandlePlayerKill(obj);
                    break;
                default:
                    base.OnNetworkDataRecved(obj);
                    break;
            }
        }

        private void HandleTeamKill(JObject json)
        {
            var killerTeamStr = json["KillerTeam"]?.ToString() ?? "Red";
            var victimTeamStr = json["VictimTeam"]?.ToString() ?? "Blue";

            Enum.TryParse<ETeam>(killerTeamStr, out var killerTeam);
            Enum.TryParse<ETeam>(victimTeamStr, out var victimTeam);

            if (killerTeam == ETeam.Red)
            {
                redTeamKills++;
                scoreUIManager?.AddRedKill();
            }
            else if (killerTeam == ETeam.Blue)
            {
                blueTeamKills++;
                scoreUIManager?.AddBlueKill();
            }

            InvokeSafely(OnTeamKill, killerTeam, victimTeam, nameof(OnTeamKill));
            Debug.Log($"[TDM] Received team kill from server: {killerTeam} killed {victimTeam}");
        }

        /// <summary>
        /// The room's tally replaces what this client counted.
        /// <para>
        /// The client counts its own kills and sends its own claims, so a score it
        /// worked out for itself is a score of its own making for the whole match,
        /// and the two sides disagree about a number on the scoreboard.
        /// </para>
        /// </summary>
        protected override void ApplyServerTeamKills(int red, int blue)
        {
            redTeamKills = Math.Max(0, red);
            blueTeamKills = Math.Max(0, blue);

            scoreUIManager?.UpdateScoreFromServer(redTeamKills, blueTeamKills);
        }

        private void HandleScoreUpdate(JObject json)
        {
            var redKills = ReadInt(json, "RedTeamKills");
            var blueKills = ReadInt(json, "BlueTeamKills");
            redKills = Math.Max(0, redKills);
            blueKills = Math.Max(0, blueKills);

            scoreUIManager?.UpdateScoreFromServer(redKills, blueKills);
            Debug.Log($"[TDM] Score update: Red={redKills}, Blue={blueKills}");
        }

        private void HandlePlayerKill(JObject json)
        {
            var killerId = json["KillerId"]?.ToString();
            var victimId = json["VictimId"]?.ToString();
            var headshot = ReadBool(json, "Headshot");

            Debug.Log($"[TDM] Player kill: {killerId} killed {victimId} (headshot: {headshot})");
        }

        /// <summary>
        /// What a mode does with a result the server has already decided.
        /// </summary>
        protected override void OnServerMatchEnd(JObject json)
        {
            var winningTeam = json["WinningTeam"]?.ToString() ?? "Draw";
            var myTeam = json["MyTeam"]?.ToString() ?? "Spectator";

            Debug.Log($"[TDM] Match ended: winner={winningTeam}, myTeam={myTeam}");
            if (IsOfflineMatch())
            {
                StoreOfflineMatchResult(winningTeam, myTeam);
            }
            InvokeSafely(OnMatchEnded, Enum.TryParse(winningTeam, out ETeam team) ? team : ETeam.NoTeam, nameof(OnMatchEnded));
            ScheduleResultSceneTransition(0f);
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
                    Debug.LogError($"[TDMMatchMainScrip] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<ETeam, ETeam> handlers, ETeam killerTeam, ETeam victimTeam, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<ETeam, ETeam> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(killerTeam, victimTeam);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TDMMatchMainScrip] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private void StoreOfflineMatchResult(string winningTeam, string myTeam)
        {
            var players = ResolveLocalPlayers();
            var result = new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.MatchEndNotification,
                ["WinningTeam"] = winningTeam,
                ["MyTeam"] = string.IsNullOrWhiteSpace(myTeam) ? ResolveLocalTeamName() : myTeam,
                ["RedScore"] = redTeamKills,
                ["BlueScore"] = blueTeamKills,
                ["Players"] = new JArray(players.ConvertAll(p => p?.ToJson()))
            };

            matchRoomManager?.StoreOfflineMatchResult(result);
        }
    }
}
