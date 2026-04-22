using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[FriendManager] プレイヤーIDまたは名前が空です");
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

            OnFriendAdded?.Invoke(friend);
            OnFriendListUpdated?.Invoke(GetFriends());

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
            var friend = friendData.Friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend == null)
            {
                Debug.LogWarning($"[FriendManager] フレンドが見つかりません: {playerId}");
                return false;
            }

            friendData.Friends.Remove(friend);
            SaveFriendData();

            OnFriendRemoved?.Invoke(friend);
            OnFriendListUpdated?.Invoke(GetFriends());

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
            if (string.IsNullOrEmpty(targetPlayerId))
            {
                Debug.LogWarning("[FriendManager] 対象プレイヤーIDが空です");
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

            OnFriendRequestReceived?.Invoke(request);

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
            var request = friendData.PendingRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null)
            {
                Debug.LogWarning($"[FriendManager] 申請が見つかりません: {requestId}");
                return false;
            }

            // フレンドとして追加
            AddFriend(request.TargetPlayerId, request.TargetPlayerName);

            // 申請を削除
            friendData.PendingRequests.Remove(request);
            SaveFriendData();

            OnFriendRequestAccepted?.Invoke(request);

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
            var request = friendData.PendingRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null)
            {
                Debug.LogWarning($"[FriendManager] 申請が見つかりません: {requestId}");
                return false;
            }

            friendData.PendingRequests.Remove(request);
            SaveFriendData();

            OnFriendRequestRejected?.Invoke(request);

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
            return friendData.Friends.Any(f => f.PlayerId == playerId);
        }

        /// <summary>
        /// 保留中の申請があるか確認する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>保留中の申請があるかどうか</returns>
        public bool HasPendingRequest(string playerId)
        {
            return friendData.PendingRequests.Any(r => r.TargetPlayerId == playerId);
        }

        /// <summary>
        /// フレンドのオンライン状態を更新する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="isOnline">オンライン状態</param>
        public void UpdateFriendOnlineStatus(string playerId, bool isOnline)
        {
            var friend = friendData.Friends.FirstOrDefault(f => f.PlayerId == playerId);
            if (friend == null) return;

            friend.IsOnline = isOnline;
            friend.LastOnlineDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveFriendData();

            OnFriendOnlineStatusChanged?.Invoke(playerId, isOnline);
            OnFriendListUpdated?.Invoke(GetFriends());

            Debug.Log($"[FriendManager] {friend.PlayerName}のオンライン状態を更新しました: {isOnline}");
        }

        /// <summary>
        /// ブロックリストに追加する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="playerName">プレイヤー名</param>
        public void BlockPlayer(string playerId, string playerName)
        {
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
            return friendData.BlockedPlayers.Any(b => b.PlayerId == playerId);
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
                    friendData = importedData;
                    SaveFriendData();
                    OnFriendListUpdated?.Invoke(GetFriends());
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