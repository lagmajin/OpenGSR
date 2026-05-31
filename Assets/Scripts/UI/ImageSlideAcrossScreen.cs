using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// UI Image を左外 -> 中央 -> 右外へゆっくり移動させて消す汎用演出。
    /// 必要ならフェードも付けられる。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ImageSlideAcrossScreen : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Playback")]
        [SerializeField] private bool playOnEnable = false;

        [Header("Anchored Positions")]
        [SerializeField] private Vector2 centerAnchoredPosition;
        [SerializeField] private Vector2 startOffset = new Vector2(-1200f, 0f);
        [SerializeField] private Vector2 endOffset = new Vector2(1200f, 0f);

        [Header("Timing")]
        [SerializeField, Min(0f)] private float enterDuration = 1.2f;
        [SerializeField, Min(0f)] private float holdDuration = 0.5f;
        [SerializeField, Min(0f)] private float exitDuration = 1.2f;

        [Header("Easing")]
        [SerializeField] private Ease enterEase = Ease.OutCubic;
        [SerializeField] private Ease exitEase = Ease.InCubic;

        [Header("Fade")]
        [SerializeField] private bool useFade = true;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        private Sequence currentSequence;

        private void Awake()
        {
            if (targetRect == null)
            {
                targetRect = GetComponent<RectTransform>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (targetRect != null)
            {
                // Prefab を配置した位置を中央基準として扱う
                centerAnchoredPosition = targetRect.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            currentSequence?.Kill();
            currentSequence = null;
        }

        public void Play()
        {
            if (targetRect == null)
            {
                return;
            }

            currentSequence?.Kill();
            currentSequence = null;

            var startAnchoredPosition = centerAnchoredPosition + startOffset;
            var endAnchoredPosition = centerAnchoredPosition + endOffset;

            targetRect.anchoredPosition = startAnchoredPosition;
            SetAlpha(useFade ? 0f : 1f);

            currentSequence = DOTween.Sequence()
                .SetLink(gameObject)
                .Append(MoveTo(centerAnchoredPosition, enterDuration, enterEase));

            if (useFade)
            {
                currentSequence.Join(FadeTo(1f, Mathf.Min(fadeDuration, enterDuration)));
            }

            currentSequence
                .AppendInterval(holdDuration)
                .Append(MoveTo(endAnchoredPosition, exitDuration, exitEase));

            if (useFade)
            {
                currentSequence.Join(FadeTo(0f, Mathf.Min(fadeDuration, exitDuration)));
            }

            currentSequence.OnComplete(() => Destroy(gameObject));
            currentSequence.Play();
        }

        public void HideInstant()
        {
            currentSequence?.Kill();
            currentSequence = null;
            SetAlpha(0f);
        }

        public void SetSprite(Sprite sprite)
        {
            if (targetImage == null || sprite == null)
            {
                return;
            }

            targetImage.sprite = sprite;
        }

        public void SetCenterPosition(Vector2 position) => centerAnchoredPosition = position;

        private Tweener MoveTo(Vector2 position, float duration, Ease ease)
        {
            return targetRect.DOAnchorPos(position, duration).SetEase(ease);
        }

        private Tweener FadeTo(float alpha, float duration)
        {
            if (canvasGroup != null)
            {
                return canvasGroup.DOFade(alpha, duration);
            }

            if (targetImage != null)
            {
                return targetImage.DOFade(alpha, duration);
            }

            return DOVirtual.Float(0f, 0f, duration, _ => { });
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }

            if (targetImage != null)
            {
                var color = targetImage.color;
                color.a = alpha;
                targetImage.color = color;
            }
        }
    }
}
