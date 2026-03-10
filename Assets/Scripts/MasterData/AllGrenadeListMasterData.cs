using UnityEngine;
using System.Collections.Generic;

namespace OpenGS
{
    [System.Serializable]
    public class GrenadeEntry
    {
        public string Name;
        public GameObject GrenadePrefab;
    }

    [CreateAssetMenu(menuName = "MasterData/Sound/AllGrenadeListMasterData")]
    public class AllGrenadeListMasterData : ScriptableObject
    {
        public List<GrenadeEntry> dataList = new List<GrenadeEntry>();
    }
}
