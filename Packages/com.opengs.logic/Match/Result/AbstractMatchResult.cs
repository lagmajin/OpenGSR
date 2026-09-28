#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenGSCore
{
    public interface IMatchResult
    {
        List<PlayerID> winnersList();
        List<PlayerID> losersList();
    }

    public abstract class AbstractMatchResult : IMatchResult
    {
        private readonly List<PlayerID> winners = new();
        private readonly List<PlayerID> losers = new();

        public bool Won { get; private set; } = false;
        public bool Lost { get; private set; } = false;

        public List<PlayerID> winnersList()
        {
            return new List<PlayerID>(winners);
        }

        public List<PlayerID> losersList()
        {
            return new List<PlayerID>(losers);
        }

        protected void SetOutcome(bool won, bool lost)
        {
            Won = won;
            Lost = lost;
        }

        protected void SetWinners(IEnumerable<PlayerID>? playerIds)
        {
            winners.Clear();
            if (playerIds == null)
            {
                return;
            }

            winners.AddRange(playerIds.Where(id => id != null));
        }

        protected void SetLosers(IEnumerable<PlayerID>? playerIds)
        {
            losers.Clear();
            if (playerIds == null)
            {
                return;
            }

            losers.AddRange(playerIds.Where(id => id != null));
        }
    }
}

