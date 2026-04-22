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
        private readonly Dictionary<string, INetworkTransform> m_NetworkObjects = new Dictionary<string, INetworkTransform>();

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
        }

        private void Update()
        {
            // 他プレイヤーの補間更新
            UpdateRemotePlayers();
        }

        /// <summary>
        /// ネットワークオブジェクトを登録する
        /// </summary>
        public void RegisterNetworkObject(INetworkTransform networkTransform)
        {
            m_NetworkObjects[networkTransform.OwnerPlayerId] = networkTransform;
        }

        /// <summary>
        /// ネットワークオブジェクトを解除する
        /// </summary>
        public void UnregisterNetworkObject(string playerId)
        {
            m_NetworkObjects.Remove(playerId);
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
            if (m_NetworkObjects.TryGetValue(state.playerId, out var networkTransform))
            {
                if (state.playerId == m_LocalPlayerNetworkId)
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
            foreach (var kvp in m_NetworkObjects)
            {
                var networkTransform = kvp.Value;

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
            ClearPrediction();
            ClearAllInterpolation();
            m_NetworkObjects.Clear();
            m_LatencyHistory.Clear();
            m_LocalPlayerNetworkId = string.Empty;
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
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
