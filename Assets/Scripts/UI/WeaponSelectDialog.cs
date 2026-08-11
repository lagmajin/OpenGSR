using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private const int LeftSlotCount = 5;
        private const int RightSlotCount = 5;

        [Header("Dialog")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Left Side")]
        [SerializeField] private WeaponSelectDialogSlot[] leftSlots = new WeaponSelectDialogSlot[LeftSlotCount];
        [SerializeField] private EWeaponType[] basicLeftWeapons =
        {
            EWeaponType.AK47,
            EWeaponType.MP5,
            EWeaponType.Scout,
            EWeaponType.MG42,
            EWeaponType.Glock,
        };

        [Header("Right Side")]
        [SerializeField] private WeaponSelectDialogSlot[] rightSlots = new WeaponSelectDialogSlot[RightSlotCount];
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

        [Header("Click Behavior")]
        [SerializeField] private bool singleClickFocusEnabled = true;
        [SerializeField] private bool doubleClickCloseEnabled = true;
        [SerializeField] private bool equipOnSingleClick = true;

        public Action<IReadOnlyList<EWeaponType>> OnLeftSelectionConfirmed;
        public Action<IReadOnlyList<eWeaponType>> OnRightSelectionConfirmed;
        public Action OnDialogClosed;

        private readonly List<EWeaponType> leftWeapons = new List<EWeaponType>(LeftSlotCount);
        private readonly List<eWeaponType> rightWeapons = new List<eWeaponType>(RightSlotCount);
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
                leftWeapons.AddRange(weapons.Where(w => w != EWeaponType.None).Take(LeftSlotCount));
            }

            while (leftWeapons.Count < LeftSlotCount)
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
                rightWeapons.AddRange(weapons.Where(w => w != eWeaponType.None).Take(RightSlotCount));
            }

            while (rightWeapons.Count < RightSlotCount)
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

            if (leftSlots == null || leftSlots.Length != LeftSlotCount)
            {
                Debug.LogWarning($"[WeaponSelectDialog] leftSlots should contain {LeftSlotCount} slots.");
            }

            if (rightSlots == null || rightSlots.Length != RightSlotCount)
            {
                Debug.LogWarning($"[WeaponSelectDialog] rightSlots should contain {RightSlotCount} slots.");
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

                var relay = slot.selectButton.GetComponent<WeaponSelectDialogSlotClickRelay>();
                if (relay == null)
                {
                    relay = slot.selectButton.gameObject.AddComponent<WeaponSelectDialogSlotClickRelay>();
                }

                relay.Configure(this, slotIndex, isLeft);
            }
        }

        private void RefreshData()
        {
            leftWeapons.Clear();
            if (basicLeftWeapons != null)
            {
                leftWeapons.AddRange(basicLeftWeapons
                    .Where(w => w != EWeaponType.None)
                    .Distinct()
                    .Take(LeftSlotCount));
            }

            while (leftWeapons.Count < LeftSlotCount)
            {
                leftWeapons.Add(EWeaponType.None);
            }

            if (rightWeapons.Count == 0)
            {
                rightWeapons.Clear();
                if (fallbackRightWeapons != null && fallbackRightWeapons.Length > 0)
                {
                    rightWeapons.AddRange(fallbackRightWeapons.Take(RightSlotCount));
                }

                while (rightWeapons.Count < RightSlotCount)
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

        public void OnLeftSlotClicked(int index)
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
            if (equipOnSingleClick)
            {
                EquipWeaponForPlayer(weapon);
            }
            if (!singleClickFocusEnabled)
            {
                UserSaveManager.ToggleFavoriteWeapon(weapon.ToString());
                RefreshData();
                RefreshUI();
            }
        }

        private bool EquipWeaponForPlayer(EWeaponType weapon)
        {
            var player = FindFirstObjectByType<PlayerAgent>();
            if (player == null)
            {
                Debug.LogWarning("[WeaponSelectDialog] PlayerAgent is not present; weapon remains selected in the dialog.");
                return false;
            }

            if (player.EquipWeaponType(weapon))
            {
                statusText?.SetText($"EQUIPPED: {WeaponVisualResolver.GetDisplayName(weapon)}");
                return true;
            }

            return false;
        }

        public void OnRightSlotClicked(int index)
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
            RefreshUI();
            if (!singleClickFocusEnabled)
            {
                matchRoomManager.WeaponLimit.Toggle(weapon);
                UpdateStatusText();
            }
        }

        public void OnLeftSlotDoubleClicked(int index)
        {
            SelectLeftSlot(index);
            if (doubleClickCloseEnabled && index >= 0 && index < leftWeapons.Count)
            {
                var weapon = leftWeapons[index];
                if (weapon != EWeaponType.None && !IsBanned(weapon) && EquipWeaponForPlayer(weapon))
                {
                    Close();
                }
            }
        }

        public void OnRightSlotDoubleClicked(int index)
        {
            SelectRightSlot(index);
            if (doubleClickCloseEnabled && index >= 0 && index < rightWeapons.Count)
            {
                var weapon = rightWeapons[index];
                var playerWeapon = ConvertToPlayerWeapon(weapon);
                if (playerWeapon != EWeaponType.None && !IsBanned(weapon) && EquipWeaponForPlayer(playerWeapon))
                {
                    Close();
                }
            }
        }

        private EWeaponType ConvertToPlayerWeapon(eWeaponType weapon)
        {
            return weapon switch
            {
                eWeaponType.DE => EWeaponType.DesertEagle,
                eWeaponType.FN_P90 => EWeaponType.FnP90,
                eWeaponType.FNMinimi_SAW => EWeaponType.FNMinimiSaw,
                eWeaponType.SteyAug => EWeaponType.SteyrAug,
                eWeaponType.ChirstmasGun => EWeaponType.ChristmasGun,
                _ => Enum.TryParse(weapon.ToString(), true, out EWeaponType parsed) ? parsed : EWeaponType.None
            };
        }

        public void SetClickBehavior(bool enableSingleClickFocus, bool enableDoubleClickClose)
        {
            singleClickFocusEnabled = enableSingleClickFocus;
            doubleClickCloseEnabled = enableDoubleClickClose;
        }

        private void SelectLeftSlot(int index)
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
        }

        private void SelectRightSlot(int index)
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
            RefreshUI();
        }

        private void ConfirmAndClose()
        {
            if (selectedLeftIndex >= 0 && selectedLeftIndex < leftWeapons.Count)
            {
                var leftWeapon = leftWeapons[selectedLeftIndex];
                if (leftWeapon != EWeaponType.None && !IsBanned(leftWeapon))
                {
                    UserSaveManager.ToggleFavoriteWeapon(leftWeapon.ToString());
                    RefreshData();
                }
            }

            if (selectedRightIndex >= 0 && selectedRightIndex < rightWeapons.Count && matchRoomManager != null)
            {
                var rightWeapon = rightWeapons[selectedRightIndex];
                if (rightWeapon != eWeaponType.None)
                {
                    matchRoomManager.WeaponLimit.Toggle(rightWeapon);
                }
            }

            RefreshUI();
            UpdateStatusText();
            Close();
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
            statusText.text = $"LEFT {leftCount}/{LeftSlotCount}  RIGHT {rightCount}/{RightSlotCount}";
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

    public sealed class WeaponSelectDialogSlotClickRelay : MonoBehaviour, IPointerClickHandler
    {
        private WeaponSelectDialog dialog;
        private int slotIndex;
        private bool isLeft;

        public void Configure(WeaponSelectDialog owner, int index, bool leftSide)
        {
            dialog = owner;
            slotIndex = index;
            isLeft = leftSide;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dialog == null || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (eventData.clickCount >= 2)
            {
                if (isLeft)
                {
                    dialog.OnLeftSlotDoubleClicked(slotIndex);
                }
                else
                {
                    dialog.OnRightSlotDoubleClicked(slotIndex);
                }
                return;
            }

            if (isLeft)
            {
                dialog.OnLeftSlotClicked(slotIndex);
            }
            else
            {
                dialog.OnRightSlotClicked(slotIndex);
            }
        }
    }
}
