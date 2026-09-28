using System;
using System.Collections.Generic;
using System.Text;


namespace OpenGSCore
{
    public class PlayerLoadingInfo
    {
        public PlayerID Id { get; set; } 
        public int LoadingProgress { get; set; }

        public PlayerLoadingInfo(PlayerID id)
        {
            Id = id;
            LoadingProgress = 0;
            //IsReady = false;
            //LastUpdate = DateTime.UtcNow;
        }
        public bool IsCompleted => LoadingProgress >= 100;
    }
}
