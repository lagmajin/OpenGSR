using System;

namespace OpenGSCore
{
    public interface IWaitRoom
    {
    }

    public class OfflineWaitRoom : IWaitRoom
    {
        public int BotCount { get; private set; }

        public OfflineWaitRoom()
        {
            BotCount = 0;
        }

        public void AddBot()
        {
            BotCount++;
        }

        public void RemoveBot()
        {
            if (BotCount > 0)
            {
                BotCount--;
            }
        }

        public void RemoveAllBot()
        {
            BotCount = 0;
        }
    }
}
