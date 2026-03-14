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
        public Action OnDataChanged { get; set; }

        public async UniTask<List<ShopItemData>> GetItemsAsync(EShopCategory category)
        {
            // TODO: サーバーからアイテム情報を取得
            Debug.Log("[OnlineShop] Requesting items from server...");
            await UniTask.Delay(500); // 通信シミュレーション
            return new List<ShopItemData>(); // 実際はマスターデータ等から取得
        }

        public async UniTask<bool> PurchaseItemAsync(string itemId, int price)
        {
            // TODO: サーバーに購入リクエスト送信
            Debug.Log($"[OnlineShop] Purchasing item {itemId} on server...");
            await UniTask.Delay(500);
            return true;
        }

        public async UniTask<bool> EquipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            // TODO: サーバーに装備リクエスト送信
            await UniTask.Delay(200);
            OnDataChanged?.Invoke();
            return true;
        }

        public async UniTask<bool> UnequipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            // TODO: サーバーに装備解除リクエスト送信
            await UniTask.Delay(200);
            OnDataChanged?.Invoke();
            return true;
        }

        public long GetCredits() => 0; // TODO: サーバーから取得したクレジットを返す

        public bool IsPurchased(string itemId) => false; // TODO: サーバーデータを参照

        public bool IsEquipped(string itemId, EShopCategory category, int slot = 0) => false;
    }
}
