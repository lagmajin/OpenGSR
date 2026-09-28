#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum ChatType
    {
        All,        // 全体チャット
        Team,       // チームチャット
        Whisper,    // ささやき（1対1）
        System      // システムメッセージ
    }

    public class Chat
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string PlayerId { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public string Message { get; set; } = "";
        public ChatType Type { get; set; } = ChatType.All;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? TargetId { get; set; }  // Whisper先のプレイヤーID

        public Chat()
        {
        }

        public Chat(string playerId, string playerName, string message, ChatType type = ChatType.All)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Message = message;
            Type = type;
            Timestamp = DateTime.UtcNow;
        }
    }
}

