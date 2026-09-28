#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーのアシストイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerAssistEvent
    {
        [Key(0)]
        public int AssistPlayerID { get; set; }

        [Key(1)]
        public int VictimPlayerID { get; set; }

        [Key(2)]
        // アシストに貢献したダメージ量など
        public int AssistValue { get; set; }

        [Key(3)]
        public DateTime Time { get; set; }

        [Key(4)]
        // 任意：使用武器など
        public string? Weapon { get; set; }
    }
}

