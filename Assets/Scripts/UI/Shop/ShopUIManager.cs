using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OpenGS
{
    /// <summary>
    /// ショップ画面全体の表示とロジックを管理するマネージャー。
    /// </summary>
    public class ShopUIManager : MonoBehaviour
    {
        [Header("Master Data")]
        [SerializeField] private ShopMasterData shopMasterData;

        [Header("UI Containers")]
        [SerializeField] private Transform itemGridRoot;
        [SerializeField] private GameObject itemPrefab;

        [Header("Detail Panel")]
        [SerializeField] private TextMeshProUGUI creditsText; // 所持金表示
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailName;
        [SerializeField] private TextMeshProUGUI detailDescription;
        [SerializeField] private TextMeshProUGUI detailPrice;
        [SerializeField] private Button actionButton; // 購入 or 装備ボタン
        [SerializeField] private TextMeshProUGUI actionButtonText;

        [Header("Slot Selection (Instant Items)")]
        [SerializeField] private GameObject slotSelectionRoot;
        [SerializeField] private Button[] slotButtons; // 3個のボタン
        [SerializeField] private Image[] slotButtonImages;
        [SerializeField] private Color selectedSlotColor = Color.yellow;
        [SerializeField] private Color normalSlotColor = Color.white;

        [Header("Category Tabs")]
        [SerializeField] private Button weaponTab;
        [SerializeField] private Button itemTab;
        [SerializeField] private Button boosterTab;

        private ShopItemData selectedItem;
        private List<GameObject> activeItemObjects = new List<GameObject>();
        private int currentSelectedSlot = 0;

        private void Start()
        {
            UpdateCreditsDisplay();

            // 初期表示は武器カテゴリー
            SwitchCategory(EShopCategory.Weapon);

            // タブのイベント登録
            if (weaponTab) weaponTab.onClick.AddListener(() => SwitchCategory(EShopCategory.Weapon));
            if (itemTab) itemTab.onClick.AddListener(() => SwitchCategory(EShopCategory.InstantItem));
            if (boosterTab) boosterTab.onClick.AddListener(() => SwitchCategory(EShopCategory.Booster));

            if (actionButton) actionButton.onClick.AddListener(OnActionClicked);

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                slotButtons[i].onClick.AddListener(() => SelectSlot(index));
            }

            if (detailPanel) detailPanel.SetActive(false);
            if (slotSelectionRoot) slotSelectionRoot.SetActive(false);
            SelectSlot(0);
        }

        private void SelectSlot(int index)
        {
            currentSelectedSlot = index;
            UpdateSlotButtonsVisual();
            UpdateButtonState();
        }

        private void UpdateSlotButtonsVisual()
        {
            for (int i = 0; i < slotButtonImages.Length; i++)
            {
                if (slotButtonImages[i] != null)
                    slotButtonImages[i].color = (i == currentSelectedSlot) ? selectedSlotColor : normalSlotColor;
            }
        }

        public void SwitchCategory(EShopCategory category)
        {
            // 既存のリストをクリア
            foreach (var obj in activeItemObjects)
            {
                Destroy(obj);
            }
            activeItemObjects.Clear();

            // 指定カテゴリーのアイテムを取得して生成
            var items = shopMasterData.GetItemsByCategory(category);
            foreach (var item in items)
            {
                var go = Instantiate(itemPrefab, itemGridRoot);
                var ui = go.GetComponent<ShopItemUI>();
                if (ui != null)
                {
                    ui.Setup(item, OnItemSelected);
                }
                activeItemObjects.Add(go);
            }
        }

        private void OnItemSelected(ShopItemData item)
        {
            selectedItem = item;
            
            if (detailPanel != null)
            {
                detailPanel.SetActive(true);
                if (detailIcon) detailIcon.sprite = item.icon;
                if (detailName) detailName.text = item.itemName;
                if (detailDescription) detailDescription.text = item.description;
                if (detailPrice) detailPrice.text = $"PRICE: {item.price} CR";
                
                UpdateButtonState();
            }
        }

        private void UpdateButtonState()
        {
            if (selectedItem == null || actionButton == null || actionButtonText == null) return;

            bool purchased = UserSaveManager.IsPurchased(selectedItem.id);
            
            // インスタントアイテムの場合は特定スロットの装備状況を見る
            bool equippedAtCurrentSlot = UserSaveManager.GetEquippedInSlot(selectedItem.category, currentSelectedSlot) == selectedItem.id;
            bool equippedAtAnySlot = UserSaveManager.IsEquippedAtAnySlot(selectedItem.id, selectedItem.category);

            if (slotSelectionRoot) slotSelectionRoot.SetActive(purchased && selectedItem.category == EShopCategory.InstantItem);

            if (!purchased)
            {
                actionButtonText.text = "BUY";
                actionButton.interactable = EconomyManager.CanAfford(selectedItem.price);
            }
            else if (selectedItem.category == EShopCategory.InstantItem)
            {
                if (equippedAtCurrentSlot)
                {
                    actionButtonText.text = "UNEQUIP";
                    actionButton.interactable = true;
                }
                else
                {
                    actionButtonText.text = "EQUIP TO SLOT " + (currentSelectedSlot + 1);
                    actionButton.interactable = true;
                }
            }
            else if (selectedItem.category == EShopCategory.Weapon)
            {
                bool isFavorite = UserSaveManager.IsFavoriteWeapon(selectedItem.id);
                if (isFavorite)
                {
                    actionButtonText.text = "REMOVE FAVORITE";
                    actionButton.interactable = true;
                }
                else
                {
                    actionButtonText.text = "ADD FAVORITE";
                    actionButton.interactable = true;
                }
            }
            else
            {
                // Booster 等の単体スロット用
                bool equipped = UserSaveManager.GetEquippedId(selectedItem.category) == selectedItem.id;
                if (!equipped)
                {
                    actionButtonText.text = "EQUIP";
                    actionButton.interactable = true;
                }
                else
                {
                    actionButtonText.text = "EQUIPPED";
                    actionButton.interactable = false;
                }
            }
        }

        private void OnActionClicked()
        {
            if (selectedItem == null) return;

            bool purchased = UserSaveManager.IsPurchased(selectedItem.id);

            if (!purchased)
            {
                if (EconomyManager.SpendCredits(selectedItem.price))
                {
                    UserSaveManager.SetPurchased(selectedItem.id);
                    Debug.Log($"Purchased: {selectedItem.itemName}");
                    UpdateCreditsDisplay();
                    UpdateButtonState();
                }
            }
            else
            {
                if (selectedItem.category == EShopCategory.InstantItem)
                {
                    string equippedInSlot = UserSaveManager.GetEquippedInSlot(selectedItem.category, currentSelectedSlot);
                    if (equippedInSlot == selectedItem.id)
                    {
                        // 同じスロットに装備済みなら解除
                        UserSaveManager.EquipToSlot("", selectedItem.category, currentSelectedSlot);
                    }
                    else
                    {
                        // 別のスロットに同じアイテムがあれば移動するか、重複不可にするか？
                        // ここでは、指定スロットに上書き装備
                        UserSaveManager.EquipToSlot(selectedItem.id, selectedItem.category, currentSelectedSlot);
                    }
                }
                else
                {
                    UserSaveManager.EquipItem(selectedItem.id, selectedItem.category);
                }
                UpdateButtonState();
            }
        }

        private void UpdateCreditsDisplay()
        {
            if (creditsText != null)
            {
                creditsText.text = $"CREDITS: {EconomyManager.GetCredits()}";
            }
        }
    }
}
