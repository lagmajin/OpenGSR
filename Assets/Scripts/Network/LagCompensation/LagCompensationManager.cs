using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS.Network
{
    /// <summary>
    /// ラグ補償システムの管理クラス
    /// シングルトンとして動作し、予測と補間を統合管理
    /// </summary>
    public class LagCompensationManager : MonoBehaviour
    {
        /// <summary>シングルトンインスタンス</summary>
        public static LagCompensationManager Instance { get; private set; }

        /// <summary>プレイヤー予測システム（ローカルプレイヤー用）</summary>
        private NetworkPrediction m_LocalPlayerPrediction;

        /// <summary>他プレイヤー補間システム</summary>
        private NetworkInterpolation m_RemotePlayerInterpolation;

        /// <summary>登録されたネットワークオブジェクト</summary>
        private readonly Dictionary<string, INetworkTransform> m_NetworkObjects =
            new Dictionary<string, INetworkTransform>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TransformState> m_PendingStates =
            new Dictionary<string, TransformState>(StringComparer.OrdinalIgnoreCase);
        private readonly List<KeyValuePair<string, INetworkTransform>> m_NetworkObjectSnapshot =
            new List<KeyValuePair<string, INetworkTransform>>(16);

        /// <summary>ローカルプレイヤーのネットワークID</summary>
        private string m_LocalPlayerNetworkId = string.Empty;

        /// <summary>現在のネットワーク遅延（秒）</summary>
        private float m_CurrentNetworkLatency = 0.1f;

        /// <summary>ネットワーク遅延の履歴</summary>
        private readonly Queue<float> m_LatencyHistory = new Queue<float>();
        private const int MaxLatencyHistory = 30;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // 予測と補間の初期化
            m_LocalPlayerPrediction = new NetworkPrediction(true);
            m_RemotePlayerInterpolation = new NetworkInterpolation();

            // A player may have received OnEnable before this manager's Awake.
            // Register existing instances so pending network states are not stranded.
            foreach (var player in FindObjectsByType<OpenGS.AbstractPlayer>(FindObjectsSortMode.None))
            {
                RegisterNetworkObject(player);
            }
            foreach (var player in FindObjectsByType<OpenGS.AbstractPlayerAgent>(FindObjectsSortMode.None))
            {
                RegisterNetworkObject(player);
            }
        }

        private void Update()
        {
            EnsureLocalPlayer();
            // 他プレイヤーの補間更新
            UpdateRemotePlayers();
        }

        private void EnsureLocalPlayer()
        {
            if (!string.IsNullOrEmpty(m_LocalPlayerNetworkId))
            {
                return;
            }

            foreach (var networkTransform in m_NetworkObjects.Values)
            {
                if (networkTransform is OpenGS.AbstractPlayer player &&
                    player.PlayerType() == OpenGS.EPlayerType.MyPlayer)
                {
                    SetLocalPlayer(networkTransform.OwnerPlayerId);
                    return;
                }

                if (networkTransform is OpenGS.AbstractPlayerAgent playerAgent &&
                    playerAgent.PlayerType() == OpenGS.EPlayerType.MyPlayer)
                {
                    SetLocalPlayer(networkTransform.OwnerPlayerId);
                    return;
                }
            }
        }

        /// <summary>
        /// ネットワークオブジェクトを登録する
        /// </summary>
        public void RegisterNetworkObject(INetworkTransform networkTransform)
        {
            if (networkTransform == null || string.IsNullOrWhiteSpace(networkTransform.OwnerPlayerId))
            {
                return;
            }

            m_NetworkObjects[networkTransform.OwnerPlayerId] = networkTransform;

            if (m_PendingStates.TryGetValue(networkTransform.OwnerPlayerId, out var pendingState))
            {
                m_PendingStates.Remove(networkTransform.OwnerPlayerId);
                OnPlayerStateReceived(pendingState);
            }
        }

        /// <summary>
        /// ネットワークオブジェクトを解除する
        /// </summary>
        public void UnregisterNetworkObject(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            m_NetworkObjects.Remove(playerId);
            m_PendingStates.Remove(playerId);
            m_RemotePlayerInterpolation.ClearPlayer(playerId);
        }

        /// <summary>
        /// ローカルプレイヤーを設定する
        /// </summary>
        public void SetLocalPlayer(string playerId)
        {
            m_LocalPlayerNetworkId = playerId;
        }

        /// <summary>
        /// サーバーからのプレイヤー位置更新を処理する
        /// </summary>
        public void OnPlayerStateReceived(TransformState state)
        {
            if (string.IsNullOrWhiteSpace(state.playerId)
                || !IsFinite(state.position)
                || !IsFinite(state.velocity)
                || !IsFinite(state.rotation)
                || !IsFinite(state.timestamp))
            {
                return;
            }

            if (m_NetworkObjects.TryGetValue(state.playerId, out var networkTransform))
            {
                if (string.Equals(state.playerId, m_LocalPlayerNetworkId, System.StringComparison.OrdinalIgnoreCase))
                {
                    // ローカルプレイヤーの予測校正
                    m_LocalPlayerPrediction.Reconcile(networkTransform, state);
                }
                else
                {
                    // 他プレイヤーの補間に追加
                    m_RemotePlayerInterpolation.AddServerState(state);
                }
            }
            else
            {
                m_PendingStates[state.playerId] = state;
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y)
                && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// プレイヤー入力を予測する
        /// </summary>
        public void ProcessPlayerInput(PlayerInput input)
        {
            if (m_NetworkObjects.TryGetValue(m_LocalPlayerNetworkId, out var transform))
            {
                m_LocalPlayerPrediction.Predict(transform, input);
            }
        }

        /// <summary>
        /// 他プレイヤーの補間を更新する
        /// </summary>
        private void UpdateRemotePlayers()
        {
            // プレイヤーの破棄・シーン遷移がUpdate中に起きても、辞書を
            // 直接変更せず安全に掃除できるようスナップショットを走査する。
            m_NetworkObjectSnapshot.Clear();
            foreach (var kvp in m_NetworkObjects)
            {
                m_NetworkObjectSnapshot.Add(kvp);
            }

            foreach (var kvp in m_NetworkObjectSnapshot)
            {
                var networkTransform = kvp.Value;

                if (networkTransform == null)
                {
                    m_NetworkObjects.Remove(kvp.Key);
                    m_PendingStates.Remove(kvp.Key);
                    m_RemotePlayerInterpolation.ClearPlayer(kvp.Key);
                    continue;
                }

                // ローカルプレイヤーはスキップ
                if (networkTransform.OwnerPlayerId == m_LocalPlayerNetworkId)
                    continue;

                // 補間適用
                m_RemotePlayerInterpolation.UpdateTransform(networkTransform);
            }
        }

        /// <summary>
        /// ネットワーク遅延を更新する
        /// </summary>
        public void UpdateLatency(float latency)
        {
            if (float.IsNaN(latency) || float.IsInfinity(latency))
            {
                return;
            }

            latency = Mathf.Clamp(latency, 0f, 5f);
            m_CurrentNetworkLatency = latency;

            m_LatencyHistory.Enqueue(latency);
            while (m_LatencyHistory.Count > MaxLatencyHistory)
            {
                m_LatencyHistory.Dequeue();
            }

            // 補間の遅延bufferを更新
            m_RemotePlayerInterpolation.SetTargetDelay(latency * 2);
        }

        /// <summary>
        ///  平均遅延を取得
        /// </summary>
        public float GetAverageLatency()
        {
            if (m_LatencyHistory.Count == 0)
                return m_CurrentNetworkLatency;

            float sum = 0;
            foreach (var latency in m_LatencyHistory)
            {
                sum += latency;
            }
            return sum / m_LatencyHistory.Count;
        }

        /// <summary>
        /// 現在の遅延を取得
        /// </summary>
        public float CurrentLatency => m_CurrentNetworkLatency;

        /// <summary>
        /// 予測をクリアする
        /// </summary>
        public void ClearPrediction()
        {
            m_LocalPlayerPrediction.ClearHistory();
        }

        /// <summary>
        /// 全ての補間をクリアする
        /// </summary>
        public void ClearAllInterpolation()
        {
            m_RemotePlayerInterpolation.ClearAll();
        }

        /// <summary>
        /// 全ての状態をリセットする
        /// </summary>
        public void Reset()
        {
            m_LocalPlayerPrediction?.Reset();
            m_RemotePlayerInterpolation?.ClearAll();
            m_NetworkObjects.Clear();
            m_NetworkObjectSnapshot.Clear();
            m_PendingStates.Clear();
            m_LatencyHistory.Clear();
            m_LocalPlayerNetworkId = string.Empty;
            m_CurrentNetworkLatency = 0.1f;
            m_RemotePlayerInterpolation?.SetTargetDelay(m_CurrentNetworkLatency * 2f);
        }

        /// <summary>
        /// 予測の詳細情報を取得（デバッグ用）
        /// </summary>
        public string GetDebugInfo()
        {
            return $"[LagCompensation] " +
                   $"Latency: {m_CurrentNetworkLatency:F3}s " +
                   $"AvgLatency: {GetAverageLatency():F3}s " +
                   $"PredHistory: {m_LocalPlayerPrediction.HistoryCount}";
        }

        private void OnDestroy()
        {
            Reset();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
