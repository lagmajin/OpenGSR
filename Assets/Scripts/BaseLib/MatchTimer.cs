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
        private int serverRemainingTime = 0;
        private float lastSyncTime = 0f;
        private bool receivedServerTime = false;

        // Ping同期用（オプション）
        private float pingOffset = 0f; // サーバーとクライアントの時間差

        private void Awake()
        {
            matchDuration = float.IsFinite(matchDuration) ? Mathf.Max(0f, matchDuration) : 0f;
            syncInterval = float.IsFinite(syncInterval) ? Mathf.Max(0.1f, syncInterval) : 1f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(matchDuration) || matchDuration < 0f) matchDuration = 0f;
            if (!float.IsFinite(syncInterval) || syncInterval < 0.1f) syncInterval = 0.1f;
        }

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

        private void OnDisable()
        {
            isStart = false;
        }

        private void Update()
        {
            if (isStart)
            {
                // ローカルのdeltaTimeを使用
                float delta = Time.deltaTime;
                if (!float.IsFinite(delta) || delta < 0f) return;

                if (useServerTime && receivedServerTime)
                {
                    // サーバー時間を使用する場合：サーバーから受け取った時間から経過を引き算
                    // サーバーと同期してからの経過時間を計算
                    float elapsedSinceSync = Time.time - lastSyncTime;
                    if (!float.IsFinite(elapsedSinceSync) || elapsedSinceSync < 0f) elapsedSinceSync = 0f;
                    var syncedRemainingTime = serverRemainingTime - pingOffset - elapsedSinceSync;
                    localRemainingTime = float.IsFinite(syncedRemainingTime)
                        ? Mathf.Max(0f, syncedRemainingTime)
                        : 0f;
                }
                else
                {
                    // オフラインまたはサーバー時間未受信の場合：ローカルでカウントダウン
                    localRemainingTime -= Mathf.Min(delta, 0.1f);
                }

                if (!float.IsFinite(localRemainingTime))
                {
                    localRemainingTime = 0f;
                }

                // 時間が0以下になった場合
                if (localRemainingTime <= 0f)
                {
                    localRemainingTime = 0f;
                    TimeUp();
                }

                // 時間更新イベント
                InvokeSafely(onTimeUpdated, localRemainingTime, nameof(onTimeUpdated));
            }
        }

        private static void InvokeSafely(UnityEvent<float> handlers, float value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            try
            {
                handlers.Invoke(value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchTimer] {eventName} listener failed: {ex}");
            }
        }

        /// <summary>
        /// サーバーから時間同期イベントを受け取る
        /// </summary>
        /// <param name="remainingTime">サーバーの残り時間（秒）</param>
        /// <param name="serverTimestamp">サーバーのタイムスタンプ</param>
        public void SyncServerTime(int remainingTime, long serverTimestamp)
        {
            serverRemainingTime = Mathf.Max(0, remainingTime);
            if (!float.IsFinite(pingOffset)) pingOffset = 0f;
            receivedServerTime = true;
            var now = Time.time;
            lastSyncTime = float.IsFinite(now) && now >= 0f ? now : 0f;

            // 最初の同期の場合、ローカル時間をサーバー時間に合わせる
            if (!isStart || localRemainingTime <= 0)
            {
                localRemainingTime = serverRemainingTime;
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
            matchDuration = float.IsFinite(t) ? Mathf.Max(0f, t) : 0f;
            localRemainingTime = matchDuration;
            pingOffset = 0f;
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
            syncInterval = float.IsFinite(interval) ? Mathf.Max(0.1f, interval) : 1f;
        }

        /// <summary>
        /// Pingオフセットを設定（クライアント-サーバー間の遅延補正）
        /// </summary>
        public void SetPingOffset(float offset)
        {
            pingOffset = float.IsFinite(offset) ? offset : 0f;
        }

        /// <summary>
        /// マッチ時間全体を設定
        /// </summary>
        public void SetMatchDuration(float duration)
        {
            matchDuration = float.IsFinite(duration) ? Mathf.Max(0f, duration) : 0f;
            localRemainingTime = matchDuration;
        }

        private void TimeUp()
        {
            isStart = false;
            localRemainingTime = 0f;

            if (timeupEvent != null)
            {
                try
                {
                    timeupEvent.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MatchTimer] timeup listener failed: {ex}");
                }
            }
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
            pingOffset = 0f;
        }
    }
}
