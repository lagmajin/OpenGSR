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
        private readonly GeneralServerNetworkManager serverManager;
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
            Debug.Log($"[OnlineShop] Purchasing item {itemId} on server...");
            await UniTask.Yield();

            var success = serverManager != null
                ? serverManager.PurchaseItem(itemId, price)
                : EconomyManager.SpendCredits(price);

            OnDataChanged?.Invoke();
            return success;
        }

        public async UniTask<bool> EquipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
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

            OnDataChanged?.Invoke();
            return success;
        }

        public async UniTask<bool> UnequipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
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

            OnDataChanged?.Invoke();
            return success;
        }

        public long GetCredits() => serverManager != null ? serverManager.GetCredits() : EconomyManager.GetCredits();

        public bool IsPurchased(string itemId)
        {
            return serverManager != null ? serverManager.IsPurchased(itemId) : UserSaveManager.IsPurchased(itemId);
        }

        public bool IsEquipped(string itemId, EShopCategory category, int slot = 0)
        {
            return serverManager != null
                ? serverManager.IsEquipped(itemId, category, slot)
                : UserSaveManager.IsEquippedAtAnySlot(itemId, category);
        }
    }
}
