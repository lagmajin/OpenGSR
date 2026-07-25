using OpenGSCore;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Player/PlayerPrefabMasterData")]
    public class PlayerPrefabMasterData : ScriptableObject
    {
        [Serializable]
        public class CharacterPrefabEntry
        {
            public EPlayerCharacter character;
            public GameObject prefab;
        }

        [SerializeField] private GameObject defaultPrefab;
        public GameObject mistyPrefab;
        [SerializeField] private List<CharacterPrefabEntry> characterPrefabs = new List<CharacterPrefabEntry>();

        public GameObject SearchPlayerPrefab(string charId)
        {
            if (string.IsNullOrWhiteSpace(charId))
            {
                Debug.LogWarning("[PlayerPrefabMasterData] Character id is empty.");
                return null;
            }

            if (string.Equals(charId, EPlayerCharacter.Misty.ToString(), System.StringComparison.OrdinalIgnoreCase))
            {
                return mistyPrefab;
            }

            if (Enum.TryParse(charId, true, out EPlayerCharacter character))
            {
                var prefab = SearchPlayerPrefab(character);
                if (prefab != null)
                {
                    return prefab;
                }
            }

            Debug.LogWarning($"[PlayerPrefabMasterData] Unknown character id: {charId}");
            return defaultPrefab != null ? defaultPrefab : mistyPrefab;
        }

        public GameObject SearchPlayerPrefab(EPlayerCharacter character)
        {
            if (character == EPlayerCharacter.Misty && mistyPrefab != null)
            {
                return mistyPrefab;
            }

            foreach (var entry in characterPrefabs)
            {
                if (entry != null && entry.character == character && entry.prefab != null)
                {
                    return entry.prefab;
                }
            }

            // Keep character spawning resilient while the serialized roster is being rebuilt.
            // The generated playable prefabs live outside Resources, so load them through the
            // editor/runtime asset reference only when they are already registered; otherwise
            // use the known-good default instead of failing the whole match.

            return defaultPrefab != null ? defaultPrefab : mistyPrefab;
        }
    }
}
