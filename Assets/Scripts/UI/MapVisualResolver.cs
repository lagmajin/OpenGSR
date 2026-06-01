using System;
using System.Collections.Generic;
using System.Linq;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public static class MapVisualResolver
    {
        private static readonly Dictionary<EMap, MapInfoMasterData> mapCache = new Dictionary<EMap, MapInfoMasterData>();
        private static readonly Dictionary<string, MapInfoMasterData> nameCache = new Dictionary<string, MapInfoMasterData>(StringComparer.OrdinalIgnoreCase);

        public static MapInfoMasterData GetMapInfo(EMap map)
        {
            if (mapCache.TryGetValue(map, out var cached))
            {
                return cached;
            }

            var all = Resources.LoadAll<MapInfoMasterData>("MasterData/Map");
            foreach (var info in all)
            {
                if (info == null)
                {
                    continue;
                }

                mapCache[info.MapType()] = info;
                nameCache[info.name] = info;
            }

            mapCache.TryGetValue(map, out cached);
            return cached;
        }

        public static string GetDisplayName(EMap map)
        {
            return GetMapInfo(map)?.MapDisplayName() ?? map.ToString();
        }

        public static Sprite GetSmallThumbnail(EMap map)
        {
            return GetMapInfo(map)?.SmallThumbnail();
        }

        public static Sprite GetBigThumbnail(EMap map)
        {
            return GetMapInfo(map)?.BigThumbNail();
        }

        public static bool TryParseMap(string value, out EMap map)
        {
            if (Enum.TryParse(value, true, out map))
            {
                return true;
            }

            switch (value)
            {
                case "DrayDays":
                case "DryDays":
                    map = EMap.DryDays;
                    return true;
                case "GreenHill1":
                    map = EMap.GreenHillSide1;
                    return true;
                case "GreenHill2":
                    map = EMap.GreenHillSide2;
                    return true;
                case "Jungle1":
                    map = EMap.DesertedJungleSide1;
                    return true;
                case "Jungle2":
                    map = EMap.DesertedJungleSide2;
                    return true;
                case "Ruin":
                    map = EMap.RuinOfWarSide1;
                    return true;
                case "House":
                    map = EMap.GhostHouse;
                    return true;
                case "SecretFactory":
                    map = EMap.RobotFactory;
                    return true;
                default:
                    map = EMap.Unknown;
                    return false;
            }
        }
    }
}
