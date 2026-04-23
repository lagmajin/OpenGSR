using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using OpenGSCore;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "Shop/ShopMasterData")]
    public class ShopMasterData : ScriptableObject
    {
        [TableList]
        public List<ShopItemData> allItems = new List<ShopItemData>();

        public List<ShopItemData> GetItemsByCategory(EShopCategory category)
        {
            if (allItems == null || allItems.Count == 0)
            {
                return ShopCatalogFactory.GetDefaultItems(category);
            }

            var items = allItems.FindAll(item => item != null && item.category == category);
            return items.Count > 0 ? items : ShopCatalogFactory.GetDefaultItems(category);
        }

        public ShopItemData GetItemById(string id)
        {
            if (allItems != null)
            {
                var item = allItems.Find(entry => entry != null && entry.id == id);
                if (item != null)
                {
                    return item;
                }
            }

            return ShopCatalogFactory.GetDefaultItemById(id);
        }
    }
}
