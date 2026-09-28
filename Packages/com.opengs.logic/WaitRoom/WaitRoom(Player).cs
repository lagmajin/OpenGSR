using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{

    public partial class WaitRoom{



        public void RemovePlayer()
        {
            RemoveAllPlayers();
        }

        public void LoadingStart()
        {
            lock (lockObject)
            {
                NowPlaying = true;
            }
        }
    }



}
