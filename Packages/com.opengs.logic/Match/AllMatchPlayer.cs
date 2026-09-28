#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenGSCore
{
    public class AllMatchPlayer
    {
        private readonly List<PlayerStatus> players = new();

        public AllMatchPlayer()
        {
        }

        public PlayerStatus? SearchPlayer()
        {
            return players.FirstOrDefault() ?? new PlayerStatus();
        }

        public List<PlayerStatus>? SearchPlayers()
        {
            return new List<PlayerStatus>(players);
        }

        public List<PlayerStatus>? SearchTeamPlayers(eTeam team)
        {
            var teamPlayers = new List<PlayerStatus>();
            var teamProperty = typeof(PlayerStatus).GetProperty("Team", BindingFlags.Instance | BindingFlags.NonPublic);
            if (teamProperty == null)
            {
                return teamPlayers;
            }

            foreach (var player in players)
            {
                if (player == null)
                {
                    continue;
                }

                var value = teamProperty.GetValue(player);
                if (value is ETeam playerTeam &&
                    Enum.TryParse<eTeam>(playerTeam.ToString(), true, out var parsedTeam) &&
                    parsedTeam == team)
                {
                    teamPlayers.Add(player);
                }
            }

            return teamPlayers;
        }

        public void AddPlayer(string id, string displayName)
        {
            players.Add(new PlayerStatus());
        }
    }
}
