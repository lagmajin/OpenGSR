using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public sealed class MissionSetting
    {
        public bool LifeLimit { get; private set; } = true;

        public int LifeCount { get; private set; } = 3;

        public int MaxPlayer { get; private set; } = 3;

        public MissionSetting(int life = 3, int maxPlayer = 3)
        {
            LifeLimit = life > 0;
            LifeCount = Math.Max(1, life);
            MaxPlayer = Math.Max(1, maxPlayer);
        }
    }
}
