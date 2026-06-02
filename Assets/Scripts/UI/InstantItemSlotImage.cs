using OpenGSCore;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class InstantItemSlotImage : MonoBehaviour, IInstantItemSlotImage
    {
        [SerializeField] private Image slotImage;
        [SerializeField] private EInstantItemType currentType = EInstantItemType.None;

        private void Awake()
        {
            CacheReferences();
            RefreshImage();
        }

        private void Reset()
        {
            CacheReferences();
            RefreshImage();
        }

        private void OnValidate()
        {
            CacheReferences();
            RefreshImage();
        }

        public void SetInstantItemType(EInstantItemType type)
        {
            currentType = type;
            RefreshImage();
        }

        public void Clear()
        {
            SetInstantItemType(EInstantItemType.None);
        }

        private void CacheReferences()
        {
            if (slotImage == null)
            {
                slotImage = GetComponent<Image>();
            }
        }

        private void RefreshImage()
        {
            if (slotImage == null)
            {
                return;
            }

            var isEmpty = currentType == EInstantItemType.None;
            var sprite = isEmpty ? null : InstantItemVisualResolver.GetIcon(currentType);
            slotImage.sprite = sprite;

            var color = slotImage.color;
            color.a = (isEmpty || sprite == null) ? 0f : 1f;
            slotImage.color = color;
        }
    }
}
