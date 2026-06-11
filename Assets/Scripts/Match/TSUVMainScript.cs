using System;
using System.Collections.Generic;
using System.Linq;
using CoreETeam = OpenGSCore.ETeam;
using OpenGSCore;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class TSUVMainScript : AbstractMatchMainScript, ITSuvMainScript
    {
        public static TSUVMainScript Instance { get; private set; }

        public event Action<ETeam> OnPlayerKilled;
        public event Action<ETeam, ETeam> OnTeamKill;
        public event Action<ETeam> OnMatchEnded;

        [SerializeField] [Required] private TeamReSpawnPoints redTeamRespawnPoints;
        [SerializeField] [Required] private TeamReSpawnPoints blueTeamRespawnPoint;

        private MatchRUDPServerNetworkManager networkManager;
        private ETeam localTeam = ETeam.Blue;

        // チーム生存数管理
        private int redAliveCount;
        private int blueAliveCount;
        private int redKills;
        private int blueKills;

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
                Debug.LogWarning($"[TSUV] Failed to resolve MatchRUDPServerNetworkManager: {ex.Message}");
                networkManager = null;
            }

            Debug.Log("[TSUV] Team Survival GameStart");
            Invoke(nameof(GameSetup), 0.1f);
        }

        private void GameSetup()
        {
            PlayGameStartVoice();
            localTeam = ResolveLocalTeam(matchRoomManager?.WaitRoom);
            CountTeamPlayers();
            CreateMyPlayerLocally();
            CreateOtherPlayers();
        }

        /// <summary>
        /// 待機部屋のプレイヤー情報から各チームの生存数を初期化する。
        /// </summary>
        private void CountTeamPlayers()
        {
            var room = matchRoomManager?.WaitRoom;
            if (room == null)
            {
                // フォールバック: 自身だけ
                redAliveCount = localTeam == ETeam.Red ? 1 : 0;
                blueAliveCount = localTeam == ETeam.Blue ? 1 : 0;
                return;
            }

            var players = room.AllPlayers();
            redAliveCount = players?.Count(p => p != null && p.Team == CoreETeam.Red) ?? 0;
            blueAliveCount = players?.Count(p => p != null && p.Team == CoreETeam.Blue) ?? 0;

            // 自分のチームに最低1人はいることを保証
            if (localTeam == ETeam.Red && redAliveCount == 0) redAliveCount = 1;
            if (localTeam == ETeam.Blue && blueAliveCount == 0) blueAliveCount = 1;

            Debug.Log($"[TSUV] Teams: Red={redAliveCount}, Blue={blueAliveCount}");
        }

        private void CreateMyPlayerLocally()
        {
            var spawnSource = localTeam == ETeam.Blue ? blueTeamRespawnPoint : redTeamRespawnPoints;
            var spawnPos = GetRandomSpawnPoint(spawnSource, localTeam);
            var myPlayer = CreateMyPlayer(spawnPos, localTeam);
            AttachPlayerId(myPlayer, ResolveLocalPlayerId());
        }

        private void CreateOtherPlayers()
        {
            var room = matchRoomManager?.WaitRoom;
            if (room == null)
            {
                Debug.Log("[TSUV] No wait room found. Skipping other player spawn.");
                return;
            }

            var players = room.AllPlayers();
            if (players == null || players.Count == 0)
            {
                Debug.Log("[TSUV] Wait room has no players to spawn.");
                return;
            }

            var localPlayerId = ResolveLocalPlayerId();
            var spawnCount = 0;

            foreach (var info in players)
            {
                if (info == null || string.IsNullOrWhiteSpace(info.Id))
                    continue;

                if (!string.IsNullOrWhiteSpace(localPlayerId) &&
                    string.Equals(info.Id, localPlayerId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var team = Enum.TryParse<CoreETeam>(info.Team.ToString(), out var parsedTeam) && parsedTeam != CoreETeam.NoTeam
                    ? ToLocalTeam(parsedTeam)
                    : ETeam.Blue;

                var spawnSource = team == ETeam.Red ? redTeamRespawnPoints : blueTeamRespawnPoint;
                var spawnPos = GetRandomSpawnPoint(spawnSource, team);

                var prefab = prefabMasterData?.SearchPlayerPrefab(info.playerCharacter.ToString());
                if (prefab == null)
                {
                    prefab = prefabMasterData?.SearchPlayerPrefab(EPlayerCharacter.Misty.ToString());
                    if (prefab == null) continue;
                }

                var playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
                playerObj.name = $"OtherPlayer_{info.Name}";

                var player = playerObj.GetComponent<AbstractPlayer>();
                if (player != null)
                {
                    player.SetPlayerType(EPlayerType.OtherPlayer);
                    player.SetTeam(team);
                    AttachPlayerId(playerObj, info.Id);
                    player.OnSpawn();
                }

                spawnCount++;
            }

            Debug.Log($"[TSUV] Spawned {spawnCount} other players.");
        }

        private void Update()
        {
            if (HandleEscapeToBackScene())
                return;

            if (Input.GetKeyDown(KeyCode.F1))
                ForceGameEnd("Red");

            if (Input.GetKeyDown(KeyCode.F2))
                ForceGameEnd("Blue");
        }

        private void ForceGameEnd(string winningTeam)
        {
            endFlag = true;
            var myLabel = ResolveLocalTeamName();
            Debug.Log($"[TSUV] Force game end: winner={winningTeam}, myTeam={myLabel}");
            StoreOfflineMatchResult(winningTeam, myLabel);
            GoToResult();
        }

        /// <summary>
        /// プレイヤー死亡時のチーム生存数管理。
        /// TDM の OnPlayerDead とは異なり、生存数を減らして全滅判定を行う。
        /// </summary>
        public void OnPlayerDead(ETeam victimTeam, ETeam killerTeam)
        {
            // 生存数を減らす
            if (victimTeam == ETeam.Red)
                redAliveCount = Math.Max(0, redAliveCount - 1);
            else if (victimTeam == ETeam.Blue)
                blueAliveCount = Math.Max(0, blueAliveCount - 1);

            // キルを記録
            if (killerTeam == ETeam.Red)
                redKills++;
            else if (killerTeam == ETeam.Blue)
                blueKills++;

            OnPlayerKilled?.Invoke(victimTeam);
            OnTeamKill?.Invoke(killerTeam, victimTeam);

            Debug.Log($"[TSUV] Player killed: victim={victimTeam}, killer={killerTeam}. " +
                      $"Alive: Red={redAliveCount}, Blue={blueAliveCount}");

            if (GameManager != null && GameManager.IsOnlineGameMode)
                SendKillEventToServer(killerTeam, victimTeam);

            // チーム全滅チェック
            CheckTeamElimination();
        }

        protected override float ResolveMatchDuration()
        {
            return 600f;
        }

        protected override void OnTimeUp()
        {
            if (endFlag) return;
            Debug.Log("[TSUV] Time up!");
            endFlag = true;
            string winningTeam = redAliveCount > blueAliveCount ? "Red" : (blueAliveCount > redAliveCount ? "Blue" : "Draw");
            var myLabel = ResolveLocalTeamName();
            OnMatchEnded?.Invoke(System.Enum.TryParse(winningTeam, out ETeam team) ? team : ETeam.NoTeam);
            StoreOfflineMatchResult(winningTeam, myLabel);
            Invoke(nameof(GoToResultScene), gotoResultSceneWaitTime);
        }

        /// <summary>
        /// チーム全滅 or 時間切れをチェックして試合終了。
        /// </summary>
        private void CheckTeamElimination()
        {
            if (endFlag)
                return;

            // 両チームの生存者がいる場合は継続
            if (redAliveCount > 0 && blueAliveCount > 0)
                return;

            endFlag = true;

            string winningTeam = redAliveCount > 0 ? "Red" : (blueAliveCount > 0 ? "Blue" : "Draw");
            var myLabel = ResolveLocalTeamName();

            Debug.Log($"[TSUV] Team eliminated! Winner={winningTeam}");
            OnMatchEnded?.Invoke(Enum.TryParse(winningTeam, out ETeam team) ? team : ETeam.NoTeam);

            StoreOfflineMatchResult(winningTeam, myLabel);
            Invoke(nameof(GoToResultScene), gotoResultSceneWaitTime);
        }

        public override void OnMyPlayerDead()
        {
            // Survival = リスポーンなし。観戦モードに移行
            Debug.Log("[TSUV] My player died. Switching to spectator mode.");

            // プレイヤーを非表示にして観戦カメラに切り替え
            if (player != null)
                player.SetActive(false);

            EnterSpectatorMode(player?.transform);
        }

        private void GoToResultScene()
        {
            GoToResult();
        }

        private void SendKillEventToServer(ETeam killerTeam, ETeam victimTeam)
        {
            if (networkManager == null || !networkManager.IsConnected())
                return;

            var json = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.TeamKill,
                ["KillerTeam"] = killerTeam.ToString(),
                ["VictimTeam"] = victimTeam.ToString()
            };

            networkManager.SendToServer(json);
            Debug.Log($"[TSUV] Sent kill event to server: {killerTeam} killed {victimTeam}");
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e == null) return;

            if (e is PlayerKillEvent killEvent)
            {
                ProcessKillEvent(killEvent);
            }

            if (e is PlayerDeadEvent deadEvent && GameManager != null && GameManager.IsOnlineGameMode)
            {
                SendEventToServer(e);
            }
        }

        private void ProcessKillEvent(PlayerKillEvent e)
        {
            Debug.Log($"[TSUV] Kill event: {e.KillerID()} killed {e.VictimID()}");
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
                networkManager.SendToServer(json);
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

            Enum.TryParse<CoreETeam>(killerTeamStr, out var killerTeamCore);
            Enum.TryParse<CoreETeam>(victimTeamStr, out var victimTeamCore);
            var killerTeam = ToLocalTeam(killerTeamCore);
            var victimTeam = ToLocalTeam(victimTeamCore);

            // 生存数管理
            if (victimTeam == ETeam.Red)
                redAliveCount = Math.Max(0, redAliveCount - 1);
            else if (victimTeam == ETeam.Blue)
                blueAliveCount = Math.Max(0, blueAliveCount - 1);

            if (killerTeam == ETeam.Red)
                redKills++;
            else if (killerTeam == ETeam.Blue)
                blueKills++;

            OnTeamKill?.Invoke(killerTeam, victimTeam);
            Debug.Log($"[TSUV] Received team kill: {killerTeam} killed {victimTeam}. " +
                      $"Alive: Red={redAliveCount}, Blue={blueAliveCount}");

            CheckTeamElimination();
        }

        private void HandleScoreUpdate(JObject json)
        {
            var red = json["RedTeamKills"]?.ToObject<int>() ?? 0;
            var blue = json["BlueTeamKills"]?.ToObject<int>() ?? 0;
            redKills = Math.Max(redKills, red);
            blueKills = Math.Max(blueKills, blue);
            Debug.Log($"[TSUV] Score update: Red={red}, Blue={blue}");
        }

        private void HandlePlayerKill(JObject json)
        {
            var killerId = json["KillerId"]?.ToString();
            var victimId = json["VictimId"]?.ToString();
            var headshot = json["Headshot"]?.ToObject<bool>() ?? false;
            Debug.Log($"[TSUV] Player kill: {killerId} killed {victimId} (headshot: {headshot})");
        }

        private void HandleMatchEnd(JObject json)
        {
            endFlag = true;

            var winningTeam = json["WinningTeam"]?.ToString() ?? "Draw";
            var myTeam = json["MyTeam"]?.ToString() ?? "Spectator";

            Debug.Log($"[TSUV] Match ended: winner={winningTeam}, myTeam={myTeam}");

            if (IsOfflineMatch())
                StoreOfflineMatchResult(winningTeam, myTeam);

            OnMatchEnded?.Invoke(Enum.TryParse(winningTeam, out ETeam team) ? team : ETeam.NoTeam);
            Invoke(nameof(GoToResultScene), gotoResultSceneWaitTime);
        }

        private void StoreOfflineMatchResult(string winningTeam, string myTeam)
        {
            var safeMyTeam = string.IsNullOrWhiteSpace(myTeam) ? ResolveLocalTeamName() : myTeam;
            var result = new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.MatchEndNotification,
                ["WinningTeam"] = winningTeam,
                ["MyTeam"] = safeMyTeam,
                ["RedAliveCount"] = redAliveCount,
                ["BlueAliveCount"] = blueAliveCount,
                ["RedTeamKills"] = redKills,
                ["BlueTeamKills"] = blueKills,
                ["Players"] = new JArray(ResolveLocalPlayers().ConvertAll(p => p?.ToJson()))
            };

            matchRoomManager?.StoreOfflineMatchResult(result);
        }

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? string.Empty : profile.GlobalUserId;
        }

        private static void AttachPlayerId(GameObject playerObj, string playerId)
        {
            if (playerObj == null) return;

            var linker = playerObj.GetComponent<PlayerDataLinker>();
            if (linker == null)
                linker = playerObj.AddComponent<PlayerDataLinker>();

            linker.SetPlayerId(playerId ?? string.Empty);
        }

        private static ETeam ResolveLocalTeam(OpenGSCore.WaitRoom room)
        {
            if (room == null) return ETeam.Blue;

            var localId = ResolveLocalPlayerId();
            foreach (var player in room.AllPlayers())
            {
                if (player == null || string.IsNullOrWhiteSpace(player.Id))
                    continue;

                if (string.Equals(player.Id, localId, StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse<CoreETeam>(player.Team.ToString(), out var parsedTeam) &&
                    parsedTeam != CoreETeam.NoTeam)
                {
                    return ToLocalTeam(parsedTeam);
                }
            }

            return ETeam.Blue;
        }

        private static ETeam ToLocalTeam(CoreETeam team)
        {
            return team switch
            {
                CoreETeam.Red => ETeam.Red,
                CoreETeam.Blue => ETeam.Blue,
                _ => ETeam.NoTeam
            };
        }
    }
}
