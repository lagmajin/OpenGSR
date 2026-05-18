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
        private const int InstantItemSlotCount = 3;
        private const int SAVE_VERSION = 1;
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
                InstantItemSlots = Enumerable.Repeat(EInstantItemType.None, InstantItemSlotCount).ToArray()
            };

            var jObj = JsonStorage.LoadVersioned<JObject>(
                _fileName,
                SAVE_VERSION,
                Migrate,
                null);

            if (jObj == null)
            {
                // Backward compatibility for non-versioned legacy files.
                jObj = JsonStorage.Load<JObject>(_fileName, null);
                if (jObj != null)
                {
                    JsonStorage.SaveVersioned(_fileName, jObj, SAVE_VERSION);
                }
            }

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
                    : Enumerable.Repeat(EInstantItemType.None, InstantItemSlotCount).ToArray();

                if (items.Length != InstantItemSlotCount)
                {
                    var normalized = Enumerable.Repeat(EInstantItemType.None, InstantItemSlotCount).ToArray();
                    var copyLength = Mathf.Min(items.Length, normalized.Length);
                    for (var index = 0; index < copyLength; index++)
                    {
                        normalized[index] = items[index];
                    }

                    items = normalized;
                }

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

        private static JObject Migrate(int fromVersion, JObject oldData)
        {
            return oldData;
        }
    }
}
