using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OpenGS
{
    /// <summary>
    /// ショップ画面の個別のアイテムを表示するUI要素。
    /// </summary>
    public class ShopItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image accentImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private Button selectButton;

        private ShopItemData itemData;
        private System.Action<ShopItemData> onSelected;

        public ShopItemData ItemData => itemData;

        public void Setup(ShopItemData data, System.Action<ShopItemData> callback)
        {
            itemData = data;
            onSelected = callback;

            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.color = data.category == EShopCategory.Booster ? data.itemColor : Color.white;
            }
            if (accentImage != null)
            {
                accentImage.color = data.category == EShopCategory.Booster ? data.itemColor : Color.clear;
            }
            if (nameText != null) nameText.text = data.itemName;
            if (priceText != null) priceText.text = $"{data.price} CR";

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected?.Invoke(itemData));
            }
        }

        public void RefreshState(bool purchased, bool equipped)
        {
            if (stateText == null)
            {
                return;
            }

            if (equipped)
            {
                stateText.text = "EQUIPPED";
            }
            else if (purchased)
            {
                stateText.text = "OWNED";
            }
            else
            {
                stateText.text = "LOCKED";
            }
        }
    }
}
