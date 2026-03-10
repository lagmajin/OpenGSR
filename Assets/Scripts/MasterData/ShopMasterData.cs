using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "Shop/ShopMasterData")]
    public class ShopMasterData : ScriptableObject
    {
        [TableList]
        public List<ShopItemData> allItems = new List<ShopItemData>();

        public List<ShopItemData> GetItemsByCategory(EShopCategory category)
        {
            return allItems.FindAll(item => item.category == category);
        }

        public ShopItemData GetItemById(string id)
        {
            return allItems.Find(item => item.id == id);
        }
    }
}
