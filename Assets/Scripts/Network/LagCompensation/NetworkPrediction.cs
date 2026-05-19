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
            public PlayerInput input;
            public byte inputSequence;
            public float timestamp;
        }

        /// <summary>予測履歴（サーバーからの確認を待つ）</summary>
        private readonly List<PredictedState> m_PredictionHistory = new List<PredictedState>();

        /// <summary>現在のシーケンス番号</summary>
        private byte m_CurrentSequence;

        /// <summary>最大予測ステップ数</summary>
        private const int MaxPredictionSteps = 120;

        /// <summary>ソフト補正を行う誤差閾値</summary>
        private const float SoftCorrectionThreshold = 0.05f;

        /// <summary>ハード補正を行う誤差閾値</summary>
        private const float HardCorrectionThreshold = 1.0f;

        /// <summary>クライアント所有のオブジェクトかどうか</summary>
        private readonly bool m_IsLocalPlayer;

        /// <summary> Lerp係数（位置補正の強さ）</summary>
        private float m_CorrectionFactor = 0.1f;

        /// <summary>最後に確定したシーケンス番号</summary>
        private byte m_LastConfirmedSequence;

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
            var currentState = new PredictedState
            {
                position = transform.Position,
                rotation = transform.Rotation,
                velocity = transform.Velocity,
                input = input,
                inputSequence = m_CurrentSequence,
                timestamp = Time.time
            };

            m_PredictionHistory.Add(currentState);

            // 履歴サイズ制限
            TrimHistory();

            // 予測移動を適用
            ApplyPredictedMovement(transform, input);
        }

        /// <summary>
        /// 予測移動を適用（プレースホルダー実装）
        /// 実際のプレイヤー移動ロジックに置き換えること
        /// </summary>
        private void ApplyPredictedMovement(INetworkTransform transform, PlayerInput input)
        {
            float dt = Mathf.Max(0f, input.deltaTime);
            Vector3 moveInput = input.moveInput;

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            const float moveSpeed = 10f;
            Vector3 predictedVelocity = moveInput * moveSpeed;
            Vector3 newPosition = transform.Position + predictedVelocity * dt;
            transform.Position = newPosition;

            if (input.lookInput.sqrMagnitude > 0.0001f)
            {
                Vector3 look = input.lookInput.normalized;
                float angle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
                transform.Rotation = Quaternion.Euler(0f, 0f, angle);
            }
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

            if (!IsSequenceNewerOrEqual(serverState.sequenceNumber, m_LastConfirmedSequence))
            {
                return;
            }

            // 対応する予測状態を見つける
            int matchedIndex = FindMatchingPredictionIndex(serverState.sequenceNumber);

            if (matchedIndex < 0)
            {
                transform.Position = serverState.position;
                transform.Rotation = serverState.rotation;
                ClearHistory();
                m_LastConfirmedSequence = serverState.sequenceNumber;
                return;
            }

            // サーバーとの誤差を計算
            Vector3 positionError = transform.Position - serverState.position;
            float errorMagnitude = positionError.magnitude;

            // 許容誤差内ならそのまま
            if (errorMagnitude < SoftCorrectionThreshold)
            {
                TrimConfirmedHistory(matchedIndex);
                m_LastConfirmedSequence = serverState.sequenceNumber;
                return;
            }

            if (errorMagnitude > HardCorrectionThreshold)
            {
                transform.Position = serverState.position;
                transform.Rotation = serverState.rotation;
                TrimConfirmedHistory(matchedIndex);
                ReplayPendingInputs(transform);
            }
            else
            {
                transform.Position = Vector3.Lerp(transform.Position, serverState.position, m_CorrectionFactor);
                transform.Rotation = Quaternion.Slerp(transform.Rotation, serverState.rotation, m_CorrectionFactor);
                TrimConfirmedHistory(matchedIndex);
            }

            m_LastConfirmedSequence = serverState.sequenceNumber;
        }

        /// <summary>
        /// 指定シーケンスの予測状態を見つける
        /// </summary>
        private int FindMatchingPredictionIndex(byte targetSequence)
        {
            for (int i = m_PredictionHistory.Count - 1; i >= 0; i--)
            {
                var state = m_PredictionHistory[i];
                if (state.inputSequence == targetSequence)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 予測履歴を最大件数に丸める
        /// </summary>
        private void TrimHistory()
        {
            while (m_PredictionHistory.Count > MaxPredictionSteps)
            {
                m_PredictionHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// サーバーで確定したシーケンスまでの履歴を捨てる
        /// </summary>
        private void TrimConfirmedHistory(int matchedIndex)
        {
            if (matchedIndex < 0)
            {
                return;
            }

            int removeCount = Mathf.Min(matchedIndex + 1, m_PredictionHistory.Count);
            if (removeCount > 0)
            {
                m_PredictionHistory.RemoveRange(0, removeCount);
            }
        }

        /// <summary>
        /// 残っている予測入力を順に再適用する
        /// </summary>
        private void ReplayPendingInputs(INetworkTransform transform)
        {
            for (int i = 0; i < m_PredictionHistory.Count; i++)
            {
                ApplyPredictedMovement(transform, m_PredictionHistory[i].input);
            }
        }

        /// <summary>
        /// シーケンス番号比較（ラップアラウンド対応）
        /// </summary>
        private bool IsSequenceNewerOrEqual(byte newSeq, byte oldSeq)
        {
            byte delta = (byte)(newSeq - oldSeq);
            return delta == 0 || delta < 128;
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
