using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーのリロードイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerReloadEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        // 武器識別子または種類
        public string WeaponId { get; set; } = string.Empty;

        [Key(2)]
        // リロード前の残弾
        public int AmmoBefore { get; set; }

        [Key(3)]
        // リロード後の残弾
        public int AmmoAfter { get; set; }

        [Key(4)]
        // リロードが完了した時刻
        public DateTime Time { get; set; }

        [Key(5)]
        // リロードが挙動によるキャンセル等か
        public bool IsCancelled { get; set; } = false;

        [Key(6)]
        // リロードにかかった時間（秒, オプション）
        public float DurationSeconds { get; set; } = 0f;
    }
}
