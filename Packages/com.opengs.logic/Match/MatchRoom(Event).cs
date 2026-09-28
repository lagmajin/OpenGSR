using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGSCore
{
    public partial class MatchRoom
    {
        private readonly HashSet<string> _readyPlayers = new();

        public void StartLoading()
        {
            lock (playerSyncLock)
            {
                _readyPlayers.Clear();
            }
            eventBus.PublishLoadingStart();
        }

        public void SetPlayerReady(string playerId)
        {
            bool shouldStart = false;

            lock (playerSyncLock)
            {
                if (!Players.Exists(p => string.Equals(p.Id, playerId, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var canonicalPlayerId = Players.First(p =>
                    string.Equals(p.Id, playerId, StringComparison.OrdinalIgnoreCase)).Id;
                _readyPlayers.Add(canonicalPlayerId);
                shouldStart = _readyPlayers.Count >= Players.Count && Players.Count > 0 && !Playing;
            }

            if (shouldStart)
            {
                GameStart();
            }
        }
    }


}
