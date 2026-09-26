using UnityEngine;

namespace OpenGS.Network
{
    /// <summary>
    /// ネットワーク同期可能なトランスフォームインターフェース
    /// ラグ補償の対象となるオブジェクトに実装
    /// </summary>
    public interface INetworkTransform
    {
        /// <summary>オブジェクトの一意のID</summary>
        uint NetworkId { get; }

        /// <summary>現在の位置</summary>
        Vector3 Position { get; set; }

        /// <summary>現在の回転</summary>
        Quaternion Rotation { get; set; }

        /// <summary>現在の速度</summary>
        Vector3 Velocity { get; }

        /// <summary>ネットワーク 所有者のプレイヤーID</summary>
        string OwnerPlayerId { get; }
    }

    /// <summary>
    /// トランスフォームの状態を表す構造体
    /// </summary>
    public struct TransformState
    {
        public uint networkId;
        public string playerId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public float timestamp;
        public byte sequenceNumber;

        public static TransformState Create(INetworkTransform transform, float timestamp, byte sequence)
        {
            if (transform == null)
            {
                return default;
            }

            var position = SanitizeVector(transform.Position);
            var rotation = SanitizeRotation(transform.Rotation);
            var velocity = SanitizeVector(transform.Velocity);
            return new TransformState
            {
                networkId = transform.NetworkId,
                playerId = transform.OwnerPlayerId ?? string.Empty,
                position = position,
                rotation = rotation,
                velocity = velocity,
                timestamp = float.IsFinite(timestamp) ? timestamp : (float.IsFinite(Time.time) && Time.time >= 0f ? Time.time : 0f),
                sequenceNumber = sequence
            };
        }



        private static Vector3 SanitizeVector(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z)
                ? value
                : Vector3.zero;
        }

        private static Quaternion SanitizeRotation(Quaternion value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w) ||
                QuaternionMath.SqrMagnitude(value) < 0.000001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.Normalize(value);
        }
    }

    /// <summary>
    /// プレイヤー入力データ
    /// </summary>
    public struct PlayerInput
    {
        public string playerId;
        public Vector3 moveInput;
        public Vector3 lookInput;
        public bool jump;
        public bool fire;
        public byte sequenceNumber;
        public float timestamp;
        public float deltaTime;

        public static PlayerInput Create(string playerId, Vector3 move, Vector3 look, bool jump, bool fire, byte seq, float time, float dt)
        {
            return new PlayerInput
            {
                playerId = playerId ?? string.Empty,
                moveInput = SanitizeInputVector(move),
                lookInput = SanitizeInputVector(look),
                jump = jump,
                fire = fire,
                sequenceNumber = seq,
                timestamp = float.IsFinite(time) ? time : (float.IsFinite(Time.time) && Time.time >= 0f ? Time.time : 0f),
                deltaTime = float.IsFinite(dt) ? Mathf.Clamp(dt, 0f, 0.25f) : 0f
            };
        }

        private static Vector3 SanitizeInputVector(Vector3 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
            {
                return Vector3.zero;
            }

            return value.sqrMagnitude > 1f ? value.normalized : value;
        }
    }
}
