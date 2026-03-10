using System.Collections.Generic;
using OpenGSCore;

namespace OpenGS
{
    public static class FavoriteWeaponMemoryStorage
    {
        private const string FILE_NAME = "FavoriteWeapons.json";

        public static void Save(List<EWeaponType> weapons)
        {
            JsonStorage.Save(FILE_NAME, weapons);
        }

        public static List<EWeaponType> Load()
        {
            if (!JsonStorage.Exists(FILE_NAME))
            {
                // 初回実行時はデフォルト武器を返す
                return new List<EWeaponType>() { EWeaponType.AssaultRifle };
            }

            return JsonStorage.Load<List<EWeaponType>>(FILE_NAME, new List<EWeaponType>());
        }
    }
}