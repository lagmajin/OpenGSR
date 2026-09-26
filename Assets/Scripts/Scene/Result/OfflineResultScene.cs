using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// オフライン（シングルプレイ）用リザルト画面。
    /// ネットワークには一切接続せず、ローカルの GameManager や SessionData から直接情報を読み出す。
    /// </summary>
    public class OfflineResultScene : AbstractResultScene
    {
        [Header("UI Manager")]
        public AbstractMatchResultUIManager resultUIManager;

        protected override void Start()
        {
            base.Start();

            MatchRoomManager manager = null;
            try
            {
                manager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OfflineResultScene] MatchRoomManager resolution failed: {ex.Message}");
            }
            var result = manager != null ? manager.LastOfflineMatchResult : null;

            string winningTeam = result?["WinningTeam"]?.ToString() ?? "Draw";
            string myTeam = ResolveMyTeam(result) ?? "Draw";

            ShowResult(winningTeam, myTeam);
            RecordHistory(result, winningTeam, myTeam);
            ShowPlayerList(result);
        }

        private static void RecordHistory(JObject result, string winningTeam, string myTeam)
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            var playerName = profile != null ? profile.DisplayName : string.Empty;
            var playerId = profile != null ? profile.GlobalUserId : string.Empty;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Player";
            }

            var outcome = string.Equals(winningTeam, "Draw", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(myTeam, "Draw", System.StringComparison.OrdinalIgnoreCase)
                ? "Draw"
                : string.Equals(winningTeam, myTeam, System.StringComparison.OrdinalIgnoreCase)
                    ? "Win"
                    : "Lose";

            var score = 0;
            var kills = 0;
            var deaths = 0;
            if (result?["Players"] is JArray players)
            {
                foreach (var token in players)
                {
                    if (token is not JObject player)
                    {
                        continue;
                    }

                    var resultPlayerId = player["PlayerId"]?.ToString()
                        ?? player["PlayerID"]?.ToString()
                        ?? player["Id"]?.ToString();
                    var name = player["Name"]?.ToString() ?? player["PlayerName"]?.ToString();
                    var isLocalPlayer = !string.IsNullOrWhiteSpace(playerId)
                        && string.Equals(resultPlayerId, playerId, System.StringComparison.OrdinalIgnoreCase);
                    if (!isLocalPlayer && string.IsNullOrWhiteSpace(resultPlayerId))
                    {
                        isLocalPlayer = !string.IsNullOrWhiteSpace(name)
                            && string.Equals(name, playerName, System.StringComparison.OrdinalIgnoreCase);
                    }

                    if (isLocalPlayer)
                    {
                        kills = ReadNonNegativeInt(player["Kills"]);
                        deaths = ReadNonNegativeInt(player["Deaths"]);
                        score = ReadNonNegativeInt(player["Score"], kills);
                        break;
                    }
                }
            }

            if (MatchHistoryManager.Instance != null)
            {
                MatchHistoryManager.Instance.RecordMatch(playerName, "Offline", outcome, score, kills, deaths);
            }
        }

        protected override void GoToNextScene()
        {
            RequestSceneTransition(GeneralSceneMasterData.Instance().OfflineWaitRoomScene(), "ResultToOfflineWaitRoom");
        }

        private static string ResolveMyTeam(JObject result)
        {
            if (result == null)
            {
                return "Draw";
            }

            var players = result["Players"] as JArray;
            if (players == null)
            {
                return "Draw";
            }

            var profile = AccountManager.Instance?.CurrentProfile;
            var localId = profile?.GlobalUserId;
            var localName = profile?.DisplayName;
            string fallbackTeam = null;

            foreach (var token in players)
            {
                var player = token as JObject;
                if (player == null)
                {
                    continue;
                }

                var team = player["Team"]?.ToString();
                if (!string.IsNullOrWhiteSpace(team) && team != "NoTeam")
                {
                    fallbackTeam ??= team;

                    var playerId = player["PlayerId"]?.ToString()
                        ?? player["PlayerID"]?.ToString()
                        ?? player["Id"]?.ToString();
                    var playerName = player["Name"]?.ToString() ?? player["PlayerName"]?.ToString();
                    var isLocal = !string.IsNullOrWhiteSpace(localId)
                        && string.Equals(playerId, localId, System.StringComparison.OrdinalIgnoreCase);
                    if (!isLocal && string.IsNullOrWhiteSpace(playerId))
                    {
                        isLocal = !string.IsNullOrWhiteSpace(localName)
                            && string.Equals(playerName, localName, System.StringComparison.OrdinalIgnoreCase);
                    }

                    if (isLocal)
                    {
                        return team;
                    }
                }
            }

            return fallbackTeam ?? "Draw";
        }

        private void ShowPlayerList(JObject result)
        {
            if (resultUIManager == null || result == null)
            {
                resultUIManager?.UpdateResultList(new List<PlayerMatchResultData>());
                return;
            }

            var playersArray = result["Players"] as JArray;
            if (playersArray == null)
            {
                resultUIManager.UpdateResultList(new List<PlayerMatchResultData>());
                return;
            }

            var parsedData = new List<PlayerMatchResultData>();
            foreach (var pToken in playersArray)
            {
                var p = pToken as JObject;
                if (p == null) continue;

                parsedData.Add(new PlayerMatchResultData()
                {
                    PlayerId = p["PlayerId"]?.ToString() ?? p["Id"]?.ToString() ?? "",
                    PlayerName = p["Name"]?.ToString() ?? p["PlayerName"]?.ToString() ?? "Unknown",
                    Team = p["Team"]?.ToString() ?? p["TeamName"]?.ToString() ?? "None",
                    Kills = ReadNonNegativeInt(p["Kills"]),
                    Deaths = ReadNonNegativeInt(p["Deaths"]),
                    Score = ReadNonNegativeInt(p["Score"], ReadNonNegativeInt(p["Kills"]))
                });
            }

            resultUIManager.UpdateResultList(parsedData);
        }

        private static int ReadNonNegativeInt(JToken token, int fallback = 0)
        {
            try
            {
                return Mathf.Max(0, token?.ToObject<int>() ?? fallback);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OfflineResultScene] Invalid result number: {ex.Message}");
                return Mathf.Max(0, fallback);
            }
        }
    }
}
