using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGSCore
{
    public partial class MatchRoom
    {
        private readonly HashSet<string> _readyPlayers = new();
        private bool _loading;
        private bool _starting;

        public void StartLoading()
        {
            lock (playerSyncLock)
            {
                if (_loading || _starting || Playing || Finished)
                {
                    return;
                }

                _readyPlayers.Clear();
                _loading = true;
            }
            eventBus.PublishLoadingStart();
        }

        public void SetPlayerReady(string playerId)
        {
            bool shouldStart = false;

            lock (playerSyncLock)
            {
                if (!_loading || _starting || Playing || Finished)
                {
                    return;
                }

                if (!Players.Exists(p => string.Equals(p.Id, playerId, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var canonicalPlayerId = Players.First(p =>
                    string.Equals(p.Id, playerId, StringComparison.OrdinalIgnoreCase)).Id;
                _readyPlayers.Add(canonicalPlayerId);
                shouldStart = _readyPlayers.Count >= Players.Count && Players.Count > 0;
            }

            if (shouldStart)
            {
                GameStart();
            }
        }

    }


}
