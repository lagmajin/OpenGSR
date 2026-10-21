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
        private bool hasRecordedHistory;

        [Header("UI Manager")]
        public AbstractMatchResultUIManager resultUIManager;

        protected override void Start()
        {
            base.Start();

            try
            {
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch (System.Exception ex)
            {
                networkManager = null;
                Debug.LogWarning($"[OnlineResultScene] GeneralServerNetworkManager resolution failed: {ex.Message}");
            }

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

        /// <summary>
        /// A result that arrived from the match server rather than the lobby.
        /// <para>
        /// Both servers use the same notification envelope for a finished match, so
        /// the parsing is the same work; keeping one path means a result that came
        /// from the match server is not silently dropped while the same message
        /// from the lobby renders. It used to be an empty method with a note saying
        /// it was unused, which left that whole path doing nothing.
        /// </para>
        /// </summary>
        public void ParseMessageFromMatchServer(JObject json)
        {
            ParseNetworkMatchMessageFromServer(json);
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

            var winningPlayerId = ReadString(json, "WinningPlayerId", "WinningPlayerID", "WinnerPlayerId", "Winner");
            var winnerName = ReadString(json, "WinnerName", "WinningPlayerName", "WinnerDisplayName");
            var localPlayerId = ResolveLocalPlayerId();
            var isTeamResult = IsTeamResultToken(winningTeam);

            if (!isTeamResult && IsPlayerResultToken(winningPlayerId))
            {
                // DeathMatch 系の個人戦は、勝者プレイヤー ID を比較対象にする。
                winningTeam = winningPlayerId;
                myTeam = localPlayerId;
            }

            ShowResult(winningTeam, myTeam);

            if (!hasRecordedHistory)
            {
                var profile = AccountManager.Instance?.CurrentProfile;
                var localPlayerName = profile?.DisplayName;
                if (string.IsNullOrWhiteSpace(localPlayerName))
                {
                    localPlayerName = localPlayerId;
                }

                var result = string.Equals(winningTeam, "Draw", System.StringComparison.OrdinalIgnoreCase)
                    ? "Draw"
                    : string.Equals(winningTeam, myTeam, System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(winningTeam, localPlayerId, System.StringComparison.OrdinalIgnoreCase)
                        ? "Win"
                        : "Lose";
                var gameMode = ReadString(json, "GameMode", "Mode");
                if (string.IsNullOrWhiteSpace(gameMode))
                {
                    gameMode = "Online";
                }

                var historyScore = 0;
                var historyKills = 0;
                var historyDeaths = 0;
                var historyPlayers = FindPlayersArray(json);
                foreach (var token in historyPlayers ?? new JArray())
                {
                    if (token is not JObject player)
                    {
                        continue;
                    }

                    var playerId = ReadString(player, "PlayerId", "Id", "PlayerID", "AccountId");
                    var playerName = ReadString(player, "Name", "PlayerName", "DisplayName", "Nickname", "AccountName");
                    var isLocal = !string.IsNullOrWhiteSpace(localPlayerId)
                        && string.Equals(playerId, localPlayerId, System.StringComparison.OrdinalIgnoreCase);
                    if (!isLocal && string.IsNullOrWhiteSpace(playerId))
                    {
                        isLocal = string.Equals(playerName, localPlayerName, System.StringComparison.OrdinalIgnoreCase);
                    }

                    if (!isLocal)
                    {
                        continue;
                    }

                    historyKills = ReadInt(player, 0, "Kills", "KillCount", "TotalKill");
                    historyDeaths = ReadInt(player, 0, "Deaths", "DeathCount");
                    historyScore = ReadInt(player, historyKills, "Score", "TotalScore", "Points");
                    break;
                }

                if (MatchHistoryManager.Instance != null)
                {
                    MatchHistoryManager.Instance.RecordMatch(
                        localPlayerName, gameMode, result, historyScore, historyKills, historyDeaths);
                }
                hasRecordedHistory = true;
            }

            if (!string.IsNullOrWhiteSpace(winnerName))
            {
                Debug.Log($"[OnlineResultScene] Winner={winnerName} ({winningPlayerId}), LocalPlayer={localPlayerId}");
            }

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

            ApplyReportedProgress(json);
        }

        /// <summary>
        /// Takes the experience a finished match earned.
        /// <para>
        /// The server sends this per player on the result, and nothing on this side
        /// read it: a player could win a match, go up a level, and the profile
        /// still said the level they started on. The total is adopted rather than
        /// the gain added, so a result delivered twice is not credited twice.
        /// </para>
        /// </summary>
        private void ApplyReportedProgress(JObject json)
        {
            if (json == null || json["XpGained"] == null && json["NewLevel"] == null)
            {
                return;
            }

            var newLevel = json["NewLevel"]?.ToObject<int>() ?? 0;
            if (newLevel <= 0)
            {
                // A result that carries no level is not a progress report, and
                // reading the level off the login would reset a profile that has
                // since been told a different one.
                return;
            }

            AccountManager.Instance?.ApplyMatchProgress(
                newLevel,
                json["TotalExp"]?.ToObject<long>() ?? 0L,
                json["XpGained"]?.ToObject<long>() ?? 0L);
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

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            if (profile != null && !string.IsNullOrWhiteSpace(profile.GlobalUserId))
            {
                return profile.GlobalUserId;
            }

            return "Spectator";
        }

        private static bool IsPlayerResultToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return !string.Equals(value, "Draw", System.StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "None", System.StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "NoPlayers", System.StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Spectator", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTeamResultToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return string.Equals(value, "Red", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Blue", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Green", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Yellow", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
