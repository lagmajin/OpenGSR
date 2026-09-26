using System.Collections.Generic;
using System;
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

            var items = new List<ShopItemData>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in allItems)
            {
                if (item == null || item.category != category || string.IsNullOrWhiteSpace(item.id))
                {
                    continue;
                }

                var id = item.id.Trim();
                if (!ids.Add(id))
                {
                    continue;
                }

                // Normalize malformed asset data before it reaches purchase
                // and UI code.
                if (item.price < 0)
                {
                    item.price = 0;
                }
                items.Add(item);
            }
            return items.Count > 0 ? items : ShopCatalogFactory.GetDefaultItems(category);
        }

        public ShopItemData GetItemById(string id)
        {
            id = id?.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (allItems != null)
            {
                var item = allItems.Find(entry => entry != null
                    && !string.IsNullOrWhiteSpace(entry.id)
                    && string.Equals(entry.id.Trim(), id, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    return item;
                }
            }

            return ShopCatalogFactory.GetDefaultItemById(id);
        }
    }
}
