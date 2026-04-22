using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OpenGS
{
    /// <summary>
    /// マッチ履歴マネージャー
    /// マッチ結果の記録と履歴管理を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class MatchHistoryManager : MonoBehaviour
    {
        // ─── シングルトン ───────────────────────────────────────────

        private static MatchHistoryManager _instance;
        public static MatchHistoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MatchHistoryManager");
                    _instance = go.AddComponent<MatchHistoryManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // ─── 定数 ─────────────────────────────────────────────────

        private const string HISTORY_SAVE_KEY = "MatchHistoryData";
        private const int MAX_HISTORY_ENTRIES = 100;
        private const int DISPLAY_HISTORY_COUNT = 20;

        // ─── 内部状態 ───────────────────────────────────────────────

        private MatchHistoryData historyData = new MatchHistoryData();
        private bool isInitialized = false;

        // ─── イベント ───────────────────────────────────────────────

        public event Action<List<MatchHistoryEntry>> OnHistoryUpdated;
        public event Action<MatchHistoryEntry> OnMatchRecorded;

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
        /// マッチ履歴システムを初期化する
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            LoadHistoryData();
            isInitialized = true;

            Debug.Log("[MatchHistoryManager] 初期化完了");
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// マッチ結果を記録する
        /// </summary>
        /// <param name="entry">マッチ履歴エントリー</param>
        public void RecordMatch(MatchHistoryEntry entry)
        {
            if (entry == null)
            {
                Debug.LogWarning("[MatchHistoryManager] エントリーがnullです");
                return;
            }

            // タイムスタンプが設定されていない場合は現在時刻を設定
            if (string.IsNullOrEmpty(entry.Timestamp))
            {
                entry.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // 履歴に追加
            historyData.Entries.Insert(0, entry);

            // 最大件数を超えた場合、古いエントリーを削除
            if (historyData.Entries.Count > MAX_HISTORY_ENTRIES)
            {
                historyData.Entries = historyData.Entries.Take(MAX_HISTORY_ENTRIES).ToList();
            }

            // 保存
            SaveHistoryData();

            // イベント発火
            OnMatchRecorded?.Invoke(entry);
            OnHistoryUpdated?.Invoke(GetRecentHistory(DISPLAY_HISTORY_COUNT));

            Debug.Log($"[MatchHistoryManager] マッチ結果を記録しました: {entry.GameMode} - {entry.Result}");
        }

        /// <summary>
        /// マッチ結果を記録する（簡易版）
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="result">結果（Win/Lose/Draw）</param>
        /// <param name="score">スコア</param>
        /// <param name="kills">キル数</param>
        /// <param name="deaths">デス数</param>
        /// <param name="mapName">マップ名</param>
        public void RecordMatch(
            string playerName,
            string gameMode,
            string result,
            int score,
            int kills = 0,
            int deaths = 0,
            string mapName = "")
        {
            var entry = new MatchHistoryEntry
            {
                PlayerName = playerName,
                GameMode = gameMode,
                Result = result,
                Score = score,
                Kills = kills,
                Deaths = deaths,
                MapName = mapName,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            RecordMatch(entry);
        }

        /// <summary>
        /// 最近の履歴を取得する
        /// </summary>
        /// <param name="count">取得件数</param>
        /// <returns>履歴リスト</returns>
        public List<MatchHistoryEntry> GetRecentHistory(int count = DISPLAY_HISTORY_COUNT)
        {
            return historyData.Entries.Take(count).ToList();
        }

        /// <summary>
        /// プレイヤーの履歴を取得する
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="count">取得件数</param>
        /// <returns>履歴リスト</returns>
        public List<MatchHistoryEntry> GetPlayerHistory(string playerName, int count = DISPLAY_HISTORY_COUNT)
        {
            return historyData.Entries
                .Where(e => e.PlayerName == playerName)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// ゲームモード別の履歴を取得する
        /// </summary>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="count">取得件数</param>
        /// <returns>履歴リスト</returns>
        public List<MatchHistoryEntry> GetHistoryByGameMode(string gameMode, int count = DISPLAY_HISTORY_COUNT)
        {
            return historyData.Entries
                .Where(e => e.GameMode == gameMode)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 勝敗統計を取得する
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="gameMode">ゲームモード（nullの場合は全モード）</param>
        /// <returns>勝敗統計</returns>
        public MatchStatistics GetStatistics(string playerName, string gameMode = null)
        {
            var entries = string.IsNullOrEmpty(gameMode)
                ? historyData.Entries.Where(e => e.PlayerName == playerName)
                : historyData.Entries.Where(e => e.PlayerName == playerName && e.GameMode == gameMode);

            var entriesList = entries.ToList();

            var stats = new MatchStatistics
            {
                TotalMatches = entriesList.Count,
                Wins = entriesList.Count(e => e.Result == "Win"),
                Losses = entriesList.Count(e => e.Result == "Lose"),
                Draws = entriesList.Count(e => e.Result == "Draw"),
                TotalKills = entriesList.Sum(e => e.Kills),
                TotalDeaths = entriesList.Sum(e => e.Deaths),
                TotalScore = entriesList.Sum(e => e.Score),
                AverageScore = entriesList.Any() ? entriesList.Average(e => e.Score) : 0,
                WinRate = entriesList.Count > 0 
                    ? (float)entriesList.Count(e => e.Result == "Win") / entriesList.Count * 100 
                    : 0
            };

            return stats;
        }

        /// <summary>
        /// 履歴をクリアする
        /// </summary>
        /// <param name="playerName">プレイヤー名（nullの場合は全プレイヤー）</param>
        public void ClearHistory(string playerName = null)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                historyData.Entries.Clear();
                Debug.Log("[MatchHistoryManager] 全履歴をクリアしました");
            }
            else
            {
                historyData.Entries.RemoveAll(e => e.PlayerName == playerName);
                Debug.Log($"[MatchHistoryManager] {playerName}の履歴をクリアしました");
            }

            SaveHistoryData();
            OnHistoryUpdated?.Invoke(GetRecentHistory(DISPLAY_HISTORY_COUNT));
        }

        /// <summary>
        /// 履歴データをエクスポートする
        /// </summary>
        /// <returns>JSON形式の履歴データ</returns>
        public string ExportHistoryData()
        {
            return JsonConvert.SerializeObject(historyData, Formatting.Indented);
        }

        /// <summary>
        /// 履歴データをインポートする
        /// </summary>
        /// <param name="json">JSON形式の履歴データ</param>
        public void ImportHistoryData(string json)
        {
            try
            {
                var importedData = JsonConvert.DeserializeObject<MatchHistoryData>(json);
                if (importedData != null && importedData.Entries != null)
                {
                    historyData = importedData;
                    SaveHistoryData();
                    OnHistoryUpdated?.Invoke(GetRecentHistory(DISPLAY_HISTORY_COUNT));
                    Debug.Log("[MatchHistoryManager] 履歴データをインポートしました");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchHistoryManager] インポートエラー: {ex.Message}");
            }
        }

        // ─── プライベートメソッド ─────────────────────────────────────

        /// <summary>
        /// 履歴データを読み込む
        /// </summary>
        private void LoadHistoryData()
        {
            var json = PlayerPrefs.GetString(HISTORY_SAVE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    historyData = JsonConvert.DeserializeObject<MatchHistoryData>(json) ?? new MatchHistoryData();
                    Debug.Log($"[MatchHistoryManager] 履歴データを読み込みました: {historyData.Entries.Count}件");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MatchHistoryManager] 読み込みエラー: {ex.Message}");
                    historyData = new MatchHistoryData();
                }
            }
            else
            {
                historyData = new MatchHistoryData();
            }
        }

        /// <summary>
        /// 履歴データを保存する
        /// </summary>
        private void SaveHistoryData()
        {
            try
            {
                var json = JsonConvert.SerializeObject(historyData);
                PlayerPrefs.SetString(HISTORY_SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log($"[MatchHistoryManager] 履歴データを保存しました: {historyData.Entries.Count}件");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchHistoryManager] 保存エラー: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// マッチ履歴データクラス
    /// </summary>
    [Serializable]
    public class MatchHistoryData
    {
        public List<MatchHistoryEntry> Entries = new List<MatchHistoryEntry>();
    }

    /// <summary>
    /// マッチ履歴エントリークラス
    /// </summary>
    [Serializable]
    public class MatchHistoryEntry
    {
        public string PlayerName;
        public string GameMode;
        public string Result; // Win, Lose, Draw
        public int Score;
        public int Kills;
        public int Deaths;
        public string MapName;
        public string Timestamp;
        public Dictionary<string, object> AdditionalData;
    }

    /// <summary>
    /// マッチ統計クラス
    /// </summary>
    [Serializable]
    public class MatchStatistics
    {
        public int TotalMatches;
        public int Wins;
        public int Losses;
        public int Draws;
        public int TotalKills;
        public int TotalDeaths;
        public int TotalScore;
        public float AverageScore;
        public float WinRate;
    }
}