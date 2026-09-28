
#nullable enable
using System;
using System.Collections.Generic;

namespace OpenGSCore
{
    public interface IMultipleKeyDictionary
    {

    }

    public class SearchOption
    {

    }


    public class SearchInfo
    {
        public MultipleKey Key { get; set; } = null!;

        
    }

    public class PlayerData
    {
        public PlayerInfo info;
        public PlayerStatus status;
        public PlayerData(PlayerInfo info,PlayerStatus status)
        {
            this.info = info ?? new PlayerInfo();
            this.status = status ?? new PlayerStatus();
        }

    }


    public class PlayerDatabase
    {
        //private List<Tuple<MultipleKey, PlayerStatus>> player = new();

        private List<string> allIDCache=new();

        //private List<Tuple<MultipleKey, PlayerInfo>> data=new ();

        private List<PlayerData> data = new();


        public PlayerDatabase()
        {

        }

        public void Player(SearchInfo info)
        {



        }

        public PlayerData? Player(string id)
        {
            return TryGetPlayer(id, out var player) ? player : null;
        }

        public bool TryGetPlayer(string id, out PlayerData? player)
        {
            player = null;

            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            foreach (PlayerData temp in data)
            {
                if(id==temp.info.Id)
                {
                    player = temp;
                    return true;
                }

            }


            Console.WriteLine($"[PlayerDatabase] Player not found: {id}");
            return false;
        }

        public bool AddPlayer(PlayerData player)
        {
            if (player == null || player.info == null || string.IsNullOrWhiteSpace(player.info.Id))
            {
                return false;
            }

            for (var index = 0; index < data.Count; index++)
            {
                if (string.Equals(data[index].info.Id, player.info.Id, StringComparison.OrdinalIgnoreCase))
                {
                    data[index] = player;
                    if (!allIDCache.Contains(player.info.Id))
                    {
                        allIDCache.Add(player.info.Id);
                    }
                    return true;
                }
            }

            data.Add(player);
            if (!allIDCache.Contains(player.info.Id))
            {
                allIDCache.Add(player.info.Id);
            }
            return true;
        }

        public bool RemovePlayer(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            var removed = data.RemoveAll(entry => string.Equals(entry.info?.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
            allIDCache.RemoveAll(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));
            return removed;
        }

        public List<string> AllID()
        {

            return allIDCache;
        }

        public List<PlayerData> AllPlayer()
        {
            return data;
        }

        public void RemoveAll()
        {
            data.Clear ();
            allIDCache.Clear();

        }
    }
}
