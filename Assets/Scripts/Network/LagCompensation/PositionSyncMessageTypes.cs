using UnityEngine;

namespace OpenGS.Network
{
    /// <summary>
    /// プレイヤー位置更新イベント（Client用）
    /// </summary>
    public class PlayerPositionUpdateMessage
    {
        public string MessageType { get; set; } = "PlayerPositionUpdate";
        public string PlayerID { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationY { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float VelocityZ { get; set; }
        public byte SequenceNumber { get; set; }
        public float Timestamp { get; set; }

        public Vector3 GetPosition()
        {
            return new Vector3(PositionX, PositionY, PositionZ);
        }

        public Vector3 GetVelocity()
        {
            return new Vector3(VelocityX, VelocityY, VelocityZ);
        }
    }

    /// <summary>
    /// プレイヤー状態同期イベント
    /// </summary>
    public class PlayerStateSyncMessage
    {
        public string MessageType { get; set; } = "GameStateSync";
        public string PlayerID { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationY { get; set; }
        public int Health { get; set; }
        public int Score { get; set; }
        public byte SequenceNumber { get; set; }
        public float Timestamp { get; set; }
        public bool IsGrounded { get; set; }
        public byte WeaponIndex { get; set; }

        public Vector3 GetPosition()
        {
            return new Vector3(PositionX, PositionY, PositionZ);
        }
    }
}

