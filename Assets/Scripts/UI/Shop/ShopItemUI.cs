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
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button selectButton;

        private ShopItemData itemData;
        private System.Action<ShopItemData> onSelected;

        public void Setup(ShopItemData data, System.Action<ShopItemData> callback)
        {
            itemData = data;
            onSelected = callback;

            if (iconImage != null) iconImage.sprite = data.icon;
            if (nameText != null) nameText.text = data.itemName;
            if (priceText != null) priceText.text = $"{data.price} CR";

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelected?.Invoke(itemData));
        }
    }
}
