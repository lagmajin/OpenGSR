using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    // JSON保存用のデータクラス
    [Serializable]
    public class UserSaveData
    {
        public List<string> purchasedItems = new List<string>();
        public string equippedBooster = "";
        public string[] equippedInstantItems = new string[3];

        public UserSaveData()
        {
            for (int i = 0; i < equippedInstantItems.Length; i++)
            {
                equippedInstantItems[i] = "";
            }
        }
    }

    /// <summary>
    /// ユーザーの所持品や装備状態を管理する。
    /// JsonStorageを利用して永続化される。
    /// </summary>
    public static class UserSaveManager
    {
        private const int SAVE_VERSION = 1;
        private const string SAVE_FILE = "UserData.json";
        private static UserSaveData data;

        private static void LoadData()
        {
            if (data == null)
            {
                data = JsonStorage.LoadVersioned<UserSaveData>(
                    SAVE_FILE,
                    SAVE_VERSION,
                    Migrate,
                    null);

                if (data == null)
                {
                    // Backward compatibility for non-versioned legacy files.
                    data = JsonStorage.Load<UserSaveData>(SAVE_FILE, new UserSaveData());
                    JsonStorage.SaveVersioned(SAVE_FILE, data, SAVE_VERSION);
                }
            }
        }

        private static void SaveData()
        {
            JsonStorage.SaveVersioned(SAVE_FILE, data, SAVE_VERSION);
        }

        private static UserSaveData Migrate(int fromVersion, UserSaveData oldData)
        {
            // Currently a pass-through migration. Keep hook for future schema upgrades.
            return oldData ?? new UserSaveData();
        }

        public static bool IsPurchased(string itemId)
        {
            // デフォルトで最初から持っているアイテムなどの処理が必要ならここに追加
            if (string.IsNullOrEmpty(itemId)) return true;
            
            LoadData();
            return data.purchasedItems.Contains(itemId);
        }

        public static void SetPurchased(string itemId, bool purchased = true)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            LoadData();
            if (purchased && !data.purchasedItems.Contains(itemId))
            {
                data.purchasedItems.Add(itemId);
            }
            else if (!purchased && data.purchasedItems.Contains(itemId))
            {
                data.purchasedItems.Remove(itemId);
            }
            SaveData();
        }

        public static void EquipItem(string itemId, EShopCategory category)
        {
            EquipToSlot(itemId, category, 0);
        }

        public static void EquipToSlot(string itemId, EShopCategory category, int slotIndex)
        {
            if (category == EShopCategory.Weapon)
            {
                // Weaponは FavoriteWeaponMemoryStorage で複数管理する (トグル式)
                ToggleFavoriteWeapon(itemId);
                return;
            }

            LoadData();

            if (category == EShopCategory.Booster)
            {
                data.equippedBooster = itemId;
            }
            else if (category == EShopCategory.InstantItem)
            {
                if (slotIndex >= 0 && slotIndex < data.equippedInstantItems.Length)
                {
                    data.equippedInstantItems[slotIndex] = itemId;
                }
            }

            SaveData();
            Debug.Log($"Equipped {category} (Slot {slotIndex}): {itemId}");
        }

        public static string GetEquippedId(EShopCategory category)
        {
            return GetEquippedInSlot(category, 0);
        }

        public static string GetEquippedInSlot(EShopCategory category, int slotIndex)
        {
            LoadData();

            if (category == EShopCategory.Booster)
            {
                return data.equippedBooster;
            }
            else if (category == EShopCategory.InstantItem)
            {
                if (slotIndex >= 0 && slotIndex < data.equippedInstantItems.Length)
                {
                    return data.equippedInstantItems[slotIndex];
                }
            }

            return "";
        }

        public static bool IsEquippedAtAnySlot(string itemId, EShopCategory category)
        {
            if (category == EShopCategory.Weapon)
            {
                return IsFavoriteWeapon(itemId);
            }

            if (category == EShopCategory.InstantItem)
            {
                LoadData();
                foreach (var eq in data.equippedInstantItems)
                {
                    if (eq == itemId) return true;
                }
                return false;
            }
            
            return GetEquippedId(category) == itemId;
        }

        // --- Favorite Weapon Integration ---
        public static bool IsFavoriteWeapon(string itemId)
        {
            if (!Enum.TryParse(itemId, true, out OpenGSCore.EWeaponType wType)) return false;

            var favs = FavoriteWeaponMemoryStorage.Load();
            return favs.Contains(wType);
        }

        public static void ToggleFavoriteWeapon(string itemId)
        {
            if (!Enum.TryParse(itemId, true, out OpenGSCore.EWeaponType wType))
            {
                Debug.LogWarning($"Unknown weapon type: {itemId}. Cannot add to favorite.");
                return;
            }

            var favs = FavoriteWeaponMemoryStorage.Load();
            if (favs.Contains(wType))
            {
                favs.Remove(wType);
                Debug.Log($"Removed weapon {itemId} from favorites.");
            }
            else
            {
                favs.Add(wType);
                Debug.Log($"Added weapon {itemId} to favorites.");
            }
            FavoriteWeaponMemoryStorage.Save(favs);
        }
    }
}
