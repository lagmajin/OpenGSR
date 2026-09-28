using System;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class Guild
    {
        private readonly string uuid;

        public string GuildName { get; private set; }
        public string GuildShortName { get; private set; }
        public string LeaderId { get; private set; }
        public int Level { get; private set; }
        public long Experience { get; private set; }

        public string Uuid => uuid;

        public Guild(in string guildName, in string guildShortName = null, in string leaderId = "")
        {
            uuid = Guid.NewGuid().ToString("N");
            GuildName = guildName ?? string.Empty;
            GuildShortName = string.IsNullOrWhiteSpace(guildShortName) ? GuildName : guildShortName;
            LeaderId = leaderId ?? string.Empty;
            Level = 1;
            Experience = 0;
        }

        public void SetLeader(string leaderId)
        {
            LeaderId = leaderId ?? string.Empty;
        }

        public void AddExperience(long exp)
        {
            if (exp <= 0)
            {
                return;
            }

            Experience += exp;
            while (Experience >= Level * 1000L)
            {
                Experience -= Level * 1000L;
                Level++;
            }
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["Id"] = Uuid,
                ["GuildName"] = GuildName,
                ["GuildShortName"] = GuildShortName,
                ["LeaderId"] = LeaderId,
                ["Level"] = Level,
                ["Experience"] = Experience
            };
        }
    }
}
