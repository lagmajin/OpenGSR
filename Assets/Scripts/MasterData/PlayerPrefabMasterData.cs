using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Player/PlayerPrefabMasterData")]
    public class PlayerPrefabMasterData : ScriptableObject
    {
        public GameObject mistyPrefab;

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

            Debug.LogWarning($"[PlayerPrefabMasterData] Unknown character id: {charId}");
            return null;
        }
    }
}
