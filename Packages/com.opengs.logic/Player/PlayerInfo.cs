#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace OpenGSCore
{

    public class PlayerInfoLite
    {
        public string Id { get; }
        public string Name { get; }
        public string? CurrentIp { get; }

        public PlayerInfoLite(string id, string name, string? currentIp = null)
        {
            Id = id;
            Name = name;
            CurrentIp = currentIp;
        }

        public JObject ToJson()
        {
            var result = new JObject();
            result["Id"] = Id;
            result["Name"] = Name;
            if (CurrentIp != null) result["CurrentIP"] = CurrentIp;
            return result;
        }

    }

    //#PlayerInfo
    public class PlayerInfo
    {
        // public-facing properties
        public string Id { get; set; }
        public string Name { get; set; }
        public string? CurrentIp { get; set; } = null;
        public int Ping { get; set; } = 0;
        public EPlayerCharacter playerCharacter { get; set; }

        // New properties for player stats
        public int Level { get; set; } = 1;
        public long Exp { get; set; } = 0;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int AttackPower { get; set; } = 10;
        public int DefensePower { get; set; } = 5;

        // Match specific properties
        public ETeam Team { get; set; } = ETeam.NoTeam;
        public bool IsReady { get; set; } = false;
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int LifeCount { get; set; } = 3;
        public int Score { get; set; } = 0;
        public List<EInstantItemType> EquipInstantItems { get; set; } = new();

        // Indicates whether this entry represents a bot
        public bool IsBot { get; set; } = false;

        public PlayerInfo(string? id = null, string name = "", string? currentIp = null, int level = 1, long exp = 0, int health = 100, int attack = 10, int defense = 5)
        {
            Id = id ?? string.Empty;
            Name = name;
            CurrentIp = currentIp;
            playerCharacter = EPlayerCharacter.Misty;
            Level = level;
            Exp = exp;
            Health = health;
            MaxHealth = health; // MaxHealth defaults to initial Health
            AttackPower = attack;
            DefensePower = defense;
        }

        public JObject ToJson()
        {
            var result = new JObject();
            result["Id"] = Id;
            result["PlayerId"] = Id;
            result["PlayerID"] = Id;
            result["Name"] = Name;
            result["PlayerName"] = Name;
            result["DisplayName"] = Name;
            if (CurrentIp != null) result["CurrentIP"] = CurrentIp;
            if (CurrentIp != null) result["CurrentIp"] = CurrentIp;
            result["Ping"] = Ping;
            result["IsBot"] = IsBot;
            result["Level"] = Level;
            result["Exp"] = Exp;
            result["Health"] = Health;
            result["MaxHealth"] = MaxHealth;
            result["AttackPower"] = AttackPower;
            result["DefensePower"] = DefensePower;
            result["Team"] = Team.ToString();
            result["TeamName"] = Team.ToString();
            result["IsReady"] = IsReady;
            var equipInstantItems = new JArray();
            foreach (var item in EquipInstantItems)
            {
                equipInstantItems.Add(item.ToString());
            }
            result["EquipInstantItems"] = equipInstantItems;
            result["PlayerCharacter"] = playerCharacter.ToString();
            result["Kills"] = Kills;
            result["KillCount"] = Kills;
            result["Deaths"] = Deaths;
            result["DeathCount"] = Deaths;
            result["Score"] = Score;
            result["LifeCount"] = LifeCount;
            result["TotalScore"] = Kills * 100;
            result["Points"] = Kills * 100;
            return result;
        }

        public static PlayerInfo FromJson(JObject json)
        {
            if (json == null)
            {
                return new PlayerInfo();
            }

            var player = new PlayerInfo(
                id: json.GetStringAny("Id", "PlayerId", "PlayerID", "PlayerLocalId") ?? string.Empty,
                name: json.GetStringAny("Name", "PlayerName", "DisplayName") ?? string.Empty,
                currentIp: json.GetStringAny("CurrentIP", "CurrentIp"))
            {
                Ping = json.GetIntAny("Ping") ?? 0,
                IsBot = json.GetBoolAny("IsBot") ?? false,
                Level = json.GetIntAny("Level") ?? 1,
                Exp = json["Exp"]?.ToObject<long>() ?? 0,
                Health = json.GetIntAny("Health") ?? 100,
                MaxHealth = json.GetIntAny("MaxHealth") ?? json.GetIntAny("Health") ?? 100,
                AttackPower = json.GetIntAny("AttackPower") ?? 10,
                DefensePower = json.GetIntAny("DefensePower") ?? 5,
                IsReady = json.GetBoolAny("IsReady") ?? false,
                Kills = json.GetIntAny("Kills", "KillCount") ?? 0,
                Deaths = json.GetIntAny("Deaths", "DeathCount") ?? 0
            };

            if (Enum.TryParse(json.GetStringAny("PlayerCharacter"), true, out EPlayerCharacter playerCharacter))
            {
                player.playerCharacter = playerCharacter;
            }

            if (Enum.TryParse(json.GetStringAny("Team", "TeamName"), true, out ETeam team))
            {
                player.Team = team;
            }

            if (json["EquipInstantItems"] is JArray equipInstantItems)
            {
                player.EquipInstantItems.Clear();
                foreach (var token in equipInstantItems)
                {
                    if (Enum.TryParse(token?.ToString(), true, out EInstantItemType itemType))
                    {
                        player.EquipInstantItems.Add(itemType);
                    }
                }
            }

            return player;
        }
    }
}

