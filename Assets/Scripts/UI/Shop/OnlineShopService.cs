using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// オンライン（サーバー通信）用のショップサービス。
    /// 将来的にサーバーRPCを介してデータをやり取りする実装。
    /// </summary>
    public class OnlineShopService : IShopService
    {
        private GeneralServerNetworkManager serverManager;
        private readonly ShopMasterData shopMasterData;

        public Action OnDataChanged { get; set; }

        public OnlineShopService()
            : this(null)
        {
        }

        public OnlineShopService(ShopMasterData shopMasterData)
        {
            this.shopMasterData = shopMasterData != null
                ? shopMasterData
                : Resources.Load<ShopMasterData>("MasterData/ShopMasterData");

            try
            {
                serverManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch
            {
                serverManager = null;
            }
        }

        public async UniTask<List<ShopItemData>> GetItemsAsync(EShopCategory category)
        {
            Debug.Log("[OnlineShop] Requesting items from server...");
            await UniTask.Yield();

            if (shopMasterData != null)
            {
                return shopMasterData.GetItemsByCategory(category);
            }

            return ShopCatalogFactory.GetDefaultItems(category);
        }

        public async UniTask<bool> PurchaseItemAsync(string itemId, int price)
        {
            EnsureServerManager();
            if (string.IsNullOrWhiteSpace(itemId) || price < 0)
            {
                Debug.LogWarning($"[OnlineShop] Purchase rejected: itemId='{itemId}', price={price}.");
                return false;
            }

            Debug.Log($"[OnlineShop] Purchasing item {itemId} on server...");
            await UniTask.Yield();

            var success = serverManager != null
                ? serverManager.PurchaseItem(itemId, price)
                : EconomyManager.SpendCredits(price);

            if (success)
            {
                // サーバー購入成功時も、次のUI更新・再起動で状態を失わないよう
                // クライアントの購入済みキャッシュを同期する。
                UserSaveManager.SetPurchased(itemId);
            }

            NotifyDataChanged();
            return success;
        }

        public async UniTask<bool> EquipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            EnsureServerManager();
            itemId = itemId?.Trim();
            if (string.IsNullOrWhiteSpace(itemId) || (category == EShopCategory.InstantItem && !IsValidSlot(slot)))
            {
                Debug.LogWarning($"[OnlineShop] Equip rejected: itemId='{itemId}', category={category}, slot={slot}.");
                return false;
            }

            await UniTask.Yield();

            var success = true;
            if (serverManager != null)
            {
                success = serverManager.EquipItem(itemId, category, slot);
            }
            else if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot(itemId, category, slot);
            }
            else
            {
                UserSaveManager.EquipItem(itemId, category);
            }

            NotifyDataChanged();
            return success;
        }

        public async UniTask<bool> UnequipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            EnsureServerManager();
            if (category == EShopCategory.InstantItem && !IsValidSlot(slot))
            {
                Debug.LogWarning($"[OnlineShop] Unequip rejected: category={category}, slot={slot}.");
                return false;
            }

            await UniTask.Yield();

            var success = true;
            if (serverManager != null)
            {
                success = serverManager.UnequipItem(category, slot);
            }
            else if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot("", category, slot);
            }
            else
            {
                UserSaveManager.EquipItem("", category);
            }

            NotifyDataChanged();
            return success;
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
                    Debug.LogError($"[OnlineShopService] OnDataChanged subscriber failed: {ex}");
                }
            }
        }

        public long GetCredits()
        {
            EnsureServerManager();
            return serverManager != null ? serverManager.GetCredits() : EconomyManager.GetCredits();
        }

        public bool IsPurchased(string itemId)
        {
            EnsureServerManager();
            return serverManager != null ? serverManager.IsPurchased(itemId) : UserSaveManager.IsPurchased(itemId);
        }

        public bool IsEquipped(string itemId, EShopCategory category, int slot = 0)
        {
            EnsureServerManager();
            return serverManager != null
                ? serverManager.IsEquipped(itemId, category, slot)
                : (category == EShopCategory.InstantItem
                    ? UserSaveManager.GetEquippedInSlot(category, slot) == itemId
                    : category == EShopCategory.Weapon
                        ? UserSaveManager.IsFavoriteWeapon(itemId)
                        : UserSaveManager.GetEquippedId(category) == itemId);
        }

        public string GetEquippedItemId(EShopCategory category, int slot = 0)
        {
            EnsureServerManager();
            if (serverManager != null)
            {
                return serverManager.GetEquippedItemId(category, slot);
            }

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

        private void EnsureServerManager()
        {
            if (serverManager != null)
            {
                return;
            }

            try
            {
                serverManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch
            {
                // The offline fallback remains valid until the shared manager is ready.
            }
        }
    }
}
