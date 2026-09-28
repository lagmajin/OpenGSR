using Newtonsoft.Json.Linq;
using System;

namespace OpenGSCore
{
    public class PingRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Nonce { get; set; } = Guid.NewGuid().ToString("N");
        public long ClientSentAtUnixMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public JObject ToJson()
        {
            return new JObject
            {
                ["PlayerId"] = PlayerId,
                ["Nonce"] = Nonce,
                ["ClientSentAtUnixMs"] = ClientSentAtUnixMs
            };
        }

        public static PingRequest FromJson(JObject json)
        {
            if (json == null)
            {
                return new PingRequest();
            }

            return new PingRequest
            {
                PlayerId = json["PlayerId"]?.ToString() ?? string.Empty,
                Nonce = json["Nonce"]?.ToString() ?? Guid.NewGuid().ToString("N"),
                ClientSentAtUnixMs = json["ClientSentAtUnixMs"]?.ToObject<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
}
