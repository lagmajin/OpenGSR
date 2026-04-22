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
            return new TransformState
            {
                networkId = transform.NetworkId,
                playerId = transform.OwnerPlayerId,
                position = transform.Position,
                rotation = transform.Rotation,
                velocity = transform.Velocity,
                timestamp = timestamp,
                sequenceNumber = sequence
            };
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
                playerId = playerId,
                moveInput = move,
                lookInput = look,
                jump = jump,
                fire = fire,
                sequenceNumber = seq,
                timestamp = time,
                deltaTime = dt
            };
        }
    }
}
