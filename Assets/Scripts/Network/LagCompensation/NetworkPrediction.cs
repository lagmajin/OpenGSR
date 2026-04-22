using System.Collections.Generic;
using UnityEngine;

namespace OpenGS.Network
{
    /// <summary>
    /// クライアントサイド予測システム
    /// プレイヤーの入力を即座にローカルで適用し、サーバーとの差異を解決する
    /// </summary>
    public class NetworkPrediction
    {
        /// <summary>予測状態を保存する構造体</summary>
        private struct PredictedState
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public byte inputSequence;
            public float timestamp;
        }

        /// <summary>予測履歴（サーバーからの確認を待つ）</summary>
        private readonly Queue<PredictedState> m_PredictionHistory = new Queue<PredictedState>();

        /// <summary>現在のシーケンス番号</summary>
        private byte m_CurrentSequence;

        /// <summary>最大予測ステップ数</summary>
        private const int MaxPredictionSteps = 120;

        /// <summary>クライアント所有のオブジェクトかどうか</summary>
        private readonly bool m_IsLocalPlayer;

        /// <summary> Lerp係数（位置補正の強さ）</summary>
        private float m_CorrectionFactor = 0.1f;

        /// <summary>現在校正中のシーケンス</summary>
        private byte m_PendingCorrectionSequence;

        public NetworkPrediction(bool isLocalPlayer)
        {
            m_IsLocalPlayer = isLocalPlayer;
        }

        /// <summary>
        /// 入力予測を実行する
        /// 実際のゲームロジックをローカルでシミュレートする
        /// </summary>
        /// <param name="transform">ネットワークトランスフォーム</param>
        /// <param name="input">プレイヤー入力</param>
        public void Predict(INetworkTransform transform, PlayerInput input)
        {
            if (!m_IsLocalPlayer) return;

            // シーケンス番号を進める
            m_CurrentSequence = (byte)((m_CurrentSequence + 1) % 256);
            input.sequenceNumber = m_CurrentSequence;

            // 現在の状態を保存（ロールバック用）
            PredictedState currentState = new PredictedState
            {
                position = transform.Position,
                rotation = transform.Rotation,
                velocity = transform.Velocity,
                inputSequence = m_CurrentSequence,
                timestamp = Time.time
            };

            m_PredictionHistory.Enqueue(currentState);

            // 履歴サイズ制限
            while (m_PredictionHistory.Count > MaxPredictionSteps)
            {
                m_PredictionHistory.Dequeue();
            }

            // 予測移動を適用（実際のゲームロジックに置換が必要）
            ApplyPredictedMovement(transform, input);
        }

        /// <summary>
        /// 予測移動を適用（プレースホルダー実装）
        /// 実際のプレイヤー移動ロジックに置き換えること
        /// </summary>
        private void ApplyPredictedMovement(INetworkTransform transform, PlayerInput input)
        {
            // TODO: 実際のプレイヤー移動ロジックを実装
            // 例:
            // Vector3 newVelocity = CalculateVelocity(input.moveInput, transform.Velocity);
            // transform.Position += newVelocity * input.deltaTime;
            // transform.Rotation = Quaternion.Euler(input.lookInput);

            // 簡易的な移動の実装（プレースホルダー）
            Vector3 predictedVelocity = input.moveInput * 10f; // 移動速度定数
            Vector3 newPosition = transform.Position + predictedVelocity * input.deltaTime;
            transform.Position = newPosition;
        }

        /// <summary>
        /// サーバーからの状態更新で予測を校正する
        /// </summary>
        /// <param name="transform">ネットワークトランスフォーム</param>
        /// <param name="serverState">サーバーから受信した状態</param>
        public void Reconcile(INetworkTransform transform, TransformState serverState)
        {
            // 自分のプレイヤーだけ校正を行う
            if (!m_IsLocalPlayer) return;

            // 対応する予測状態を見つける
            PredictedState? matchedState = FindMatchingPrediction(serverState.sequenceNumber);

            if (matchedState.HasValue)
            {
                // サーバーとの誤差を計算
                Vector3 positionError = transform.Position - serverState.position;
                float errorMagnitude = positionError.magnitude;

                // 許容誤差内ならそのまま
                if (errorMagnitude < 0.01f)
                {
                    // 小さい誤差はそのまま
                    return;
                }

                // 大きい誤差的正好輯
                if (errorMagnitude > 1.0f)
                {
                    // 大きな誤差的正好
                    transform.Position = serverState.position;
                    transform.Rotation = serverState.rotation;

                    // 履歴を消去して再予測
                    ClearHistory();
                }
                else
                {
                    // 中間の誤差的少しずつ校正
                    transform.Position = Vector3.Lerp(transform.Position, serverState.position, m_CorrectionFactor);
                }

                // サーバー速度を採用
                // 注意: 実際のゲームでは速度の校正も考慮すること
            }
        }

        /// <summary>
        /// 指定シーケンスの予測状態を見つける
        /// </summary>
        private PredictedState? FindMatchingPrediction(byte targetSequence)
        {
            foreach (var state in m_PredictionHistory)
            {
                if (state.inputSequence == targetSequence)
                {
                    return state;
                }
            }
            return null;
        }

        /// <summary>
        /// 予測履歴をクリアする
        /// </summary>
        public void ClearHistory()
        {
            m_PredictionHistory.Clear();
        }

        /// <summary>
        /// 校正係数を設定する
        /// </summary>
        public void SetCorrectionFactor(float factor)
        {
            m_CorrectionFactor = Mathf.Clamp01(factor);
        }

        /// <summary>
        /// 現在のシーケンス番号を取得
        /// </summary>
        public byte CurrentSequence => m_CurrentSequence;

        /// <summary>
        /// 予測履歴の件数を取得
        /// </summary>
        public int HistoryCount => m_PredictionHistory.Count;
    }
}