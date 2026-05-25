using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// オンライン（TCPマルチプレイ）版のリザルト画面。
    /// GeneralServerNetworkManager から MatchResult イベントの JSON を受け取り、
    /// その勝敗データに基づいて画面を表示・次のウェイトルームへ戻る。
    /// </summary>
    public class OnlineResultScene : AbstractResultScene, INetworkManagerScript
    {
        private GeneralServerNetworkManager networkManager;

        [Header("UI Manager")]
        public AbstractMatchResultUIManager resultUIManager;

        protected override void Start()
        {
            base.Start();

            networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();

            if (networkManager != null)
            {
                networkManager.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(OnDataReceived)
                    .AddTo(this);

                networkManager.Subscribe(this);

                if (networkManager.LastMatchResult != null)
                {
                    OnDataReceived(networkManager.LastMatchResult);
                }
            }
            else
            {
                Debug.LogWarning("GeneralServerNetworkManager が見つかりません。オンライン結果を受け取れません。");
            }
        }

        protected override void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.UnSubscribe(this);
            }

            base.OnDestroy();
        }

        public void ParseMessageFromGeneralServer(JObject json)
        {
            ParseNetworkMatchMessageFromServer(json);
        }

        public void ParseMessageFromMatchServer(JObject json)
        {
            // 使わない
        }

        private void OnDataReceived(JObject json)
        {
            ParseNetworkMatchMessageFromServer(json);
        }

        public void ParseNetworkMatchMessageFromServer(JObject json)
        {
            if (json == null)
            {
                Debug.LogWarning("[OnlineResultScene] Match result json is null.");
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (messageType != MessageType.MatchResult && messageType != MessageType.MatchEndNotification)
            {
                Debug.LogWarning($"[OnlineResultScene] Unsupported message type: {messageType}");
                return;
            }

            var winningTeam = ReadString(json, "WinningTeam", "WinnerTeam", "WinningSide", "Winner", "ResultTeam", "Team");
            if (string.IsNullOrWhiteSpace(winningTeam))
            {
                winningTeam = "Draw";
            }

            var myTeam = ReadString(json, "MyTeam", "PlayerTeam", "SelfTeam");
            if (string.IsNullOrWhiteSpace(myTeam))
            {
                myTeam = "Spectator";
            }

            ShowResult(winningTeam, myTeam);

            if (resultUIManager == null)
            {
                return;
            }

            var playersArray = FindPlayersArray(json);
            if (playersArray == null)
            {
                Debug.LogWarning("[OnlineResultScene] Players array was not found in match result json.");
            }

            var parsedData = new System.Collections.Generic.List<PlayerMatchResultData>();

            foreach (var pToken in playersArray ?? new JArray())
            {
                if (pToken is not JObject p)
                {
                    continue;
                }

                parsedData.Add(new PlayerMatchResultData
                {
                    PlayerId = ReadString(p, "PlayerId", "Id", "PlayerID", "AccountId"),
                    PlayerName = ReadString(p, "Unknown", "Name", "PlayerName", "DisplayName", "Nickname", "AccountName"),
                    Team = ReadString(p, "None", "Team", "TeamName", "PlayerTeam"),
                    Kills = ReadInt(p, 0, "Kills", "KillCount", "TotalKill"),
                    Deaths = ReadInt(p, 0, "Deaths", "DeathCount"),
                    Score = ReadInt(p, ReadInt(p, 0, "Score", "TotalScore", "Points"), "Kills", "KillCount", "TotalKill")
                });
            }

            resultUIManager.UpdateResultList(parsedData);
        }

        protected override void GoToNextScene()
        {
            var nextScene = GeneralSceneMasterData.Instance().OnlineWaitRoomScene();
            RequestSceneTransition(nextScene, () =>
            {
                if (networkManager != null)
                {
                    networkManager.ClearLastMatchResult();
                }
            }, "ResultToWaitRoom");
        }

        public void TestFunc()
        {
            Debug.Log("[OnlineResultScene] TestFunc");
        }

        public void OnConnected()
        {
            Debug.Log("[OnlineResultScene] Connected");
        }

        public void OnDisconnected()
        {
            Debug.Log("[OnlineResultScene] Disconnected");
        }

        private static string ReadString(JObject json, params string[] keys)
        {
            return ReadString(json, string.Empty, keys);
        }

        private static string ReadString(JObject json, string fallback, params string[] keys)
        {
            if (json == null)
            {
                return fallback;
            }

            foreach (var key in keys)
            {
                var value = json[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return fallback;
        }

        private static int ReadInt(JObject json, int fallback, params string[] keys)
        {
            if (json == null)
            {
                return fallback;
            }

            foreach (var key in keys)
            {
                var token = json[key];
                if (token == null)
                {
                    continue;
                }

                if (int.TryParse(token.ToString(), out var parsed))
                {
                    return parsed;
                }
            }

            return fallback;
        }

        private static JArray FindPlayersArray(JObject json)
        {
            if (json == null)
            {
                Debug.LogWarning("[OnlineResultScene] FindPlayersArray received null json.");
                return null;
            }

            var direct = json["Players"] as JArray;
            if (direct != null)
            {
                return direct;
            }

            var result = json["Result"] as JObject;
            if (result != null)
            {
                return result["Players"] as JArray;
            }

            var roomInfo = json["RoomInfo"] as JObject;
            if (roomInfo != null)
            {
                return roomInfo["Players"] as JArray;
            }

            Debug.LogWarning("[OnlineResultScene] Could not resolve players array from json.");
            return null;
        }
    }
}
