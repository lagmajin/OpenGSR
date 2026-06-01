using OpenGSCore;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class GrenadeSlotImage : MonoBehaviour, IGrenadeSlotImage
    {
        [SerializeField] private Image slotImage;
        [SerializeField] private EGrenadeType currentType = EGrenadeType.Empty;

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

        public void SetGrenadeType(EGrenadeType type)
        {
            currentType = type;
            RefreshImage();
        }

        public void Clear()
        {
            SetGrenadeType(EGrenadeType.Empty);
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

            slotImage.sprite = currentType == EGrenadeType.Empty
                ? null
                : GrenadeVisualResolver.GetPackHudSprite(currentType);
        }
    }
}
