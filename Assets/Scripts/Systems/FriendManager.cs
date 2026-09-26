using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    /// <summary>
    /// フレンドマネージャー
    /// フレンド管理機能を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class FriendManager : MonoBehaviour
    {
        // ─── シングルトン ───────────────────────────────────────────

        private static FriendManager _instance;
        public static FriendManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("FriendManager");
                    _instance = go.AddComponent<FriendManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // ─── 定数 ─────────────────────────────────────────────────

        private const string FRIEND_SAVE_KEY = "FriendData";
        private const int MAX_FRIENDS = 100;
        private const int MAX_PENDING_REQUESTS = 50;

        // ─── 内部状態 ───────────────────────────────────────────────

        private FriendData friendData = new FriendData();
        private bool isInitialized = false;

        // ─── イベント ───────────────────────────────────────────────

        public event Action<List<FriendEntry>> OnFriendListUpdated;
        public event Action<FriendEntry> OnFriendAdded;
        public event Action<FriendEntry> OnFriendRemoved;
        public event Action<FriendRequest> OnFriendRequestReceived;
        public event Action<FriendRequest> OnFriendRequestAccepted;
        public event Action<FriendRequest> OnFriendRequestRejected;
        public event Action<string, bool> OnFriendOnlineStatusChanged;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// フレンドシステムを初期化する
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            LoadFriendData();
            isInitialized = true;

            Debug.Log("[FriendManager] 初期化完了");
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// フレンドを追加する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <returns>追加成功かどうか</returns>
        public bool AddFriend(string playerId, string playerName)
        {
            playerId = playerId?.Trim();
            playerName = playerName?.Trim();
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[FriendManager] プレイヤーIDまたは名前が空です");
                return false;
            }

            if (IsBlocked(playerId))
            {
                Debug.LogWarning($"[FriendManager] ブロック中のプレイヤーは追加できません: {playerId}");
                return false;
            }

            // 既にフレンドか確認
            if (IsFriend(playerId))
            {
                Debug.LogWarning($"[FriendManager] {playerName}は既にフレンドです");
                return false;
            }

            // 最大フレンド数チェック
            if (friendData.Friends.Count >= MAX_FRIENDS)
            {
                Debug.LogWarning("[FriendManager] フレンド数が上限に達しています");
                return false;
            }

            var friend = new FriendEntry
            {
                PlayerId = playerId,
                PlayerName = playerName,
                AddedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsOnline = false,
                LastOnlineDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            friendData.Friends.Add(friend);
            SaveFriendData();

            InvokeSafely(OnFriendAdded, friend, nameof(OnFriendAdded));
            InvokeSafely(OnFriendListUpdated, GetFriends(), nameof(OnFriendListUpdated));

            Debug.Log($"[FriendManager] フレンドを追加しました: {playerName}");
            return true;
        }

        /// <summary>
        /// フレンドを削除する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>削除成功かどうか</returns>
        public bool RemoveFriend(string playerId)
        {
            playerId = NormalizePlayerId(playerId);
            var friend = friendData.Friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend == null)
            {
                Debug.LogWarning($"[FriendManager] フレンドが見つかりません: {playerId}");
                return false;
            }

            friendData.Friends.Remove(friend);
            SaveFriendData();

            InvokeSafely(OnFriendRemoved, friend, nameof(OnFriendRemoved));
            InvokeSafely(OnFriendListUpdated, GetFriends(), nameof(OnFriendListUpdated));

            Debug.Log($"[FriendManager] フレンドを削除しました: {friend.PlayerName}");
            return true;
        }

        /// <summary>
        /// フレンド申請を送信する
        /// </summary>
        /// <param name="targetPlayerId">対象プレイヤーID</param>
        /// <param name="targetPlayerName">対象プレイヤー名</param>
        /// <param name="senderPlayerName">送信者名</param>
        /// <returns>送信成功かどうか</returns>
        public bool SendFriendRequest(string targetPlayerId, string targetPlayerName, string senderPlayerName)
        {
            targetPlayerId = targetPlayerId?.Trim();
            targetPlayerName = targetPlayerName?.Trim();
            senderPlayerName = senderPlayerName?.Trim();
            if (string.IsNullOrEmpty(targetPlayerId))
            {
                Debug.LogWarning("[FriendManager] 対象プレイヤーIDが空です");
                return false;
            }

            if (IsBlocked(targetPlayerId))
            {
                Debug.LogWarning($"[FriendManager] ブロック中のプレイヤーには申請できません: {targetPlayerId}");
                return false;
            }

            // 既にフレンドか確認
            if (IsFriend(targetPlayerId))
            {
                Debug.LogWarning($"[FriendManager] {targetPlayerName}は既にフレンドです");
                return false;
            }

            // 既に申請済みか確認
            if (HasPendingRequest(targetPlayerId))
            {
                Debug.LogWarning($"[FriendManager] {targetPlayerName}には既に申請を送信済みです");
                return false;
            }

            // 保留中の申請数チェック
            if (friendData.PendingRequests.Count >= MAX_PENDING_REQUESTS)
            {
                Debug.LogWarning("[FriendManager] 保留中の申請数が上限に達しています");
                return false;
            }

            var request = new FriendRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                TargetPlayerId = targetPlayerId,
                TargetPlayerName = targetPlayerName,
                SenderPlayerName = senderPlayerName,
                RequestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "Pending"
            };

            friendData.PendingRequests.Add(request);
            SaveFriendData();

            InvokeSafely(OnFriendRequestReceived, request, nameof(OnFriendRequestReceived));

            Debug.Log($"[FriendManager] フレンド申請を送信しました: {targetPlayerName}");
            return true;
        }

        /// <summary>
        /// フレンド申請を承認する
        /// </summary>
        /// <param name="requestId">申請ID</param>
        /// <returns>承認成功かどうか</returns>
        public bool AcceptFriendRequest(string requestId)
        {
            requestId = requestId?.Trim();
            var request = friendData.PendingRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null)
            {
                Debug.LogWarning($"[FriendManager] 申請が見つかりません: {requestId}");
                return false;
            }

            // フレンドとして追加。上限到達などで失敗した場合は申請を残す。
            if (!AddFriend(request.TargetPlayerId, request.TargetPlayerName))
            {
                return false;
            }

            // 申請を削除
            friendData.PendingRequests.Remove(request);
            SaveFriendData();

            InvokeSafely(OnFriendRequestAccepted, request, nameof(OnFriendRequestAccepted));

            Debug.Log($"[FriendManager] フレンド申請を承認しました: {request.TargetPlayerName}");
            return true;
        }

        /// <summary>
        /// フレンド申請を拒否する
        /// </summary>
        /// <param name="requestId">申請ID</param>
        /// <returns>拒否成功かどうか</returns>
        public bool RejectFriendRequest(string requestId)
        {
            requestId = requestId?.Trim();
            var request = friendData.PendingRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null)
            {
                Debug.LogWarning($"[FriendManager] 申請が見つかりません: {requestId}");
                return false;
            }

            friendData.PendingRequests.Remove(request);
            SaveFriendData();

            InvokeSafely(OnFriendRequestRejected, request, nameof(OnFriendRequestRejected));

            Debug.Log($"[FriendManager] フレンド申請を拒否しました: {request.TargetPlayerName}");
            return true;
        }

        /// <summary>
        /// フレンドリストを取得する
        /// </summary>
        /// <param name="onlineOnly">オンラインのみかどうか</param>
        /// <returns>フレンドリスト</returns>
        public List<FriendEntry> GetFriends(bool onlineOnly = false)
        {
            if (onlineOnly)
            {
                return friendData.Friends.Where(f => f.IsOnline).ToList();
            }
            return new List<FriendEntry>(friendData.Friends);
        }

        /// <summary>
        /// 保留中のフレンド申請を取得する
        /// </summary>
        /// <returns>保留中の申請リスト</returns>
        public List<FriendRequest> GetPendingRequests()
        {
            return new List<FriendRequest>(friendData.PendingRequests);
        }

        /// <summary>
        /// フレンドかどうか確認する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>フレンドかどうか</returns>
        public bool IsFriend(string playerId)
        {
            playerId = NormalizePlayerId(playerId);
            return friendData.Friends.Any(f => f.PlayerId == playerId);
        }

        /// <summary>
        /// 保留中の申請があるか確認する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>保留中の申請があるかどうか</returns>
        public bool HasPendingRequest(string playerId)
        {
            playerId = NormalizePlayerId(playerId);
            return friendData.PendingRequests.Any(r => r.TargetPlayerId == playerId);
        }

        /// <summary>
        /// サーバーから受け取ったフレンド一覧をローカルキャッシュへ反映する。
        /// ローカル専用のブロックリストは維持する。
        /// </summary>
        public void ApplyServerFriendList(JObject response)
        {
            if (response == null || !ReadSuccess(response["Success"]))
            {
                return;
            }

            var friends = new List<FriendEntry>();
            var seenPlayerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (response["Friends"] is JArray friendArray)
            {
                foreach (var token in friendArray.OfType<JObject>())
                {
                    var playerId = token.Value<string>("PlayerID") ?? token.Value<string>("PlayerId");
                    playerId = playerId?.Trim();
                    if (string.IsNullOrEmpty(playerId) || !seenPlayerIds.Add(playerId))
                    {
                        continue;
                    }

                    friends.Add(new FriendEntry
                    {
                        PlayerId = playerId,
                        PlayerName = (token.Value<string>("PlayerName") ?? token.Value<string>("DisplayName") ?? playerId).Trim(),
                        IsOnline = ReadBool(token["IsOnline"]),
                        LastOnlineDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            friendData.Friends = friends
                .Where(friend => !IsBlocked(friend.PlayerId))
                .ToList();
            friendData.PendingRequests = ParseServerPendingRequests(response["PendingRequests"] as JArray)
                .Where(request => !IsBlocked(request.TargetPlayerId))
                .ToList();
            NormalizeFriendData();
            SaveFriendData();
            InvokeSafely(OnFriendListUpdated, GetFriends(), nameof(OnFriendListUpdated));
        }

        /// <summary>サーバーから届いた申請通知を保留一覧へ追加する。</summary>
        public void ApplyServerFriendRequestNotification(JObject notification)
        {
            if (notification == null || !ReadSuccess(notification["Success"]))
            {
                return;
            }

            var fromPlayerId = notification.Value<string>("FromPlayerID") ?? notification.Value<string>("FromPlayerId");
            fromPlayerId = fromPlayerId?.Trim();
            if (string.IsNullOrEmpty(fromPlayerId)
                || friendData.PendingRequests.Any(r => string.Equals(
                    NormalizePlayerId(r.TargetPlayerId),
                    fromPlayerId,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var request = new FriendRequest
            {
                RequestId = fromPlayerId,
                TargetPlayerId = fromPlayerId,
                TargetPlayerName = (notification.Value<string>("FromPlayerName") ?? fromPlayerId).Trim(),
                SenderPlayerName = (notification.Value<string>("FromPlayerName") ?? fromPlayerId).Trim(),
                RequestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "Pending"
            };
            friendData.PendingRequests.Add(request);
            SaveFriendData();
            InvokeSafely(OnFriendRequestReceived, request, nameof(OnFriendRequestReceived));
        }

        private static List<FriendRequest> ParseServerPendingRequests(JArray pendingRequests)
        {
            var result = new List<FriendRequest>();
            var seenPlayerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (pendingRequests == null)
            {
                return result;
            }

            foreach (var token in pendingRequests.OfType<JObject>())
            {
                var fromPlayerId = token.Value<string>("FromPlayerID") ?? token.Value<string>("FromPlayerId");
                fromPlayerId = fromPlayerId?.Trim();
                if (string.IsNullOrEmpty(fromPlayerId) || !seenPlayerIds.Add(fromPlayerId))
                {
                    continue;
                }

                result.Add(new FriendRequest
                {
                    RequestId = fromPlayerId,
                    TargetPlayerId = fromPlayerId,
                    TargetPlayerName = token.Value<string>("FromPlayerName") ?? fromPlayerId,
                    SenderPlayerName = token.Value<string>("FromPlayerName") ?? fromPlayerId,
                    RequestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "Pending"
                });
            }
            return result;
        }

        private static bool ReadSuccess(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return true;
            }

            try
            {
                return token.ToObject<bool>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FriendManager] Invalid success value: {ex.Message}");
                return false;
            }
        }

        private static bool ReadBool(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }

            try
            {
                return token.ToObject<bool>();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// フレンドのオンライン状態を更新する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="isOnline">オンライン状態</param>
        public void UpdateFriendOnlineStatus(string playerId, bool isOnline)
        {
            playerId = NormalizePlayerId(playerId);
            var friend = friendData.Friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend == null) return;

            friend.IsOnline = isOnline;
            friend.LastOnlineDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveFriendData();

            InvokeSafely(OnFriendOnlineStatusChanged, playerId, isOnline, nameof(OnFriendOnlineStatusChanged));
            InvokeSafely(OnFriendListUpdated, GetFriends(), nameof(OnFriendListUpdated));

            Debug.Log($"[FriendManager] {friend.PlayerName}のオンライン状態を更新しました: {isOnline}");
        }

        /// <summary>
        /// ブロックリストに追加する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="playerName">プレイヤー名</param>
        public void BlockPlayer(string playerId, string playerName)
        {
            playerId = NormalizePlayerId(playerId);
            playerName = playerName?.Trim();
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(playerName))
            {
                return;
            }

            if (!friendData.BlockedPlayers.Any(b => b.PlayerId == playerId))
            {
                friendData.BlockedPlayers.Add(new BlockedPlayer
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    BlockedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                SaveFriendData();

                Debug.Log($"[FriendManager] プレイヤーをブロックしました: {playerName}");
            }
        }

        /// <summary>
        /// ブロックリストから削除する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        public void UnblockPlayer(string playerId)
        {
            playerId = NormalizePlayerId(playerId);
            var blocked = friendData.BlockedPlayers.FirstOrDefault(b => b.PlayerId == playerId);
            if (blocked != null)
            {
                friendData.BlockedPlayers.Remove(blocked);
                SaveFriendData();

                Debug.Log($"[FriendManager] プレイヤーのブロックを解除しました: {blocked.PlayerName}");
            }
        }

        /// <summary>
        /// ブロックされているか確認する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>ブロックされているかどうか</returns>
        public bool IsBlocked(string playerId)
        {
            playerId = NormalizePlayerId(playerId);
            return friendData.BlockedPlayers.Any(b => b.PlayerId == playerId);
        }

        private static string NormalizePlayerId(string playerId)
        {
            return playerId?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// フレンドデータをエクスポートする
        /// </summary>
        /// <returns>JSON形式のフレンドデータ</returns>
        public string ExportFriendData()
        {
            return JsonConvert.SerializeObject(friendData, Formatting.Indented);
        }

        /// <summary>
        /// フレンドデータをインポートする
        /// </summary>
        /// <param name="json">JSON形式のフレンドデータ</param>
        public void ImportFriendData(string json)
        {
            try
            {
                var importedData = JsonConvert.DeserializeObject<FriendData>(json);
                if (importedData != null)
                {
                    importedData.Friends ??= new List<FriendEntry>();
                    importedData.PendingRequests ??= new List<FriendRequest>();
                    importedData.BlockedPlayers ??= new List<BlockedPlayer>();
                    friendData = importedData;
                    NormalizeFriendData();
                    SaveFriendData();
                    InvokeSafely(OnFriendListUpdated, GetFriends(), nameof(OnFriendListUpdated));
                    Debug.Log("[FriendManager] フレンドデータをインポートしました");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] インポートエラー: {ex.Message}");
            }
        }

        // ─── プライベートメソッド ─────────────────────────────────────

        /// <summary>
        /// フレンドデータを読み込む
        /// </summary>
        private void LoadFriendData()
        {
            var json = PlayerPrefs.GetString(FRIEND_SAVE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    friendData = JsonConvert.DeserializeObject<FriendData>(json) ?? new FriendData();
                    friendData.Friends ??= new List<FriendEntry>();
                    friendData.PendingRequests ??= new List<FriendRequest>();
                    friendData.BlockedPlayers ??= new List<BlockedPlayer>();
                    NormalizeFriendData();
                    Debug.Log($"[FriendManager] フレンドデータを読み込みました: {friendData.Friends.Count}人");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FriendManager] 読み込みエラー: {ex.Message}");
                    friendData = new FriendData();
                }
            }
            else
            {
                friendData = new FriendData();
            }
        }

        private void NormalizeFriendData()
        {
            friendData ??= new FriendData();
            friendData.Friends ??= new List<FriendEntry>();
            friendData.PendingRequests ??= new List<FriendRequest>();
            friendData.BlockedPlayers ??= new List<BlockedPlayer>();

            var seenFriends = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            friendData.Friends = friendData.Friends
                .Where(friend => friend != null
                    && !string.IsNullOrWhiteSpace(friend.PlayerId)
                    && seenFriends.Add(friend.PlayerId.Trim()))
                .Take(MAX_FRIENDS)
                .ToList();

            foreach (var friend in friendData.Friends)
            {
                friend.PlayerId = friend.PlayerId.Trim();
                friend.PlayerName = string.IsNullOrWhiteSpace(friend.PlayerName)
                    ? friend.PlayerId
                    : friend.PlayerName.Trim();
            }

            var seenRequests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            friendData.PendingRequests = friendData.PendingRequests
                .Where(request => request != null
                    && !string.IsNullOrWhiteSpace(request.TargetPlayerId)
                    && seenRequests.Add(request.TargetPlayerId.Trim()))
                .Take(MAX_PENDING_REQUESTS)
                .ToList();

            foreach (var request in friendData.PendingRequests)
            {
                request.TargetPlayerId = request.TargetPlayerId.Trim();
                request.RequestId = string.IsNullOrWhiteSpace(request.RequestId)
                    ? request.TargetPlayerId
                    : request.RequestId.Trim();
                request.TargetPlayerName = string.IsNullOrWhiteSpace(request.TargetPlayerName)
                    ? request.TargetPlayerId
                    : request.TargetPlayerName.Trim();
            }

            var seenBlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            friendData.BlockedPlayers = friendData.BlockedPlayers
                .Where(blocked => blocked != null
                    && !string.IsNullOrWhiteSpace(blocked.PlayerId)
                    && seenBlocked.Add(blocked.PlayerId.Trim()))
                .ToList();

            foreach (var blocked in friendData.BlockedPlayers)
            {
                blocked.PlayerId = blocked.PlayerId.Trim();
                blocked.PlayerName = string.IsNullOrWhiteSpace(blocked.PlayerName)
                    ? blocked.PlayerId
                    : blocked.PlayerName.Trim();
            }
        }

        private static void InvokeSafely<T>(Action<T> handlers, T value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FriendManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<string, bool> handlers, string playerId, bool isOnline, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string, bool> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(playerId, isOnline);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FriendManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        /// <summary>
        /// フレンドデータを保存する
        /// </summary>
        private void SaveFriendData()
        {
            try
            {
                var json = JsonConvert.SerializeObject(friendData);
                PlayerPrefs.SetString(FRIEND_SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log($"[FriendManager] フレンドデータを保存しました: {friendData.Friends.Count}人");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 保存エラー: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// フレンドデータクラス
    /// </summary>
    [Serializable]
    public class FriendData
    {
        public List<FriendEntry> Friends = new List<FriendEntry>();
        public List<FriendRequest> PendingRequests = new List<FriendRequest>();
        public List<BlockedPlayer> BlockedPlayers = new List<BlockedPlayer>();
    }

    /// <summary>
    /// フレンドエントリークラス
    /// </summary>
    [Serializable]
    public class FriendEntry
    {
        public string PlayerId;
        public string PlayerName;
        public string AddedDate;
        public bool IsOnline;
        public string LastOnlineDate;
        public string StatusMessage;
    }

    /// <summary>
    /// フレンド申請クラス
    /// </summary>
    [Serializable]
    public class FriendRequest
    {
        public string RequestId;
        public string TargetPlayerId;
        public string TargetPlayerName;
        public string SenderPlayerName;
        public string RequestDate;
        public string Status; // Pending, Accepted, Rejected
    }

    /// <summary>
    /// ブロックプレイヤークラス
    /// </summary>
    [Serializable]
    public class BlockedPlayer
    {
        public string PlayerId;
        public string PlayerName;
        public string BlockedDate;
    }
}
