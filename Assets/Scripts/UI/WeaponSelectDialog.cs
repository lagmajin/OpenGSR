using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// Weapon selection dialog.
    /// Left side shows favorite/shop weapons, right side shows representative weakest weapons.
    /// Banned weapons are rendered in gray and disabled.
    /// </summary>
    public class WeaponSelectDialog : MonoBehaviour
    {
        private const int SlotCount = 5;

        [Header("Dialog")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Left Side")]
        [SerializeField] private WeaponSelectDialogSlot[] leftSlots = new WeaponSelectDialogSlot[SlotCount];
        [SerializeField] private bool autoLoadFavoriteWeapons = true;

        [Header("Right Side")]
        [SerializeField] private WeaponSelectDialogSlot[] rightSlots = new WeaponSelectDialogSlot[SlotCount];
        [SerializeField] private bool allowRightSideBanToggle = true;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.25f, 1f);
        [SerializeField] private Color bannedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

        [Header("Fallback Right Weapons")]
        [SerializeField] private eWeaponType[] fallbackRightWeapons =
        {
            eWeaponType.Glock,
            eWeaponType.MP5,
            eWeaponType.M16,
            eWeaponType.Scout,
            eWeaponType.MG42,
        };

        public Action<IReadOnlyList<EWeaponType>> OnLeftSelectionConfirmed;
        public Action<IReadOnlyList<eWeaponType>> OnRightSelectionConfirmed;
        public Action OnDialogClosed;

        private readonly List<EWeaponType> leftWeapons = new List<EWeaponType>(SlotCount);
        private readonly List<eWeaponType> rightWeapons = new List<eWeaponType>(SlotCount);
        private readonly Dictionary<EWeaponType, eWeaponType> weaponMapCache = new Dictionary<EWeaponType, eWeaponType>();
        private MatchRoomManager matchRoomManager;
        private int selectedLeftIndex = -1;
        private int selectedRightIndex = -1;
        private bool listenersRegistered;

        private void Awake()
        {
            ResolveDependencies();
            SetupListeners();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            EnsureRuntimeLayout();
            RefreshData();
            RefreshUI();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureRuntimeLayout();
            RefreshData();
            RefreshUI();
        }

        public void Hide()
        {
            Close();
        }

        public void SetLeftWeapons(IEnumerable<EWeaponType> weapons)
        {
            leftWeapons.Clear();
            if (weapons != null)
            {
                leftWeapons.AddRange(weapons.Where(w => w != EWeaponType.None).Take(SlotCount));
            }

            while (leftWeapons.Count < SlotCount)
            {
                leftWeapons.Add(EWeaponType.None);
            }

            RefreshUI();
        }

        public void SetRightWeapons(IEnumerable<eWeaponType> weapons)
        {
            rightWeapons.Clear();
            if (weapons != null)
            {
                rightWeapons.AddRange(weapons.Where(w => w != eWeaponType.None).Take(SlotCount));
            }

            while (rightWeapons.Count < SlotCount)
            {
                rightWeapons.Add(eWeaponType.None);
            }

            RefreshUI();
        }

        private void ResolveDependencies()
        {
            if (matchRoomManager == null)
            {
                try
                {
                    matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
                }
                catch
                {
                    matchRoomManager = null;
                }
            }

            group ??= GetComponent<CanvasGroup>();
        }

        private void EnsureRuntimeLayout()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (leftSlots != null && leftSlots.Length == SlotCount && rightSlots != null && rightSlots.Length == SlotCount)
            {
                if (leftSlots.All(slot => slot != null && slot.iconImage != null) &&
                    rightSlots.All(slot => slot != null && slot.iconImage != null))
                {
                    return;
                }
            }

            BuildRuntimeLayout();
        }

        private void BuildRuntimeLayout()
        {
            var root = FindChild("RuntimeLayout");
            if (root == null)
            {
                root = CreateRectTransform("RuntimeLayout", transform as RectTransform ?? GetComponent<RectTransform>());
                StretchFull(root);
            }

            var header = FindChild("Header");
            if (header == null)
            {
                header = CreateRectTransform("Header", root);
                ConfigureHeader(header);
            }

            var content = FindChild("Content");
            if (content == null)
            {
                content = CreateRectTransform("Content", root);
                ConfigureContent(content);
            }

            var leftPanel = FindChild("LeftPanel");
            if (leftPanel == null)
            {
                leftPanel = CreateRectTransform("LeftPanel", content);
                ConfigureSidePanel(leftPanel, true);
            }

            var rightPanel = FindChild("RightPanel");
            if (rightPanel == null)
            {
                rightPanel = CreateRectTransform("RightPanel", content);
                ConfigureSidePanel(rightPanel, false);
            }

            var bottom = FindChild("BottomBar");
            if (bottom == null)
            {
                bottom = CreateRectTransform("BottomBar", root);
                ConfigureBottomBar(bottom);
            }

            statusText ??= CreateText(bottom, "StatusText", "LEFT 0/5  RIGHT 0/5", 24, TextAlignmentOptions.Left);

            leftSlots = BuildRuntimeSlots(leftPanel, "LeftSlot", true);
            rightSlots = BuildRuntimeSlots(rightPanel, "RightSlot", false);
        }

        private WeaponSelectDialogSlot[] BuildRuntimeSlots(RectTransform parent, string prefix, bool isLeft)
        {
            var slots = new WeaponSelectDialogSlot[SlotCount];
            for (var index = 0; index < SlotCount; index++)
            {
                var slotRoot = CreateRectTransform($"{prefix}{index + 1}", parent);
                ConfigureSlotRoot(slotRoot, index);

                var view = slotRoot.gameObject.AddComponent<WeaponSelectDialogSlot>();
                view.canvasGroup = slotRoot.gameObject.GetComponent<CanvasGroup>() ?? slotRoot.gameObject.AddComponent<CanvasGroup>();
                view.CacheReferences();

                var background = CreateImage(slotRoot, "Background", new Color(0.12f, 0.12f, 0.12f, 0.92f));
                StretchFull(background.rectTransform);

                var icon = CreateImage(slotRoot, "Icon", Color.white);
                ConfigureIcon(icon.rectTransform);
                view.iconImage = icon;

                view.nameText = CreateText(slotRoot, "NameText", isLeft ? $"LEFT {index + 1}" : $"RIGHT {index + 1}", 22, TextAlignmentOptions.Left);
                view.detailText = CreateText(slotRoot, "DetailText", string.Empty, 16, TextAlignmentOptions.Left);

                var selectButton = CreateButton(slotRoot, "SelectButton", string.Empty, Vector2.zero, Vector2.zero);
                StretchFull(selectButton.GetComponent<RectTransform>());
                selectButton.targetGraphic = background;
                selectButton.transition = Selectable.Transition.ColorTint;
                selectButton.colors = GetButtonColors(isLeft);
                view.selectButton = selectButton;

                view.selectedMarker = CreateMarker(slotRoot, "SelectedMarker", new Color(1f, 0.9f, 0.2f, 0.12f));
                view.bannedMarker = CreateMarker(slotRoot, "BannedMarker", new Color(0.25f, 0.25f, 0.25f, 0.4f));

                slots[index] = view;
            }

            return slots;
        }

        private static void ConfigureHeader(RectTransform header)
        {
            header.anchorMin = new Vector2(0.05f, 0.88f);
            header.anchorMax = new Vector2(0.95f, 0.98f);
            header.offsetMin = Vector2.zero;
            header.offsetMax = Vector2.zero;
        }

        private static void ConfigureContent(RectTransform content)
        {
            content.anchorMin = new Vector2(0.05f, 0.18f);
            content.anchorMax = new Vector2(0.95f, 0.84f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
        }

        private static void ConfigureSidePanel(RectTransform panel, bool isLeft)
        {
            panel.anchorMin = isLeft ? new Vector2(0f, 0f) : new Vector2(0.52f, 0f);
            panel.anchorMax = isLeft ? new Vector2(0.48f, 1f) : new Vector2(1f, 1f);
            panel.offsetMin = new Vector2(10f, 10f);
            panel.offsetMax = new Vector2(-10f, -10f);
        }

        private static void ConfigureBottomBar(RectTransform bottom)
        {
            bottom.anchorMin = new Vector2(0.05f, 0.03f);
            bottom.anchorMax = new Vector2(0.95f, 0.14f);
            bottom.offsetMin = Vector2.zero;
            bottom.offsetMax = Vector2.zero;
        }

        private static void ConfigureSlotRoot(RectTransform slotRoot, int index)
        {
            slotRoot.anchorMin = new Vector2(0f, 1f - ((index + 1) * 0.18f));
            slotRoot.anchorMax = new Vector2(1f, 1f - (index * 0.18f));
            slotRoot.offsetMin = new Vector2(0f, 4f);
            slotRoot.offsetMax = new Vector2(0f, -4f);
        }

        private static void ConfigureIcon(RectTransform icon)
        {
            icon.anchorMin = new Vector2(0.02f, 0.15f);
            icon.anchorMax = new Vector2(0.18f, 0.85f);
            icon.offsetMin = Vector2.zero;
            icon.offsetMax = Vector2.zero;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private RectTransform CreateRectTransform(string objectName, RectTransform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            return rect;
        }

        private Image CreateImage(RectTransform parent, string objectName, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private TextMeshProUGUI CreateText(RectTransform parent, string objectName, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }
            return tmp;
        }

        private Button CreateButton(RectTransform parent, string objectName, string label, Vector2 size, Vector2 anchoredPosition)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

            var button = go.GetComponent<Button>();
            var labelText = CreateText(rect, "Label", label, 22, TextAlignmentOptions.Center);
            StretchFull(labelText.rectTransform);
            labelText.raycastTarget = false;

            return button;
        }

        private GameObject CreateMarker(RectTransform parent, string objectName, Color color)
        {
            var marker = CreateImage(parent, objectName, color);
            StretchFull(marker.rectTransform);
            return marker.gameObject;
        }

        private ColorBlock GetButtonColors(bool isLeft)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = isLeft ? new Color(0.22f, 0.28f, 0.38f, 1f) : new Color(0.34f, 0.22f, 0.22f, 1f);
            colors.highlightedColor = colors.normalColor + new Color(0.08f, 0.08f, 0.08f, 0f);
            colors.selectedColor = selectedColor;
            colors.pressedColor = colors.normalColor * 0.85f;
            colors.disabledColor = bannedColor;
            return colors;
        }

        private RectTransform FindChild(string objectName)
        {
            foreach (Transform child in transform)
            {
                if (child != null && child.name == objectName)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private void SetupListeners()
        {
            if (listenersRegistered)
            {
                return;
            }

            RegisterSlotListeners(leftSlots, true);
            RegisterSlotListeners(rightSlots, false);

            listenersRegistered = true;
        }

        private void RegisterSlotListeners(WeaponSelectDialogSlot[] slots, bool isLeft)
        {
            if (slots == null)
            {
                return;
            }

            for (var index = 0; index < slots.Length; index++)
            {
                var slotIndex = index;
                var slot = slots[index];
                if (slot?.selectButton == null)
                {
                    continue;
                }

                if (isLeft)
                {
                    slot.selectButton.onClick.AddListener(() => OnLeftSlotClicked(slotIndex));
                }
                else
                {
                    slot.selectButton.onClick.AddListener(() => OnRightSlotClicked(slotIndex));
                }
            }
        }

        private void RefreshData()
        {
            if (autoLoadFavoriteWeapons)
            {
                var favorites = FavoriteWeaponMemoryStorage.Load();
                leftWeapons.Clear();
                foreach (var weapon in favorites.Take(SlotCount))
                {
                    leftWeapons.Add(weapon);
                }

                while (leftWeapons.Count < SlotCount)
                {
                    leftWeapons.Add(EWeaponType.None);
                }
            }

            if (rightWeapons.Count == 0)
            {
                rightWeapons.Clear();
                if (fallbackRightWeapons != null && fallbackRightWeapons.Length > 0)
                {
                    rightWeapons.AddRange(fallbackRightWeapons.Take(SlotCount));
                }

                while (rightWeapons.Count < SlotCount)
                {
                    rightWeapons.Add(eWeaponType.None);
                }
            }
        }

        private void RefreshUI()
        {
            SyncSlotGroup(leftSlots, true);
            SyncSlotGroup(rightSlots, false);
            UpdateStatusText();
        }

        private void SyncSlotGroup(WeaponSelectDialogSlot[] slots, bool isLeft)
        {
            if (slots == null)
            {
                return;
            }

            for (var index = 0; index < slots.Length; index++)
            {
                var slot = slots[index];
                if (slot == null)
                {
                    continue;
                }

                if (isLeft)
                {
                    var weapon = index < leftWeapons.Count ? leftWeapons[index] : EWeaponType.None;
                    UpdateLeftSlot(slot, weapon, index);
                }
                else
                {
                    var weapon = index < rightWeapons.Count ? rightWeapons[index] : eWeaponType.None;
                    UpdateRightSlot(slot, weapon, index);
                }
            }
        }

        private void UpdateLeftSlot(WeaponSelectDialogSlot slot, EWeaponType weapon, int index)
        {
            if (slot == null)
            {
                return;
            }

            var isEmpty = weapon == EWeaponType.None;
            var displayName = isEmpty ? "EMPTY" : GetDisplayName(weapon);
            var icon = isEmpty ? null : ResolveWeaponSprite(weapon.ToString());
            var isBanned = !isEmpty && IsBanned(weapon);
            var isSelected = index == selectedLeftIndex;

            ApplySlotVisual(slot, icon, displayName, isEmpty ? string.Empty : weapon.ToString(), isEmpty, isBanned, isSelected);

            if (slot.selectButton != null)
            {
                slot.selectButton.interactable = !isEmpty && !isBanned;
            }
        }

        private void UpdateRightSlot(WeaponSelectDialogSlot slot, eWeaponType weapon, int index)
        {
            if (slot == null)
            {
                return;
            }

            var isEmpty = weapon == eWeaponType.None;
            var displayName = isEmpty ? "EMPTY" : GetDisplayName(weapon);
            var icon = isEmpty ? null : ResolveWeaponSprite(weapon.ToString());
            var isBanned = !isEmpty && IsBanned(weapon);
            var isSelected = index == selectedRightIndex;

            ApplySlotVisual(slot, icon, displayName, isEmpty ? string.Empty : weapon.ToString(), isEmpty, isBanned, isSelected);

            if (slot.selectButton != null)
            {
                slot.selectButton.interactable = allowRightSideBanToggle && !isEmpty;
            }
        }

        private void ApplySlotVisual(
            WeaponSelectDialogSlot slot,
            Sprite icon,
            string displayName,
            string detail,
            bool isEmpty,
            bool isBanned,
            bool isSelected)
        {
            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = icon;
                slot.iconImage.color = isEmpty
                    ? emptyColor
                    : isBanned
                        ? bannedColor
                        : isSelected
                            ? selectedColor
                            : normalColor;
            }

            if (slot.nameText != null)
            {
                slot.nameText.text = displayName;
                slot.nameText.color = isEmpty
                    ? emptyColor
                    : isBanned
                        ? bannedColor
                        : isSelected
                            ? selectedColor
                            : normalColor;
            }

            if (slot.detailText != null)
            {
                slot.detailText.text = detail;
                slot.detailText.color = isEmpty
                    ? emptyColor
                    : isBanned
                        ? bannedColor
                        : isSelected
                            ? selectedColor
                            : normalColor;
            }

            if (slot.canvasGroup != null)
            {
                slot.canvasGroup.alpha = isEmpty ? 0.35f : 1f;
                slot.canvasGroup.interactable = !isEmpty && !isBanned;
                slot.canvasGroup.blocksRaycasts = !isEmpty && !isBanned;
            }

            if (slot.selectedMarker != null)
            {
                slot.selectedMarker.SetActive(isSelected && !isEmpty);
            }

            if (slot.bannedMarker != null)
            {
                slot.bannedMarker.SetActive(isBanned);
            }
        }

        private void OnLeftSlotClicked(int index)
        {
            if (index < 0 || index >= leftWeapons.Count)
            {
                return;
            }

            var weapon = leftWeapons[index];
            if (weapon == EWeaponType.None || IsBanned(weapon))
            {
                return;
            }

            selectedLeftIndex = index;
            RefreshUI();

            UserSaveManager.ToggleFavoriteWeapon(weapon.ToString());
            RefreshData();
            RefreshUI();
        }

        private void OnRightSlotClicked(int index)
        {
            if (!allowRightSideBanToggle)
            {
                return;
            }

            if (index < 0 || index >= rightWeapons.Count)
            {
                return;
            }

            var weapon = rightWeapons[index];
            if (weapon == eWeaponType.None || matchRoomManager == null)
            {
                return;
            }

            selectedRightIndex = index;
            matchRoomManager.WeaponLimit.Toggle(weapon);
            RefreshUI();
            UpdateStatusText();
        }

        private void Close()
        {
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            OnDialogClosed?.Invoke();
            gameObject.SetActive(false);
        }

        private void UpdateStatusText()
        {
            if (statusText == null)
            {
                return;
            }

            var leftCount = leftWeapons.Count(w => w != EWeaponType.None);
            var rightCount = rightWeapons.Count(w => w != eWeaponType.None);
            statusText.text = $"LEFT {leftCount}/{SlotCount}  RIGHT {rightCount}/{SlotCount}";
        }

        private Sprite ResolveWeaponSprite(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            var catalogItem = ShopCatalogFactory.GetDefaultItemById(weaponId);
            if (catalogItem != null && catalogItem.icon != null)
            {
                return catalogItem.icon;
            }

            var resourceNames = GetResourceAliases(weaponId);
            var resourcePaths = new List<string>();
            foreach (var name in resourceNames)
            {
                resourcePaths.Add($"Weapons/{name}");
                resourcePaths.Add($"UI/Weapons/{name}");
            }

            foreach (var path in resourcePaths)
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private bool IsBanned(EWeaponType weapon)
        {
            if (matchRoomManager == null)
            {
                return false;
            }

            return matchRoomManager.WeaponLimit.IsBanned(ConvertToLimitWeapon(weapon));
        }

        private bool IsBanned(eWeaponType weapon)
        {
            if (matchRoomManager == null)
            {
                return false;
            }

            return matchRoomManager.WeaponLimit.IsBanned(weapon);
        }

        private eWeaponType ConvertToLimitWeapon(EWeaponType weapon)
        {
            if (weaponMapCache.TryGetValue(weapon, out var cached))
            {
                return cached;
            }

            var mapped = weapon switch
            {
                EWeaponType.DesertEagle => eWeaponType.DE,
                EWeaponType.FnP90 => eWeaponType.FN_P90,
                EWeaponType.FNMinimiSaw => eWeaponType.FNMinimi_SAW,
                EWeaponType.SteyrAug => eWeaponType.SteyAug,
                EWeaponType.ChristmasGun => eWeaponType.ChirstmasGun,
                _ => Enum.TryParse(weapon.ToString(), true, out eWeaponType parsed) ? parsed : eWeaponType.None
            };

            weaponMapCache[weapon] = mapped;
            return mapped;
        }

        private static string GetDisplayName(EWeaponType weapon)
        {
            return weapon switch
            {
                EWeaponType.FnP90 => "FN_P90",
                EWeaponType.FNMinimiSaw => "FNMinimi_SAW",
                EWeaponType.SteyrAug => "SteyrAug",
                EWeaponType.ChristmasGun => "ChristmasGun",
                EWeaponType.DesertEagle => "DE",
                _ => weapon.ToString()
            };
        }

        private static string GetDisplayName(eWeaponType weapon)
        {
            return weapon switch
            {
                eWeaponType.FN_P90 => "FN_P90",
                eWeaponType.FNMinimi_SAW => "FNMinimi_SAW",
                eWeaponType.SteyAug => "SteyAug",
                eWeaponType.ChirstmasGun => "ChirstmasGun",
                eWeaponType.DE => "DE",
                _ => weapon.ToString()
            };
        }

        private static string NormalizeResourceName(string weaponId)
        {
            return weaponId
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty);
        }

        private static IEnumerable<string> GetResourceAliases(string weaponId)
        {
            yield return weaponId;
            yield return NormalizeResourceName(weaponId);

            switch (weaponId)
            {
                case "DesertEagle":
                    yield return "DE";
                    break;
                case "FnP90":
                    yield return "FN_P90";
                    break;
                case "FNMinimiSaw":
                    yield return "FNMinimi_SAW";
                    break;
                case "SteyrAug":
                    yield return "SteyAug";
                    break;
                case "ChristmasGun":
                    yield return "ChirstmasGun";
                    break;
            }
        }

    }
}
