using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Runs a mission.
    /// <para>
    /// It used to be finished by pressing a key. F1 cleared the mission, F2
    /// failed it and R rebuilt the player, all of them in Update, and the mode read
    /// the same keys whichever match was running. A mission could therefore be
    /// completed by a player who had done nothing, and the only way a mission
    /// ended on its own was the local player's death, which failed the whole thing
    /// whatever lives were left.
    /// </para>
    /// <para>
    /// The outcome is the server's now, the same as every other mode. What this
    /// script owns is the player's body: spawning it, and taking it back or not
    /// according to what the room says about respawning.
    /// </para>
    /// </summary>
    public class MissionMainScript : AbstractMatchMainScript
    {
        public MissionReSpawnPoints respawnPoints;
        public MissionReSpawnPoints points;
        public MissionAndQuestMediateObject mediateObject;

        private bool respawnPending;

        private new void Start()
        {
            base.Start();
            GameStart();
        }

        private void GameStart()
        {
            if (isStarted)
            {
                return;
            }

            isStarted = true;
            endFlag = false;
            respawnPending = false;
            PlayGameStartVoice();
            SpawnPlayer();
            Debug.Log("[MissionMainScript] GameStart");
        }

        private void SpawnPlayer()
        {
            var spawnPosition = ResolveSpawnPosition();
            if (player != null)
            {
                Destroy(player);
                player = null;
            }

            player = CreateMyPlayer(spawnPosition, ETeam.NoTeam);
            Debug.Log($"[MissionMainScript] SpawnPlayer at {spawnPosition}");
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (respawnPoints != null)
            {
                return GetRandomSpawnPoint(respawnPoints);
            }

            if (points != null)
            {
                return GetRandomSpawnPoint(points);
            }

            // The world origin is not a spawn point. A mission whose scene has no
            // respawn object wired returned it in silence, so the player came back in
            // the middle of the level with nothing in the log to say why. Mission3,
            // Mission4 and the Quest scenes are still in that state and need a
            // RespawnPoints object placed and assigned rather than a default.
            Debug.LogWarning(
                $"[{GetType().Name}] No respawn points are assigned, so the player spawns at " +
                $"the world origin. Assign respawnPoints on this scene's MissionMainScript.");
            return Vector3.zero;
        }

        /// <summary>
        /// Whether this mission brings a player back after they go down.
        /// <para>
        /// It is the room's answer, not the mode's. A mission is usually played on
        /// a life count, so a player out of lives stays out, and deciding it here
        /// from the mode alone brought everybody back for ever.
        /// </para>
        /// </summary>
        private bool CanRespawnPlayer()
        {
            return MatchModeResolver.CanRespawnCurrentMatch();
        }

        public void RespawnPlayer()
        {
            if (endFlag || respawnPending || !CanRespawnPlayer())
            {
                return;
            }

            respawnPending = true;
            SpawnPlayer();
            respawnPending = false;

            // The server holds the player's lives, so it is told this player is
            // back rather than left holding the health it had already reduced to
            // zero: a player the server still has down cannot be hit at all.
            ReportRespawnToServer();
        }

        /// <summary>
        /// The server has ended the match, and says how the mission went.
        /// <para>
        /// The result is its own message, not a winning team: a mission is cleared
        /// or it is not, and reading a winner out of it would report a draw for
        /// every mission that succeeded.
        /// </para>
        /// </summary>
        protected override void OnServerMatchEnd(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var result = json["Result"]?.ToString() ?? string.Empty;
            var success = json["Success"]?.ToObject<bool>() ?? false;
            var lifeRemaining = json["LifeRemaining"]?.ToObject<int>() ?? 0;

            Debug.Log(
                $"[Mission] The server ended the mission: result={result}, " +
                $"success={success}, lives={lifeRemaining}");

            if (success)
            {
                StoreOfflineMatchResult(json);
                OnWin();
            }
            else
            {
                OnLose();
            }
        }

        /// <summary>
        /// A clear gate in the scene was reached.
        /// <para>
        /// Offline this is the only thing that can clear a mission, and it ends the
        /// match here because there is no server to say so. Online it reports the
        /// gate instead: the server's mission rule holds the objective count and the
        /// lives and already checks itself every tick, so the room ends the match on
        /// its own terms and tells every client the same result. A client that ended
        /// an online mission itself would settle a match the server was still
        /// running.
        /// </para>
        /// </summary>
        public virtual void MissionClear()
        {
            if (endFlag)
            {
                return;
            }

            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                var network = FindAnyObjectByType<ClientNetworkManager>();
                if (network == null)
                {
                    Debug.LogWarning(
                        "[MissionMainScript] A clear gate was reached online, but there is no " +
                        "network manager to report it to.");
                    return;
                }

                Debug.Log("[MissionMainScript] A clear gate was reached. Reporting it to the server.");
                network.ReportMissionClear();
                return;
            }

            Debug.Log("[MissionMainScript] A clear gate was reached.");
            EndMatchLocally(BuildOfflineClearResult(), gotoResultSceneWaitTime);
        }

        /// <summary>
        /// The result of a mission this client cleared itself.
        /// <para>
        /// Only the outcome is asserted. Lives, score and kills are the server's to
        /// count and this client is not keeping them, so they are left out and the
        /// result screen reads them as zero rather than as a number nobody measured.
        /// </para>
        /// </summary>
        private static JObject BuildOfflineClearResult()
        {
            return new JObject
            {
                ["Result"] = "Clear",
                ["ResultType"] = "Mission",
                ["Success"] = true
            };
        }

        /// <summary>
        /// Keeps the result the server sent, so a result scene that is reached
        /// without the live room can still show what happened.
        /// </summary>
        private void StoreOfflineMatchResult(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var missionScore = MissionScore.FromMissionResult(json);
            if (missionScore != null)
            {
                Debug.Log(
                    $"[Mission] Recorded result: clear={missionScore.Success}, " +
                    $"lives={missionScore.LifeRemaining}, score={missionScore.Score}");
            }
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (e is GameStartEvent)
            {
                GameStart();
                return;
            }

            if (e is GameEndEvent)
            {
                // The server has ruled, and it sends the match end for that. A
                // local end here would decide a mission this client has no standing
                // to decide.
                Debug.Log("[MissionMainScript] GameEndEvent received.");
                return;
            }

            if (e is PlayerDeadEvent deadEvent)
            {
                var myPlayerId = player != null ? player.GetComponent<IBattlePlayer>()?.UniqueID().ToString() : null;
                if (!string.IsNullOrWhiteSpace(myPlayerId) && deadEvent.PlayerID() == myPlayerId)
                {
                    OnMyPlayerDead();
                }
                return;
            }

            Debug.Log($"[MissionMainScript] PostEvent: {e.EventName}");
        }

        public override void OnMyPlayerDead()
        {
            if (endFlag)
            {
                return;
            }

            // Going down is not the end of a mission. It used to be: the local
            // player's death failed the whole thing whatever lives were left, so a
            // mission on three lives ended the first time anybody was shot.
            if (!CanRespawnPlayer())
            {
                Debug.Log("[Mission] The local player is out and this mission does not respawn.");
                return;
            }

            Debug.Log("[Mission] The local player is down and will be brought back.");
            RespawnPlayer();
        }
    }
}