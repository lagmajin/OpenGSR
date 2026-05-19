using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Scene/MapSceneMasterData")]
    public class MapSceneMasterData : ScriptableObject
    {
        [SerializeField] private MapInfoMasterData[] cachedMaps;

        public MapInfoMasterData Map(EMap map)
        {
            EnsureCache();

            if (cachedMaps == null)
            {
                return null;
            }

            foreach (var entry in cachedMaps)
            {
                if (entry != null && entry.MapType() == map)
                {
                    return entry;
                }
            }

            return null;
        }

        private void EnsureCache()
        {
            if (cachedMaps != null && cachedMaps.Length > 0)
            {
                return;
            }

            cachedMaps = Resources.LoadAll<MapInfoMasterData>("MasterData/Map");
        }

#if UNITY_EDITOR
        public void RefreshCache()
        {
            cachedMaps = Resources.LoadAll<MapInfoMasterData>("MasterData/Map");
        }
#endif
    }
}
