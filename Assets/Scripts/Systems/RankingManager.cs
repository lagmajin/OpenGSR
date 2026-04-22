using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OpenGS
{
    /// <summary>
    /// ランキングマネージャー
    /// プレイヤーのスコア管理とランキング機能を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class RankingManager : MonoBehaviour
    {
        // ─── シングルトン ───────────────────────────────────────────

        private static RankingManager _instance;
        public static RankingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("RankingManager");
                    _instance = go.AddComponent<RankingManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // ─── 定数 ─────────────────────────────────────────────────

        private const string RANKING_SAVE_KEY = "RankingData";
        private const int MAX_RANKING_ENTRIES = 100;
        private const int DISPLAY_RANKING_COUNT = 10;

        // ─── 内部状態 ───────────────────────────────────────────────

        private RankingData rankingData = new RankingData();
        private bool isInitialized = false;

        // ─── イベント ───────────────────────────────────────────────

        public event Action<List<RankingEntry>> OnRankingUpdated;
        public event Action<RankingEntry> OnNewRecord;

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
        /// ランキングシステムを初期化する
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            LoadRankingData();
            isInitialized = true;

            Debug.Log("[RankingManager] 初期化完了");
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// スコアを登録する
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="score">スコア</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="additionalData">追加データ</param>
        /// <returns>新記録かどうか</returns>
        public bool RegisterScore(string playerName, int score, string gameMode = "Total", Dictionary<string, object> additionalData = null)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[RankingManager] プレイヤー名が空です");
                return false;
            }

            var entry = new RankingEntry
            {
                PlayerName = playerName,
                Score = score,
                GameMode = gameMode,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                AdditionalData = additionalData
            };

            // ランキングに追加
            bool isNewRecord = AddToRanking(entry);

            if (isNewRecord)
            {
                OnNewRecord?.Invoke(entry);
                Debug.Log($"[RankingManager] 新記録: {playerName} - {score} ({gameMode})");
            }

            // ランキングを保存
            SaveRankingData();

            // ランキング更新イベントを発火
            OnRankingUpdated?.Invoke(GetTopRanking(DISPLAY_RANKING_COUNT));

            return isNewRecord;
        }

        /// <summary>
        /// トップランキングを取得する
        /// </summary>
        /// <param name="count">取得件数</param>
        /// <param name="gameMode">ゲームモード（nullの場合は全モード）</param>
        /// <returns>ランキングリスト</returns>
        public List<RankingEntry> GetTopRanking(int count = DISPLAY_RANKING_COUNT, string gameMode = null)
        {
            var entries = string.IsNullOrEmpty(gameMode)
                ? rankingData.Entries
                : rankingData.Entries.Where(e => e.GameMode == gameMode).ToList();

            return entries
                .OrderByDescending(e => e.Score)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// プレイヤーのランキング順位を取得する
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <returns>順位（見つからない場合は-1）</returns>
        public int GetPlayerRank(string playerName, string gameMode = null)
        {
            var entries = string.IsNullOrEmpty(gameMode)
                ? rankingData.Entries
                : rankingData.Entries.Where(e => e.GameMode == gameMode).ToList();

            var sortedEntries = entries.OrderByDescending(e => e.Score).ToList();

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                if (sortedEntries[i].PlayerName == playerName)
                {
                    return i + 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// プレイヤーの最高スコアを取得する
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <returns>最高スコア</returns>
        public int GetPlayerHighScore(string playerName, string gameMode = null)
        {
            var entries = string.IsNullOrEmpty(gameMode)
                ? rankingData.Entries
                : rankingData.Entries.Where(e => e.GameMode == gameMode).ToList();

            var playerEntries = entries.Where(e => e.PlayerName == playerName).ToList();

            return playerEntries.Any() ? playerEntries.Max(e => e.Score) : 0;
        }

        /// <summary>
        /// ランキングをクリアする
        /// </summary>
        /// <param name="gameMode">ゲームモード（nullの場合は全モード）</param>
        public void ClearRanking(string gameMode = null)
        {
            if (string.IsNullOrEmpty(gameMode))
            {
                rankingData.Entries.Clear();
                Debug.Log("[RankingManager] 全ランキングをクリアしました");
            }
            else
            {
                rankingData.Entries.RemoveAll(e => e.GameMode == gameMode);
                Debug.Log($"[RankingManager] {gameMode}のランキングをクリアしました");
            }

            SaveRankingData();
            OnRankingUpdated?.Invoke(GetTopRanking(DISPLAY_RANKING_COUNT));
        }

        /// <summary>
        /// ランキングデータをエクスポートする
        /// </summary>
        /// <returns>JSON形式のランキングデータ</returns>
        public string ExportRankingData()
        {
            return JsonConvert.SerializeObject(rankingData, Formatting.Indented);
        }

        /// <summary>
        /// ランキングデータをインポートする
        /// </summary>
        /// <param name="json">JSON形式のランキングデータ</param>
        public void ImportRankingData(string json)
        {
            try
            {
                var importedData = JsonConvert.DeserializeObject<RankingData>(json);
                if (importedData != null && importedData.Entries != null)
                {
                    rankingData = importedData;
                    SaveRankingData();
                    OnRankingUpdated?.Invoke(GetTopRanking(DISPLAY_RANKING_COUNT));
                    Debug.Log("[RankingManager] ランキングデータをインポートしました");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RankingManager] インポートエラー: {ex.Message}");
            }
        }

        // ─── プライベートメソッド ─────────────────────────────────────

        /// <summary>
        /// ランキングにエントリーを追加する
        /// </summary>
        private bool AddToRanking(RankingEntry entry)
        {
            // 同じプレイヤーの既存エントリーを確認
            var existingEntry = rankingData.Entries
                .FirstOrDefault(e => e.PlayerName == entry.PlayerName && e.GameMode == entry.GameMode);

            if (existingEntry != null)
            {
                // 既存エントリーより高いスコアの場合のみ更新
                if (entry.Score > existingEntry.Score)
                {
                    existingEntry.Score = entry.Score;
                    existingEntry.Timestamp = entry.Timestamp;
                    existingEntry.AdditionalData = entry.AdditionalData;
                    return true;
                }
                return false;
            }
            else
            {
                // 新規エントリーを追加
                rankingData.Entries.Add(entry);

                // 最大件数を超えた場合、最低スコアを削除
                if (rankingData.Entries.Count > MAX_RANKING_ENTRIES)
                {
                    var lowestEntry = rankingData.Entries
                        .OrderBy(e => e.Score)
                        .First();
                    rankingData.Entries.Remove(lowestEntry);
                }

                return true;
            }
        }

        /// <summary>
        /// ランキングデータを読み込む
        /// </summary>
        private void LoadRankingData()
        {
            var json = PlayerPrefs.GetString(RANKING_SAVE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    rankingData = JsonConvert.DeserializeObject<RankingData>(json) ?? new RankingData();
                    Debug.Log($"[RankingManager] ランキングデータを読み込みました: {rankingData.Entries.Count}件");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RankingManager] 読み込みエラー: {ex.Message}");
                    rankingData = new RankingData();
                }
            }
            else
            {
                rankingData = new RankingData();
            }
        }

        /// <summary>
        /// ランキングデータを保存する
        /// </summary>
        private void SaveRankingData()
        {
            try
            {
                var json = JsonConvert.SerializeObject(rankingData);
                PlayerPrefs.SetString(RANKING_SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log($"[RankingManager] ランキングデータを保存しました: {rankingData.Entries.Count}件");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RankingManager] 保存エラー: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ランキングデータクラス
    /// </summary>
    [Serializable]
    public class RankingData
    {
        public List<RankingEntry> Entries = new List<RankingEntry>();
    }

    /// <summary>
    /// ランキングエントリークラス
    /// </summary>
    [Serializable]
    public class RankingEntry
    {
        public string PlayerName;
        public int Score;
        public string GameMode;
        public string Timestamp;
        public Dictionary<string, object> AdditionalData;
    }
}