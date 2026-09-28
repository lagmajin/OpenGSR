using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore{
    
    public partial class WaitRoom
    {
        public void OnLoadingStart()
        {
            LoadingStart();
        }

        public void OnGameStart()
        {
            GameStart();
        }

        public void OnGameEnd()
        {
            lock (lockObject)
            {
                NowPlaying = false;
            }
        }

        public void OnRoomClosed()
        {
            lock (lockObject)
            {
                RemoveAllPlayers();
                NowPlaying = false;
                GameMode = EGameMode.Unknown;
            }
        }

        public void OnPlayerJoined(PlayerInfo info)
        {
            AddPlayer(info);
        }

        public void OnPlayerLeft(string id)
        {
            RemovePlayer(id);
        }

        public void OnLoadingComplete()
        {
            lock (lockObject)
            {
                NowPlaying = true;
            }
        }



    }
}
