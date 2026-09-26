using UnityEngine;
using System;

namespace OpenGS
{
    public enum EShopCategory
    {
        Weapon,
        Booster,
        InstantItem,
        Character
    }

    [CreateAssetMenu(menuName = "Shop/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string id;
        public string itemName;
        [TextArea] public string description;
        public int price;
        public EShopCategory category;

        [Header("Assets")]
        public Sprite icon;
        public GameObject prefab;

        [Header("Stats (Optional)")]
        public float power;
        public float weight;
        public Color itemColor = Color.white;

        private void OnValidate()
        {
            id = id?.Trim() ?? string.Empty;
            itemName = itemName?.Trim() ?? string.Empty;
            price = Mathf.Max(0, price);

            if (!float.IsFinite(power))
            {
                power = 0f;
            }

            if (!float.IsFinite(weight))
            {
                weight = 0f;
            }
        }
    }
}
