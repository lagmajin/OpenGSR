using System;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class OneOnOneChat
    {
        public string SenderID { get; set; } = string.Empty;
        public string ReceiverID { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public long SentAtUtcTicks { get; set; } = DateTime.UtcNow.Ticks;

        public OneOnOneChat()
        {
        }

        public OneOnOneChat(string senderId, string receiverId, string message)
        {
            SenderID = senderId ?? string.Empty;
            ReceiverID = receiverId ?? string.Empty;
            Message = message ?? string.Empty;
            SentAtUtcTicks = DateTime.UtcNow.Ticks;
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["SenderID"] = SenderID,
                ["ReceiverID"] = ReceiverID,
                ["Message"] = Message,
                ["SentAtUtcTicks"] = SentAtUtcTicks
            };
        }
    }
}
