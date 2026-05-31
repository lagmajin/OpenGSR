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
