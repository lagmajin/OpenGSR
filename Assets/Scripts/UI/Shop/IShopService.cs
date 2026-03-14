using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace OpenGS
{
    /// <summary>
    /// ショップのデータ取得・購入・装備処理を抽象化するインターフェース。
    /// Online (Server RPC) と Offline (Local Save) で実装を切り替える。
    /// </summary>
    public interface IShopService
    {
        /// <summary>
        /// カテゴリごとのアイテムリストを取得
        /// </summary>
        UniTask<List<ShopItemData>> GetItemsAsync(EShopCategory category);

        /// <summary>
        /// アイテムを購入
        /// </summary>
        UniTask<bool> PurchaseItemAsync(string itemId, int price);

        /// <summary>
        /// アイテムを装備
        /// </summary>
        UniTask<bool> EquipItemAsync(string itemId, EShopCategory category, int slot = 0);

        /// <summary>
        /// アイテムの装備を解除
        /// </summary>
        UniTask<bool> UnequipItemAsync(string itemId, EShopCategory category, int slot = 0);

        /// <summary>
        /// 現在の所持金を取得
        /// </summary>
        long GetCredits();

        /// <summary>
        /// アイテムを購入済みか判定
        /// </summary>
        bool IsPurchased(string itemId);

        /// <summary>
        /// 指定スロットにアイテムが装備されているか判定
        /// </summary>
        bool IsEquipped(string itemId, EShopCategory category, int slot = 0);

        /// <summary>
        /// データが変更された際の通知イベント
        /// </summary>
        Action OnDataChanged { get; set; }
    }
}
