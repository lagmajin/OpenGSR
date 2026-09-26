using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// オフライン用のショップサービス。ローカルの保存データ (UserSaveManager) と
    /// ショップマスターデータ (ShopMasterData) を使用して動作する。
    /// </summary>
    public class OfflineShopService : IShopService
    {
        private readonly ShopMasterData shopMasterData;

        public Action OnDataChanged { get; set; }

        public OfflineShopService(ShopMasterData shopMasterData)
        {
            this.shopMasterData = shopMasterData;
        }

        public UniTask<List<ShopItemData>> GetItemsAsync(EShopCategory category)
        {
            var items = shopMasterData != null
                ? shopMasterData.GetItemsByCategory(category)
                : ShopCatalogFactory.GetDefaultItems(category);

            return UniTask.FromResult(items);
        }

        public UniTask<bool> PurchaseItemAsync(string itemId, int price)
        {
            if (string.IsNullOrWhiteSpace(itemId) || price < 0)
            {
                Debug.LogWarning($"[OfflineShop] Purchase rejected: itemId='{itemId}', price={price}.");
                return UniTask.FromResult(false);
            }

            if (EconomyManager.SpendCredits(price))
            {
                UserSaveManager.SetPurchased(itemId);
                NotifyDataChanged();
                return UniTask.FromResult(true);
            }
            return UniTask.FromResult(false);
        }

        public UniTask<bool> EquipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            itemId = itemId?.Trim();
            if (string.IsNullOrWhiteSpace(itemId) || (category == EShopCategory.InstantItem && !IsValidSlot(slot)))
            {
                Debug.LogWarning($"[OfflineShop] Equip rejected: itemId='{itemId}', category={category}, slot={slot}.");
                return UniTask.FromResult(false);
            }

            if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot(itemId, category, slot);
            }
            else
            {
                UserSaveManager.EquipItem(itemId, category);
            }
            NotifyDataChanged();
            return UniTask.FromResult(true);
        }

        public UniTask<bool> UnequipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            if (category == EShopCategory.InstantItem && !IsValidSlot(slot))
            {
                Debug.LogWarning($"[OfflineShop] Unequip rejected: category={category}, slot={slot}.");
                return UniTask.FromResult(false);
            }

            if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot("", category, slot);
            }
            else
            {
                // ここでは空にする処理
                UserSaveManager.EquipItem("", category);
            }
            NotifyDataChanged();
            return UniTask.FromResult(true);
        }

        private void NotifyDataChanged()
        {
            if (OnDataChanged == null)
            {
                return;
            }

            foreach (Action handler in OnDataChanged.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OfflineShopService] OnDataChanged subscriber failed: {ex}");
                }
            }
        }

        public long GetCredits() => EconomyManager.GetCredits();

        public bool IsPurchased(string itemId) => UserSaveManager.IsPurchased(itemId);

        public bool IsEquipped(string itemId, EShopCategory category, int slot = 0)
        {
            if (category == EShopCategory.InstantItem)
            {
                return UserSaveManager.GetEquippedInSlot(category, slot) == itemId;
            }
            if (category == EShopCategory.Weapon)
            {
                return UserSaveManager.IsFavoriteWeapon(itemId);
            }
            return UserSaveManager.GetEquippedId(category) == itemId;
        }

        public string GetEquippedItemId(EShopCategory category, int slot = 0)
        {
            if (category == EShopCategory.InstantItem)
            {
                var items = UserSaveManager.GetEquippedInstantItems();
                return items != null && slot >= 0 && slot < items.Length ? items[slot] : string.Empty;
            }

            return UserSaveManager.GetEquippedId(category) ?? string.Empty;
        }

        private static bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < 3;
        }
    }
}
