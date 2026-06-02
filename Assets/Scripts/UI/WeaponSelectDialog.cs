using System;
using System.Collections;
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

        [Header("Auto Close")]
        [SerializeField, Min(0f)] private float autoCloseDelaySeconds = 5f;

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
        private Coroutine autoCloseCoroutine;

        private void Awake()
        {
            ResolveDependencies();
            SetupListeners();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            ValidateSlotConfiguration();
            RefreshData();
            RefreshUI();
            StartAutoCloseTimer();
        }

        private void OnDisable()
        {
            CancelAutoCloseTimer();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            ValidateSlotConfiguration();
            RefreshData();
            RefreshUI();
            StartAutoCloseTimer();
        }

        public void Hide()
        {
            Close();
        }

        private void StartAutoCloseTimer()
        {
            CancelAutoCloseTimer();

            if (autoCloseDelaySeconds <= 0f)
            {
                return;
            }

            autoCloseCoroutine = StartCoroutine(AutoCloseCoroutine());
        }

        private void CancelAutoCloseTimer()
        {
            if (autoCloseCoroutine == null)
            {
                return;
            }

            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        private IEnumerator AutoCloseCoroutine()
        {
            yield return new WaitForSeconds(autoCloseDelaySeconds);
            autoCloseCoroutine = null;
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

        private void ValidateSlotConfiguration()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null)
                {
                    Debug.LogWarning("[WeaponSelectDialog] CanvasGroup is missing.");
                }
            }

            if (leftSlots == null || leftSlots.Length != SlotCount)
            {
                Debug.LogWarning($"[WeaponSelectDialog] leftSlots should contain {SlotCount} slots.");
            }

            if (rightSlots == null || rightSlots.Length != SlotCount)
            {
                Debug.LogWarning($"[WeaponSelectDialog] rightSlots should contain {SlotCount} slots.");
            }

            if (leftSlots != null && leftSlots.Any(slot => slot == null))
            {
                Debug.LogWarning("[WeaponSelectDialog] leftSlots contains null entries.");
            }

            if (rightSlots != null && rightSlots.Any(slot => slot == null))
            {
                Debug.LogWarning("[WeaponSelectDialog] rightSlots contains null entries.");
            }
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
            var displayName = isEmpty ? "EMPTY" : WeaponVisualResolver.GetDisplayName(weapon);
            var isEquipped = !isEmpty && UserSaveManager.IsFavoriteWeapon(weapon.ToString());
            var equippedIcon = isEquipped ? WeaponVisualResolver.GetSelectionSprite(weapon) : null;
            var isBanned = !isEmpty && IsBanned(weapon);
            var isSelected = index == selectedLeftIndex;

            slot.SetWeaponType(weapon);
            slot.SetDetailText(displayName);
            slot.SetVisualState(isEmpty, isBanned, isSelected, isEquipped, equippedIcon, emptyColor, bannedColor, selectedColor, normalColor);

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
            var displayName = isEmpty ? "EMPTY" : WeaponVisualResolver.GetDisplayName(weapon);
            var isEquipped = false;
            var equippedIcon = isEquipped ? WeaponVisualResolver.GetSelectionSprite(weapon) : null;
            var isBanned = !isEmpty && IsBanned(weapon);
            var isSelected = index == selectedRightIndex;

            slot.SetWeaponType(weapon);
            slot.SetDetailText(displayName);
            slot.SetVisualState(isEmpty, isBanned, isSelected, isEquipped, equippedIcon, emptyColor, bannedColor, selectedColor, normalColor);

            if (slot.selectButton != null)
            {
                slot.selectButton.interactable = allowRightSideBanToggle && !isEmpty;
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
            CancelAutoCloseTimer();

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

    }
}
