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
        protected override void Start()
        {
            base.Start();

            var manager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            var result = manager != null ? manager.LastOfflineMatchResult : null;

            string winningTeam = result?["WinningTeam"]?.ToString() ?? "Draw";
            string myTeam = ResolveMyTeam(result) ?? "Draw";

            ShowResult(winningTeam, myTeam);
        }

        protected override void GoToNextScene()
        {
            SceneManager.LoadScene(GeneralSceneMasterData.Instance().OfflineWaitRoomScene());
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
    }
}
