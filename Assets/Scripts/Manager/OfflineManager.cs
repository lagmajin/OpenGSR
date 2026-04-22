using System;

namespace OpenGS
{
    public class OfflineManager
    {
        public static OfflineManager Instance { get; private set; } = new();

        
        public MetalBreakerScore MetalBreakerScore { get; private set; } = new();
        public MissionScore MissionScore { get; private set; } = new();

        private OfflineManager()
        {



        }

    }
}
