using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using MessagePack;
using MemoryPack;

namespace OpenGSCore
{

    //#core
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerShotEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        // 発射した武器の種類
        public string WeaponType { get; set; }

        [Key(2)]
        // 発射時の位置（X, Y, Z 座標）
        public Vector2 FirePosition { get; set; }
        [Key(3)]
        // 発射のタイムスタンプ
        public DateTime FireTime { get; set; }
        [Key(4)]
        // 発射時の弾速
        public float BulletSpeed { get; set; }
        [Key(5)]
        // イベントが処理されたかどうかを示すフラグ
        public bool IsProcessed { get; set; }
    }


}
