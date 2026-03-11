using System.Collections.Generic;
using OpenGSCore;

namespace OpenGS
{
    public static class FavoriteWeaponMemoryStorage
    {
        private const int SAVE_VERSION = 1;
        private const string FILE_NAME = "FavoriteWeapons.json";

        public static void Save(List<EWeaponType> weapons)
        {
            JsonStorage.SaveVersioned(FILE_NAME, weapons, SAVE_VERSION);
        }

        public static List<EWeaponType> Load()
        {
            if (!JsonStorage.Exists(FILE_NAME))
            {
                // 初回実行時はデフォルト武器を返す
                return new List<EWeaponType>() { EWeaponType.AK47 };
            }

            var loaded = JsonStorage.LoadVersioned<List<EWeaponType>>(
                FILE_NAME,
                SAVE_VERSION,
                Migrate,
                null);

            if (loaded != null)
            {
                return loaded;
            }

            // Backward compatibility for non-versioned legacy files.
            loaded = JsonStorage.Load<List<EWeaponType>>(FILE_NAME, new List<EWeaponType>());
            JsonStorage.SaveVersioned(FILE_NAME, loaded, SAVE_VERSION);
            return loaded;
        }

        private static List<EWeaponType> Migrate(int fromVersion, List<EWeaponType> oldData)
        {
            return oldData ?? new List<EWeaponType>() { EWeaponType.AK47 };
        }
    }
}
