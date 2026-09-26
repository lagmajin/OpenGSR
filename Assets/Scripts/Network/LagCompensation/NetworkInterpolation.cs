using System;
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
            public bool HasLatestSequence;
            public byte LatestSequence;
        }

        /// <summary>プレイヤーIDごとの補間バッファ</summary>
        private readonly Dictionary<string, InterpolateBuffer> m_Buffers =
            new Dictionary<string, InterpolateBuffer>(StringComparer.OrdinalIgnoreCase);

        private readonly int m_MaxBufferSize = 20;
        private float m_TargetTimeDelay = 0.1f;

        /// <summary>
        /// サーバーからの状態更新を追加する
        /// </summary>
        /// <param name="state">トランスフォーム状態</param>
        public void AddServerState(TransformState state)
        {
            if (string.IsNullOrWhiteSpace(state.playerId)
                || !IsFinite(state.position)
                || !IsFinite(state.velocity)
                || !IsFinite(state.rotation)
                || float.IsNaN(state.timestamp)
                || float.IsInfinity(state.timestamp)
                || state.timestamp < 0f)
            {
                return;
            }

            if (state.rotation.sqrMagnitude < 0.0001f)
            {
                state.rotation = Quaternion.identity;
            }
            else
            {
                state.rotation = Quaternion.Normalize(state.rotation);
            }

            if (!m_Buffers.TryGetValue(state.playerId, out var buffer))
            {
                buffer = new InterpolateBuffer { TargetTimeDelay = m_TargetTimeDelay };
                m_Buffers[state.playerId] = buffer;
            }

            // キューに複数状態がある場合でも、直近に受理した状態を基準に
            // 比較する。NextStateだけを基準にすると、古いパケットが
            // キューへ再混入する。
            if (buffer.HasLatestSequence && !IsSequenceNewer(state.sequenceNumber, buffer.LatestSequence))
            {
                return;
            }

            buffer.LatestSequence = state.sequenceNumber;
            buffer.HasLatestSequence = true;

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

            if (string.IsNullOrWhiteSpace(playerId)
                || !m_Buffers.TryGetValue(playerId, out var buffer))
            {
                return false;
            }

            var now = Time.time;
            if (!float.IsFinite(now) || now < 0f)
            {
                if (buffer.PreviousState.HasValue)
                {
                    currentPosition = buffer.PreviousState.Value.position;
                    currentRotation = buffer.PreviousState.Value.rotation;
                    return true;
                }
                return false;
            }

            float renderTimestamp = now - buffer.TargetTimeDelay;

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
            if (transform == null || string.IsNullOrWhiteSpace(transform.OwnerPlayerId))
            {
                return;
            }

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
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            m_Buffers.Remove(playerId);
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
            m_TargetTimeDelay = float.IsFinite(delay) ? Mathf.Clamp(delay, 0f, 10f) : 0.1f;
            foreach (var buffer in m_Buffers.Values)
            {
                buffer.TargetTimeDelay = m_TargetTimeDelay;
            }
        }

        /// <summary>
        /// バッファにたまった状態数を取得
        /// </summary>
        public int GetBufferCount(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId)
                && m_Buffers.TryGetValue(playerId, out var buffer))
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
            var distance = (byte)(newSeq - oldSeq);
            return distance != 0 && distance < 128;
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
    }
}
