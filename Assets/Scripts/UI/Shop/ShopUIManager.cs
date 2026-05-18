using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using Cysharp.Threading.Tasks;

namespace OpenGS
{
    /// <summary>
    /// ショップ画面全体の表示とロジックを管理するマネージャー。
    /// IShopService を介して Online/Offline を切り替える。
    /// </summary>
    public class ShopUIManager : MonoBehaviour
    {
        private const int InstantItemSlotCount = 3;

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
        [SerializeField] private TextMeshProUGUI[] slotButtonTexts;
        [SerializeField] private TextMeshProUGUI slotSummaryText;
        [SerializeField] private Color selectedSlotColor = Color.yellow;
        [SerializeField] private Color normalSlotColor = Color.white;

        [Header("Category Tabs")]
        [SerializeField] private Button weaponTab;
        [SerializeField] private Button itemTab;
        [SerializeField] private Button boosterTab;

        private IShopService shopService;
        private ShopItemData selectedItem;
        private List<ShopItemUI> activeItemObjects = new List<ShopItemUI>();
        private int currentSelectedSlot = 0;
        private EShopCategory currentCategory = EShopCategory.Weapon;

        [Inject]
        public void Construct(IShopService shopService)
        {
            this.shopService = shopService;
        }

        private void Start()
        {
            if (shopService == null)
            {
                try
                {
                    shopService = DependencyInjectionConfig.Resolve<IShopService>();
                }
                catch
                {
                    shopService = new OfflineShopService(null);
                }
            }

            shopService.OnDataChanged += UpdateUI;

            UpdateCreditsDisplay();

            // 初期表示は武器カテゴリー
            SwitchCategory(currentCategory).Forget();

            // タブのイベント登録
            if (weaponTab) weaponTab.onClick.AddListener(() => SwitchCategory(EShopCategory.Weapon).Forget());
            if (itemTab) itemTab.onClick.AddListener(() => SwitchCategory(EShopCategory.InstantItem).Forget());
            if (boosterTab) boosterTab.onClick.AddListener(() => SwitchCategory(EShopCategory.Booster).Forget());

            if (actionButton) actionButton.onClick.AddListener(() => OnActionClicked().Forget());

            if (slotButtons != null)
            {
                for (int i = 0; i < slotButtons.Length; i++)
                {
                    int index = i;
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].onClick.AddListener(() => SelectSlot(index));
                    }
                }
            }

            if (detailPanel) detailPanel.SetActive(false);
            if (slotSelectionRoot) slotSelectionRoot.SetActive(false);
            SelectSlot(0);
        }

        private void OnDestroy()
        {
            if (shopService != null)
                shopService.OnDataChanged -= UpdateUI;
        }

        private void UpdateUI()
        {
            UpdateCreditsDisplay();
            UpdateButtonState();
            UpdateSlotSummary();
            RefreshItemStates();
        }

        private void SelectSlot(int index)
        {
            currentSelectedSlot = Mathf.Clamp(index, 0, InstantItemSlotCount - 1);
            UpdateSlotButtonsVisual();
            UpdateSlotSummary();
            UpdateButtonState();
        }

        private void UpdateSlotButtonsVisual()
        {
            if (slotButtons != null)
            {
                for (int i = 0; i < slotButtons.Length; i++)
                {
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].interactable = true;
                    }
                }
            }

            if (slotButtonImages != null)
            {
                for (int i = 0; i < slotButtonImages.Length; i++)
                {
                    if (slotButtonImages[i] != null)
                    {
                        slotButtonImages[i].color = (i == currentSelectedSlot) ? selectedSlotColor : normalSlotColor;
                    }
                }
            }

            if (slotButtonTexts != null)
            {
                for (int i = 0; i < slotButtonTexts.Length; i++)
                {
                    if (slotButtonTexts[i] != null)
                    {
                        var labelIndex = i + 1;
                        slotButtonTexts[i].text = $"SLOT {labelIndex}";
                        slotButtonTexts[i].color = (i == currentSelectedSlot) ? selectedSlotColor : normalSlotColor;
                    }
                }
            }
        }

        public async UniTaskVoid SwitchCategory(EShopCategory category)
        {
            currentCategory = category;
            selectedItem = null;

            // 既存のリストをクリア
            foreach (var obj in activeItemObjects)
            {
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            activeItemObjects.Clear();

            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }

            if (slotSelectionRoot != null)
            {
                slotSelectionRoot.SetActive(false);
            }

            // 指定カテゴリーのアイテムをサービス経由で取得
            var items = await shopService.GetItemsAsync(category);
            
            foreach (var item in items)
            {
                var go = Instantiate(itemPrefab, itemGridRoot);
                var ui = go.GetComponent<ShopItemUI>();
                if (ui != null)
                {
                    ui.Setup(item, OnItemSelected);
                    activeItemObjects.Add(ui);
                }
            }

            RefreshItemStates();
        }

        private void OnItemSelected(ShopItemData item)
        {
            selectedItem = item;
            
            if (detailPanel != null)
            {
                detailPanel.SetActive(true);
                if (detailIcon) detailIcon.sprite = item.icon;
                if (detailIcon) detailIcon.color = item.category == EShopCategory.Booster ? item.itemColor : Color.white;
                if (detailName) detailName.text = item.itemName;
                if (detailDescription) detailDescription.text = item.description;
                if (detailPrice) detailPrice.text = $"PRICE: {item.price} CR";
                
                UpdateButtonState();
            }
        }

        private void UpdateButtonState()
        {
            if (selectedItem == null || actionButton == null || actionButtonText == null) return;

            bool purchased = shopService.IsPurchased(selectedItem.id);
            bool equipped = shopService.IsEquipped(selectedItem.id, selectedItem.category, currentSelectedSlot);

            if (slotSelectionRoot) slotSelectionRoot.SetActive(purchased && selectedItem.category == EShopCategory.InstantItem);

            if (!purchased)
            {
                actionButtonText.text = "BUY";
                actionButton.interactable = shopService.GetCredits() >= selectedItem.price;
            }
            else if (selectedItem.category == EShopCategory.InstantItem)
            {
                if (equipped)
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
                // ここは将来的に Favorite 管理もサービスに入れる
                actionButtonText.text = equipped ? "EQUIPPED" : "EQUIP";
                actionButton.interactable = !equipped;
            }
            else
            {
                actionButtonText.text = equipped ? "EQUIPPED" : "EQUIP";
                actionButton.interactable = !equipped;
            }

            UpdateSlotSummary();
        }

        private async UniTaskVoid OnActionClicked()
        {
            if (selectedItem == null) return;

            bool purchased = shopService.IsPurchased(selectedItem.id);

            if (!purchased)
            {
                bool success = await shopService.PurchaseItemAsync(selectedItem.id, selectedItem.price);
                if (success)
                {
                    Debug.Log($"Purchased: {selectedItem.itemName}");
                }
            }
            else
            {
                bool equipped = shopService.IsEquipped(selectedItem.id, selectedItem.category, currentSelectedSlot);
                if (equipped)
                {
                    await shopService.UnequipItemAsync(selectedItem.id, selectedItem.category, currentSelectedSlot);
                }
                else
                {
                    await shopService.EquipItemAsync(selectedItem.id, selectedItem.category, currentSelectedSlot);
                }
            }

            UpdateUI();
        }

        private void UpdateCreditsDisplay()
        {
            if (creditsText != null)
            {
                creditsText.text = $"CREDITS: {shopService.GetCredits()}";
            }
        }

        private void RefreshItemStates()
        {
            foreach (var itemUi in activeItemObjects)
            {
                if (itemUi == null)
                {
                    continue;
                }

                var itemData = itemUi.ItemData;
                if (itemData == null)
                {
                    continue;
                }

                var purchased = shopService.IsPurchased(itemData.id);
                var equipped = shopService.IsEquipped(itemData.id, itemData.category, currentSelectedSlot);
                itemUi.RefreshState(purchased, equipped);
            }
        }

        private void UpdateSlotSummary()
        {
            if (slotSummaryText == null)
            {
                return;
            }

            var equippedItems = UserSaveManager.GetEquippedInstantItems();
            if (equippedItems == null || equippedItems.Length == 0)
            {
                slotSummaryText.text = "SLOT 1-3: EMPTY";
                return;
            }

            var lines = new List<string>();
            for (var index = 0; index < Mathf.Min(InstantItemSlotCount, equippedItems.Length); index++)
            {
                var itemId = equippedItems[index];
                var displayName = "EMPTY";
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    var catalogItem = ShopCatalogFactory.GetDefaultItemById(itemId);
                    displayName = catalogItem != null ? catalogItem.itemName : itemId;
                }

                lines.Add($"SLOT {index + 1}: {displayName}");
            }

            slotSummaryText.text = string.Join("\n", lines);
        }
    }
}
