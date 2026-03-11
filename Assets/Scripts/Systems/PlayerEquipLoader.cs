using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public class PlayerEquipData
    {
        public EPlayerCharacter PlayerCharacter;
        public EInstantItemType[] InstantItemSlots;
    }

    /// <summary>
    /// Runtime loader for player equip data used by local test server.
    /// </summary>
    public class PlayerEquipLoader
    {
        private readonly string _fileName;

        public PlayerEquipLoader(string fileName = "PlayerEquip.json")
        {
            _fileName = fileName;
        }

        public PlayerEquipData Load()
        {
            var fallback = new PlayerEquipData
            {
                PlayerCharacter = EPlayerCharacter.Ami,
                InstantItemSlots = Array.Empty<EInstantItemType>()
            };

            var jObj = JsonStorage.Load<JObject>(_fileName, null);
            if (jObj == null) return fallback;

            try
            {
                var characterStr = jObj["PlayerCharacter"]?.ToString();
                var itemArray = jObj["InstantItemSlot"] as JArray;

                if (!Enum.TryParse(characterStr, out EPlayerCharacter playerCharacter))
                {
                    return fallback;
                }

                var items = itemArray != null
                    ? itemArray.Select(t => Enum.TryParse(t?.ToString(), out EInstantItemType type) ? type : default).ToArray()
                    : Array.Empty<EInstantItemType>();

                return new PlayerEquipData
                {
                    PlayerCharacter = playerCharacter,
                    InstantItemSlots = items
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerEquipLoader] Failed to parse {_fileName}: {ex.Message}");
                return fallback;
            }
        }
    }
}
