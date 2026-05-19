using System.Collections.Generic;
using UnityEngine;

namespace OpenGS.Network
{
    /// <summary>
    /// ネットワーク補間システム
    /// 他のプレイヤーの動きをスムーズに描画するために使用
    /// </summary>
    public class NetworkInterpolation
    {
        /// <summary>補間に使用する状態バッファ</summary>
        private class InterpolateBuffer
        {
            public Queue<TransformState> StateQueue = new Queue<TransformState>();
            public TransformState? PreviousState;
            public TransformState? NextState;
            public float TargetTimeDelay = 0.1f; // 遅延	buffer
        }

        /// <summary>プレイヤーIDごとの補間バッファ</summary>
        private readonly Dictionary<string, InterpolateBuffer> m_Buffers = new Dictionary<string, InterpolateBuffer>();

        /// <summary>補間パラメータ</summary>
        private readonly float m_InterpolationSmoothing = 0.2f;
        private readonly int m_MaxBufferSize = 20;

        /// <summary>
        /// サーバーからの状態更新を追加する
        /// </summary>
        /// <param name="state">トランスフォーム状態</param>
        public void AddServerState(TransformState state)
        {
            if (!m_Buffers.TryGetValue(state.playerId, out var buffer))
            {
                buffer = new InterpolateBuffer();
                m_Buffers[state.playerId] = buffer;
            }

            // シーケンスが古ければスキップ
            if (buffer.NextState.HasValue && !IsSequenceNewer(state.sequenceNumber, buffer.NextState.Value.sequenceNumber))
            {
                return;
            }

            if (buffer.NextState.HasValue)
            {
                buffer.StateQueue.Enqueue(state);

                // バッファサイズ制限
                while (buffer.StateQueue.Count > m_MaxBufferSize)
                {
                    buffer.StateQueue.Dequeue();
                }
            }
            else
            {
                // 最初の状態
                buffer.NextState = state;
                buffer.PreviousState = state;
            }
        }

        /// <summary>
        /// 補間を更新し、位置を取得する
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="currentPosition">現在の位置（出力）</param>
        /// <param name="currentRotation">現在の回転（出力）</param>
        /// <returns>補間が完了했かどうか</returns>
        public bool UpdateInterpolation(string playerId, out Vector3 currentPosition, out Quaternion currentRotation)
        {
            currentPosition = Vector3.zero;
            currentRotation = Quaternion.identity;

            if (!m_Buffers.TryGetValue(playerId, out var buffer))
            {
                return false;
            }

            float renderTimestamp = Time.time - buffer.TargetTimeDelay;

            if (!buffer.NextState.HasValue && buffer.StateQueue.Count > 0)
            {
                buffer.NextState = buffer.StateQueue.Dequeue();
                buffer.PreviousState = buffer.NextState;
            }

            // サーバー時刻に合わせてバッファを進める
            while (buffer.StateQueue.Count > 0 && buffer.NextState.HasValue && buffer.StateQueue.Peek().timestamp <= renderTimestamp)
            {
                buffer.PreviousState = buffer.NextState;
                buffer.NextState = buffer.StateQueue.Dequeue();
            }

            if (!buffer.PreviousState.HasValue && !buffer.NextState.HasValue)
            {
                return false;
            }

            if (buffer.PreviousState.HasValue && buffer.NextState.HasValue)
            {
                float serverTimeDiff = buffer.NextState.Value.timestamp - buffer.PreviousState.Value.timestamp;

                if (serverTimeDiff > 0)
                {
                    float t = Mathf.Clamp01((renderTimestamp - buffer.PreviousState.Value.timestamp) / serverTimeDiff);

                    currentPosition = Vector3.Lerp(
                        buffer.PreviousState.Value.position,
                        buffer.NextState.Value.position,
                        t
                    );

                    currentRotation = Quaternion.Slerp(
                        buffer.PreviousState.Value.rotation,
                        buffer.NextState.Value.rotation,
                        t
                    );

                    return true;
                }
            }

            // データが少ない場合は最新状態を返す
            if (buffer.PreviousState.HasValue)
            {
                currentPosition = buffer.PreviousState.Value.position;
                currentRotation = buffer.PreviousState.Value.rotation;
                return true;
            }

            if (buffer.NextState.HasValue)
            {
                currentPosition = buffer.NextState.Value.position;
                currentRotation = buffer.NextState.Value.rotation;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 補間を更新する（簡易版）
        /// </summary>
        /// <param name="transform">ネットワークトランスフォーム</param>
        public void UpdateTransform(INetworkTransform transform)
        {
            if (UpdateInterpolation(transform.OwnerPlayerId, out var position, out var rotation))
            {
                transform.Position = position;
                transform.Rotation = rotation;
            }
        }

        /// <summary>
        /// プレイヤーに関連する補間データを全てクリア
        /// </summary>
        public void ClearPlayer(string playerId)
        {
            if (m_Buffers.TryGetValue(playerId, out var buffer))
            {
                buffer.StateQueue.Clear();
                buffer.PreviousState = null;
                buffer.NextState = null;
                buffer.TargetTimeDelay = 0.1f;
            }
        }

        /// <summary>
        /// 全ての補間データをクリア
        /// </summary>
        public void ClearAll()
        {
            m_Buffers.Clear();
        }

        /// <summary>
        /// 遅延buffer時間を設定
        /// </summary>
        public void SetTargetDelay(float delay)
        {
            foreach (var buffer in m_Buffers.Values)
            {
                buffer.TargetTimeDelay = Mathf.Max(0f, delay);
            }
        }

        /// <summary>
        /// バッファにたまった状態数を取得
        /// </summary>
        public int GetBufferCount(string playerId)
        {
            if (m_Buffers.TryGetValue(playerId, out var buffer))
            {
                return buffer.StateQueue.Count;
            }
            return 0;
        }

        /// <summary>
        /// シーケンス番号が新しいかどうかを比較
        /// </summary>
        private bool IsSequenceNewer(byte newSeq, byte oldSeq)
        {
            //  Rolloverを考慮した比較
            return (byte)(newSeq - oldSeq) < 128;
        }
    }
}
