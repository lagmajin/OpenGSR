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

            var manager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            var result = manager != null ? manager.LastOfflineMatchResult : null;

            string winningTeam = result?["WinningTeam"]?.ToString() ?? "Draw";
            string myTeam = ResolveMyTeam(result) ?? "Draw";

            ShowResult(winningTeam, myTeam);
            ShowPlayerList(result);
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
                    return team;
                }
            }

            return "Draw";
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
                    Kills = p["Kills"]?.ToObject<int>() ?? 0,
                    Deaths = p["Deaths"]?.ToObject<int>() ?? 0,
                    Score = p["Score"]?.ToObject<int>() ?? p["Kills"]?.ToObject<int>() ?? 0
                });
            }

            resultUIManager.UpdateResultList(parsedData);
        }
    }
}
