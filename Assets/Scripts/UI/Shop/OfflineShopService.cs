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
            return UniTask.FromResult(shopMasterData.GetItemsByCategory(category));
        }

        public UniTask<bool> PurchaseItemAsync(string itemId, int price)
        {
            if (EconomyManager.SpendCredits(price))
            {
                UserSaveManager.SetPurchased(itemId);
                OnDataChanged?.Invoke();
                return UniTask.FromResult(true);
            }
            return UniTask.FromResult(false);
        }

        public UniTask<bool> EquipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot(itemId, category, slot);
            }
            else
            {
                UserSaveManager.EquipItem(itemId, category);
            }
            OnDataChanged?.Invoke();
            return UniTask.FromResult(true);
        }

        public UniTask<bool> UnequipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot("", category, slot);
            }
            else
            {
                // ここでは空にする処理
                UserSaveManager.EquipItem("", category);
            }
            OnDataChanged?.Invoke();
            return UniTask.FromResult(true);
        }

        public long GetCredits() => EconomyManager.GetCredits();

        public bool IsPurchased(string itemId) => UserSaveManager.IsPurchased(itemId);

        public bool IsEquipped(string itemId, EShopCategory category, int slot = 0)
        {
            if (category == EShopCategory.InstantItem)
            {
                return UserSaveManager.GetEquippedInSlot(category, slot) == itemId;
            }
            return UserSaveManager.GetEquippedId(category) == itemId;
        }
    }
}
