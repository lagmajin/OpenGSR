using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    public class WeaponSelectDialogSlot : MonoBehaviour
    {
        [SerializeField] public Image iconImage;
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] public TextMeshProUGUI detailText;
        [SerializeField] public Button selectButton;
        [SerializeField] public GameObject selectedMarker;
        [SerializeField] public GameObject bannedMarker;
        [SerializeField] public GameObject equippedMarker;
        [SerializeField] public Image equippedIconImage;
        [SerializeField] public CanvasGroup canvasGroup;

        private Enum weaponType;

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        public void CacheReferences()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            iconImage ??= FindChild<Image>("Icon");
            nameText ??= FindChild<TextMeshProUGUI>("NameText");
            detailText ??= FindChild<TextMeshProUGUI>("DetailText");
            selectButton ??= FindChild<Button>("SelectButton");
            selectedMarker ??= FindChildGameObject("SelectedMarker");
            bannedMarker ??= FindChildGameObject("BannedMarker");
            equippedMarker ??= FindChildGameObject("EquippedMarker");
            equippedIconImage ??= FindChild<Image>("EquippedIcon");
        }

        public void SetWeaponType(Enum type)
        {
            weaponType = type;
            RefreshWeaponVisual();
        }

        public void SetDetailText(string detail)
        {
            if (detailText != null)
            {
                detailText.text = detail ?? string.Empty;
            }
        }

        public void SetVisualState(bool isEmpty, bool isBanned, bool isSelected, bool isEquipped, Sprite equippedIcon,
            Color emptyColor, Color bannedColor, Color selectedColor, Color normalColor)
        {
            if (iconImage != null)
            {
                iconImage.color = isEmpty
                    ? emptyColor
                    : isBanned
                        ? bannedColor
                        : isSelected
                            ? selectedColor
                            : normalColor;

                if (iconImage.sprite == null)
                {
                    iconImage.color = Color.clear;
                }
            }

            if (nameText != null)
            {
                nameText.color = isEmpty
                    ? emptyColor
                    : isBanned
                        ? bannedColor
                        : isSelected
                            ? selectedColor
                            : normalColor;
            }

            if (detailText != null)
            {
                detailText.color = isEmpty
                    ? emptyColor
                    : isBanned
                        ? bannedColor
                        : isSelected
                            ? selectedColor
                            : normalColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = isEmpty ? 0.35f : 1f;
                canvasGroup.interactable = !isEmpty && !isBanned;
                canvasGroup.blocksRaycasts = !isEmpty && !isBanned;
            }

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(isSelected && !isEmpty);
            }

            if (bannedMarker != null)
            {
                bannedMarker.SetActive(isBanned);
            }

            if (equippedMarker != null)
            {
                equippedMarker.SetActive(isEquipped && !isEmpty && iconImage != null);
            }

            if (equippedIconImage != null)
            {
                equippedIconImage.sprite = equippedIcon;
                equippedIconImage.color = isEquipped
                    ? new Color(0.9f, 1f, 0.9f, 1f)
                    : Color.clear;

                if (equippedIconImage.sprite == null)
                {
                    equippedIconImage.color = Color.clear;
                }
            }
        }

        private void RefreshWeaponVisual()
        {
            if (iconImage == null)
            {
                return;
            }

            var weaponKey = weaponType?.ToString();
            if (string.IsNullOrWhiteSpace(weaponKey) || string.Equals(weaponKey, "None", StringComparison.OrdinalIgnoreCase))
            {
                iconImage.sprite = null;
                if (nameText != null)
                {
                    nameText.text = string.Empty;
                }
                return;
            }

            iconImage.sprite = WeaponVisualResolver.GetSelectionSprite(weaponType);
            if (iconImage.sprite == null)
            {
                iconImage.color = Color.clear;
            }
            if (nameText != null)
            {
                nameText.text = WeaponVisualResolver.GetDisplayName(weaponType);
            }

            if (detailText != null && string.IsNullOrWhiteSpace(detailText.text))
            {
                detailText.text = WeaponVisualResolver.GetDisplayName(weaponType);
            }
        }

        private T FindChild<T>(string childName) where T : Component
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private GameObject FindChildGameObject(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.gameObject : null;
        }
    }
}
