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

        private new void Start()
        {
            base.Start();
            Application.targetFrameRate = 30;

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

        private void GameSetup()
        {
            PlayGameStartVoice();
            localTeam = ResolveLocalTeam(matchRoomManager != null ? matchRoomManager.WaitRoom : null);
            CreateMyPlayerLocally();
            CreateOtherPlayers();

            if (scoreUIManager != null)
            {
                scoreUIManager.StartMatch();
            }
        }

        protected override float ResolveMatchDuration()
        {
            return 600f;
        }

        protected override void OnTimeUp()
        {
            Debug.Log("[TDM] Time up!");
            var winningTeam = redTeamKills >= blueTeamKills ? "Red" : "Blue";
            HandleMatchEnd(new Newtonsoft.Json.Linq.JObject
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
                HandleMatchEnd(new JObject
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
                var winningTeam = redTeamKills >= blueTeamKills ? "Red" : "Blue";
                HandleMatchEnd(new JObject { ["WinningTeam"] = winningTeam, ["MyTeam"] = ResolveLocalTeamName(), ["RedTeamKills"] = redTeamKills, ["BlueTeamKills"] = blueTeamKills });
                return;
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                HandleMatchEnd(new JObject
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

            OnPlayerKilled?.Invoke(victimTeam);
            OnTeamKill?.Invoke(killerTeam, victimTeam);

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
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e is PlayerKillEvent killEvent)
            {
                ProcessKillEvent(killEvent);
            }

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
                case OpenGSCore.MessageType.MatchEndNotification:
                    HandleMatchEnd(obj);
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

            OnTeamKill?.Invoke(killerTeam, victimTeam);
            Debug.Log($"[TDM] Received team kill from server: {killerTeam} killed {victimTeam}");
        }

        private void HandleScoreUpdate(JObject json)
        {
            var redKills = json["RedTeamKills"]?.ToObject<int>() ?? 0;
            var blueKills = json["BlueTeamKills"]?.ToObject<int>() ?? 0;

            scoreUIManager?.UpdateScoreFromServer(redKills, blueKills);
            Debug.Log($"[TDM] Score update: Red={redKills}, Blue={blueKills}");
        }

        private void HandlePlayerKill(JObject json)
        {
            var killerId = json["KillerId"]?.ToString();
            var victimId = json["VictimId"]?.ToString();
            var headshot = json["Headshot"]?.ToObject<bool>() ?? false;

            Debug.Log($"[TDM] Player kill: {killerId} killed {victimId} (headshot: {headshot})");
        }

        private void HandleMatchEnd(JObject json)
        {
            if (!TryBeginMatchEnd())
            {
                return;
            }

            var winningTeam = json["WinningTeam"]?.ToString() ?? "Draw";
            var myTeam = json["MyTeam"]?.ToString() ?? "Spectator";

            Debug.Log($"[TDM] Match ended: winner={winningTeam}, myTeam={myTeam}");
            if (IsOfflineMatch())
            {
                StoreOfflineMatchResult(winningTeam, myTeam);
            }
            OnMatchEnded?.Invoke(Enum.TryParse(winningTeam, out ETeam team) ? team : ETeam.NoTeam);
            ScheduleResultSceneTransition(0f);
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
