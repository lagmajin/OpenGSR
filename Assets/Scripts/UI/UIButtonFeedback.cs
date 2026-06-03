using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Target")]
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;

        [Header("Sprites")]
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private Sprite disabledSprite;

        [Header("Hover Sound")]
        [SerializeField] private bool playHoverSound;
        [SerializeField] private bool hoverUseSystemSound = true;
        [SerializeField] private ESystemSound hoverSystemSound = ESystemSound.Click;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField, Range(0f, 1f)] private float hoverVolume = 1f;

        [Header("Press Sound")]
        [SerializeField] private bool playPressSound;
        [SerializeField] private bool pressUseSystemSound = true;
        [SerializeField] private ESystemSound pressSystemSound = ESystemSound.Click;
        [SerializeField] private AudioClip pressClip;
        [SerializeField, Range(0f, 1f)] private float pressVolume = 1f;

        [Header("Click Sound")]
        [SerializeField] private bool playClickSound = true;
        [SerializeField] private bool clickUseSystemSound = true;
        [SerializeField] private ESystemSound clickSystemSound = ESystemSound.Click;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

        private Sprite initialSprite;
        private bool isPointerInside;
        private bool isPointerPressed;
        private bool isSelected;
        private bool lastInteractable = true;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            CaptureInitialSprite();
            HookButtonEvents();
            RefreshVisualState();
        }

        private void OnEnable()
        {
            CacheReferences();
            CaptureInitialSprite();
            HookButtonEvents();
            RefreshVisualState();
        }

        private void LateUpdate()
        {
            if (button == null)
            {
                return;
            }

            if (lastInteractable != button.interactable)
            {
                lastInteractable = button.interactable;
                RefreshVisualState();
            }
        }

        private void OnDisable()
        {
            UnhookButtonEvents();
            isPointerInside = false;
            isPointerPressed = false;
            isSelected = false;
            RefreshVisualState();
        }

        private void OnValidate()
        {
            CacheReferences();
            CaptureInitialSprite();
            RefreshVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            isPointerInside = true;
            RefreshVisualState();

            if (playHoverSound)
            {
                PlayConfiguredSound(hoverUseSystemSound, hoverSystemSound, hoverClip, hoverVolume, nameof(OnPointerEnter));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            isPointerPressed = false;
            RefreshVisualState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            isPointerPressed = true;
            RefreshVisualState();

            if (playPressSound)
            {
                PlayConfiguredSound(pressUseSystemSound, pressSystemSound, pressClip, pressVolume, nameof(OnPointerDown));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerPressed = false;
            RefreshVisualState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            isSelected = true;
            RefreshVisualState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            RefreshVisualState();
        }

        private void HandleClick()
        {
            if (playClickSound)
            {
                PlayConfiguredSound(clickUseSystemSound, clickSystemSound, clickClip, clickVolume, nameof(HandleClick));
            }
        }

        private void CacheReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
            }

            if (button != null)
            {
                lastInteractable = button.interactable;
            }
        }

        private void CaptureInitialSprite()
        {
            if (targetImage == null)
            {
                return;
            }

            if (initialSprite == null)
            {
                initialSprite = targetImage.sprite;
            }
        }

        private void HookButtonEvents()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void UnhookButtonEvents()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
        }

        private bool IsInteractable()
        {
            return button != null && button.interactable;
        }

        private void RefreshVisualState()
        {
            if (targetImage == null)
            {
                return;
            }

            var sprite = ResolveSpriteForCurrentState();
            if (sprite != null && targetImage.sprite != sprite)
            {
                targetImage.sprite = sprite;
            }
        }

        private Sprite ResolveSpriteForCurrentState()
        {
            if (!IsInteractable())
            {
                return GetDisabledSprite();
            }

            if (isPointerPressed)
            {
                return GetPressedSprite();
            }

            if (isPointerInside || isSelected)
            {
                return GetHoverSprite();
            }

            return GetNormalSprite();
        }

        private Sprite GetNormalSprite()
        {
            return normalSprite != null ? normalSprite : initialSprite;
        }

        private Sprite GetHoverSprite()
        {
            return hoverSprite != null ? hoverSprite : GetNormalSprite();
        }

        private Sprite GetPressedSprite()
        {
            return pressedSprite != null ? pressedSprite : GetHoverSprite();
        }

        private Sprite GetDisabledSprite()
        {
            return disabledSprite != null ? disabledSprite : GetNormalSprite();
        }

        private void PlayConfiguredSound(bool useSystemSound, ESystemSound systemSound, AudioClip clip, float volume, string context)
        {
            if (useSystemSound)
            {
                SoundManager.Instance.PlaySystemSound(systemSound);
                return;
            }

            if (clip != null)
            {
                SoundManager.Instance.PlayOneShotSafe(clip, volume, 1f, $"{nameof(UIButtonFeedback)}.{context}", warnIfMissing: false);
            }
        }
    }
}
