using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // 武器切り替えイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class WeaponChangeEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        // 前の武器
        public string OldWeaponId { get; set; }

        [Key(2)]
        // 新しい武器
        public string NewWeaponId { get; set; }

        [Key(3)]
        public DateTime Time { get; set; }

        [Key(4)]
        // 即時切替だったか（クールダウンやアニメーションの有無）
        public bool Instant { get; set; }
    }
}
