using System;
using UnityEngine;
using UnityEngine.Events;

namespace OpenGS
{
    /// <summary>
    /// マッチタイマー管理クラス
    /// サーバーからの時間同期イベントを受け取り、ローカル時間を同期する
    /// </summary>
    [DisallowMultipleComponent]
    public class MatchTimer : MonoBehaviour
    {
        [Header("Timer Settings")]
        [SerializeField] private float matchDuration = 600f; // デフォルト10分

        [Header("Sync Settings")]
        [SerializeField] private bool useServerTime = true; // サーバー時間を使用するか
        [SerializeField] private float syncInterval = 1f; // サーバーと同期する間隔（秒）

        [Header("Events")]
        public UnityEvent timeupEvent;
        public UnityEvent<float> onTimeUpdated; // 時間更新イベント（現在の残り時間）

        // ローカル時間管理
        private float localRemainingTime = 0f;
        private bool isStart = false;

        // サーバー同期用
        private float lastServerTime = 0f;
        private int serverRemainingTime = 0;
        private float lastSyncTime = 0f;
        private bool receivedServerTime = false;

        // Ping同期用（オプション）
        private float pingOffset = 0f; // サーバーとクライアントの時間差

        // 後方互換性プロパティ
        /// <summary>
        /// 後方互換性のため残しています。localRemainingTime を使用してしてください。
        /// </summary>
        public float time
        {
            get => localRemainingTime;
            set => localRemainingTime = value;
        }

        private void Start()
        {
            localRemainingTime = matchDuration;
        }

        private void Update()
        {
            if (isStart)
            {
                // ローカルのdeltaTimeを使用
                float delta = Time.deltaTime;

                if (useServerTime && receivedServerTime)
                {
                    // サーバー時間を使用する場合：サーバーから受け取った時間から経過を引き算
                    // サーバーと同期してからの経過時間を計算
                    float elapsedSinceSync = Time.time - lastSyncTime;
                    localRemainingTime = serverRemainingTime - pingOffset - elapsedSinceSync;
                }
                else
                {
                    // オフラインまたはサーバー時間未受信の場合：ローカルでカウントダウン
                    localRemainingTime -= delta;
                }

                // 時間が0以下になった場合
                if (localRemainingTime <= 0f)
                {
                    localRemainingTime = 0f;
                    TimeUp();
                }

                // 時間更新イベント
                onTimeUpdated?.Invoke(localRemainingTime);
            }
        }

        /// <summary>
        /// サーバーから時間同期イベントを受け取る
        /// </summary>
        /// <param name="remainingTime">サーバーの残り時間（秒）</param>
        /// <param name="serverTimestamp">サーバーのタイムスタンプ</param>
        public void SyncServerTime(int remainingTime, long serverTimestamp)
        {
            serverRemainingTime = remainingTime;
            receivedServerTime = true;
            lastSyncTime = Time.time;

            // 最初の同期の場合、ローカル時間をサーバー時間に合わせる
            if (!isStart || localRemainingTime <= 0)
            {
                localRemainingTime = remainingTime;
            }

            Debug.Log($"[MatchTimer] Server time synced: {remainingTime}s, local: {localRemainingTime}s");
        }

        /// <summary>
        /// サーバーからGameStateSyncメッセージを受け取る（简便方法）
        /// </summary>
        /// <param name="remainingTime">サーバーの残り時間</param>
        /// <param name="scoreData">スコアデータ（今回は無視）</param>
        public void HandleGameStateSync(int remainingTime, object scoreData)
        {
            SyncServerTime(remainingTime, 0);
        }

        /// <summary>
        /// タイマーを設定
        /// </summary>
        public void SetTime(float t)
        {
            matchDuration = t;
            localRemainingTime = t;
        }

        /// <summary>
        /// タイマーを開始
        /// </summary>
        public void StartTimer()
        {
            if (localRemainingTime <= 0f)
            {
                Debug.LogWarning("[MatchTimer] Time is 0, cannot start timer");
            }
            else
            {
                isStart = true;
                Debug.Log($"[MatchTimer] Timer started: {localRemainingTime}s");
            }
        }

        /// <summary>
        /// タイマーを停止
        /// </summary>
        public void StopTimer()
        {
            isStart = false;
        }

        /// <summary>
        /// タイマーを再開
        /// </summary>
        public void ResumeTimer()
        {
            if (localRemainingTime > 0)
            {
                isStart = true;
            }
        }

        /// <summary>
        /// 現在の残り時間を取得
        /// </summary>
        public float GetRemainingTime()
        {
            return Mathf.Max(0, localRemainingTime);
        }

        /// <summary>
        /// タイマーが動作中か
        /// </summary>
        public bool IsRunning()
        {
            return isStart;
        }

        /// <summary>
        /// サーバー時間を使用するかを設定
        /// </summary>
        public void SetUseServerTime(bool useServer)
        {
            useServerTime = useServer;
        }

        /// <summary>
        /// 同期間隔を設定
        /// </summary>
        public void SetSyncInterval(float interval)
        {
            syncInterval = Mathf.Max(0.1f, interval);
        }

        /// <summary>
        /// Pingオフセットを設定（クライアント-サーバー間の遅延補正）
        /// </summary>
        public void SetPingOffset(float offset)
        {
            pingOffset = offset;
        }

        /// <summary>
        /// マッチ時間全体を設定
        /// </summary>
        public void SetMatchDuration(float duration)
        {
            matchDuration = duration;
            localRemainingTime = duration;
        }

        private void TimeUp()
        {
            isStart = false;
            localRemainingTime = 0f;

            timeupEvent.Invoke();
            Debug.Log("[MatchTimer] Time up!");
        }

        /// <summary>
        /// テスト用：タイマーをリセット
        /// </summary>
        public void ResetTimer()
        {
            isStart = false;
            localRemainingTime = matchDuration;
            receivedServerTime = false;
        }
    }
}
