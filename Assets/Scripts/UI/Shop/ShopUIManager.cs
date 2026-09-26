using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Zenject;
using Cysharp.Threading.Tasks;
using OpenGSCore;

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
        private int categoryRequestVersion;
        private bool actionInProgress;
        private List<ShopItemUI> activeItemObjects = new List<ShopItemUI>();
        private UnityAction[] slotButtonHandlers;
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
            if (weaponTab) weaponTab.onClick.AddListener(OnWeaponTabClicked);
            if (itemTab) itemTab.onClick.AddListener(OnItemTabClicked);
            if (boosterTab) boosterTab.onClick.AddListener(OnBoosterTabClicked);

            if (actionButton) actionButton.onClick.AddListener(OnActionButtonClicked);

            if (slotButtons != null)
            {
                slotButtonHandlers = new UnityAction[slotButtons.Length];
                for (int i = 0; i < slotButtons.Length; i++)
                {
                    int index = i;
                    if (slotButtons[i] != null)
                    {
                        UnityAction handler = () => SelectSlot(index);
                        slotButtonHandlers[i] = handler;
                        slotButtons[i].onClick.AddListener(handler);
                    }
                }
            }

            if (detailPanel) detailPanel.SetActive(false);
            if (slotSelectionRoot) slotSelectionRoot.SetActive(false);
            SelectSlot(0);
        }

        private void OnDestroy()
        {
            // 画面遷移中に完了した非同期処理が、破棄済みUIを更新しないよう無効化する。
            categoryRequestVersion++;
            actionInProgress = false;
            if (shopService != null)
                shopService.OnDataChanged -= UpdateUI;
            weaponTab?.onClick.RemoveListener(OnWeaponTabClicked);
            itemTab?.onClick.RemoveListener(OnItemTabClicked);
            boosterTab?.onClick.RemoveListener(OnBoosterTabClicked);
            actionButton?.onClick.RemoveListener(OnActionButtonClicked);
            if (slotButtons != null && slotButtonHandlers != null)
            {
                for (int i = 0; i < slotButtons.Length && i < slotButtonHandlers.Length; i++)
                {
                    if (slotButtons[i] != null && slotButtonHandlers[i] != null)
                    {
                        slotButtons[i].onClick.RemoveListener(slotButtonHandlers[i]);
                    }
                }
            }
            slotButtonHandlers = null;
        }

        private void OnWeaponTabClicked() => SwitchCategory(EShopCategory.Weapon).Forget();
        private void OnItemTabClicked() => SwitchCategory(EShopCategory.InstantItem).Forget();
        private void OnBoosterTabClicked() => SwitchCategory(EShopCategory.Booster).Forget();
        private void OnActionButtonClicked() => OnActionClicked().Forget();

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

        public async UniTask SwitchCategory(EShopCategory category)
        {
            if (shopService == null)
            {
                return;
            }

            currentCategory = category;
            selectedItem = null;
            var requestVersion = ++categoryRequestVersion;

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
            List<ShopItemData> items;
            try
            {
                items = await shopService.GetItemsAsync(category) ?? new List<ShopItemData>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShopUIManager] Failed to load category {category}: {ex}");
                return;
            }
            if (this == null || !isActiveAndEnabled || itemGridRoot == null ||
                requestVersion != categoryRequestVersion || currentCategory != category)
            {
                return;
            }

            if (itemPrefab == null || itemGridRoot == null)
            {
                return;
            }
            
            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

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
            if (item == null || shopService == null)
            {
                return;
            }

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

            if (item.category == EShopCategory.Character && shopService.IsPurchased(item.id))
            {
                ApplyCharacterSelectionSafe(item).Forget();
            }
        }

        private async UniTask ApplyCharacterSelectionSafe(ShopItemData item)
        {
            try
            {
                await ApplyCharacterSelection(item);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShopUIManager] Character selection failed: {ex}");
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
            else if (selectedItem.category == EShopCategory.Character)
            {
                actionButtonText.text = equipped ? "SELECTED" : "SELECT";
                actionButton.interactable = !equipped;
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

        private async UniTask OnActionClicked()
        {
            if (actionInProgress || selectedItem == null || shopService == null) return;

            actionInProgress = true;
            if (actionButton != null)
            {
                actionButton.interactable = false;
            }

            try
            {
                var actionItem = selectedItem;
                bool purchased = shopService.IsPurchased(actionItem.id);

                if (!purchased)
                {
                    bool success = await shopService.PurchaseItemAsync(actionItem.id, actionItem.price);
                    if (this == null || !isActiveAndEnabled) return;
                    if (success)
                    {
                        Debug.Log($"Purchased: {actionItem.itemName}");
                        if (actionItem.category == EShopCategory.Character)
                        {
                            await ApplyCharacterSelection(actionItem);
                            return;
                        }
                    }
                }
                else if (actionItem.category == EShopCategory.Character)
                {
                    await ApplyCharacterSelection(actionItem);
                    if (this == null || !isActiveAndEnabled) return;
                }
                else
                {
                    bool equipped = shopService.IsEquipped(actionItem.id, actionItem.category, currentSelectedSlot);
                    if (equipped)
                    {
                        await shopService.UnequipItemAsync(actionItem.id, actionItem.category, currentSelectedSlot);
                    }
                    else
                    {
                        await shopService.EquipItemAsync(actionItem.id, actionItem.category, currentSelectedSlot);
                    }

                    if (this == null || !isActiveAndEnabled) return;
                }

                UpdateUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShopUIManager] Shop action failed: {ex}");
            }
            finally
            {
                actionInProgress = false;
                if (this != null && isActiveAndEnabled)
                {
                    UpdateButtonState();
                }
            }
        }

        private async UniTask ApplyCharacterSelection(ShopItemData item)
        {
            if (item == null || item.category != EShopCategory.Character || shopService == null)
            {
                return;
            }

            if (!Enum.TryParse(item.id, true, out EPlayerCharacter character))
            {
                Debug.LogWarning($"[ShopUIManager] Failed to parse character id: {item.id}");
                return;
            }

            var success = await shopService.EquipItemAsync(item.id, item.category, 0);
            if (this == null || !isActiveAndEnabled || !success)
            {
                return;
            }

            if (GamePlayerManager.Instance == null)
            {
                Debug.LogWarning("[ShopUIManager] GamePlayerManager is not ready; character selection was not applied.");
                return;
            }

            GamePlayerManager.Instance.SetPlayerCharacter(character);
            UpdateUI();
        }

        private void UpdateCreditsDisplay()
        {
            if (creditsText != null)
            {
                creditsText.text = $"CREDITS: {(shopService != null ? shopService.GetCredits() : 0)}";
            }
        }

        private void RefreshItemStates()
        {
            if (shopService == null)
            {
                return;
            }

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

            var lines = new List<string>();
            for (var index = 0; index < InstantItemSlotCount; index++)
            {
                var itemId = shopService != null
                    ? shopService.GetEquippedItemId(EShopCategory.InstantItem, index)
                    : string.Empty;
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
