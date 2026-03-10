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
            return null;
        }
    }
}
