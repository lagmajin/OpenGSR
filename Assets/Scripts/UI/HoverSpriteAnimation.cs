using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public sealed class HoverSpriteAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private enum ExitBehavior
        {
            KeepCurrentFrame,
            ResetToInitialSprite,
            SetCustomSprite
        }

        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Sprite initialSpriteOverride;
        [SerializeField] private float framesPerSecond = 24f;
        [SerializeField] private ExitBehavior exitBehavior = ExitBehavior.ResetToInitialSprite;
        [SerializeField] private Sprite exitSpriteOverride;

        private Coroutine playCoroutine;
        private Sprite initialSprite;
        private bool isHovered;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
            }

            if (targetImage != null)
            {
                initialSprite = targetImage.sprite;
            }

            if (initialSpriteOverride != null)
            {
                initialSprite = initialSpriteOverride;
            }
        }

        private void OnDisable()
        {
            StopPlayback();
            ApplyExitBehavior();
            isHovered = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            StopPlayback();
            playCoroutine = StartCoroutine(PlayOnceCoroutine());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            StopPlayback();
            ApplyExitBehavior();
        }

        private IEnumerator PlayOnceCoroutine()
        {
            if (targetImage == null || frames == null || frames.Length == 0)
            {
                yield break;
            }

            var wait = new WaitForSecondsRealtime(1f / Mathf.Max(1f, framesPerSecond));
            for (var i = 0; i < frames.Length; i++)
            {
                targetImage.sprite = frames[i];
                if (i < frames.Length - 1)
                {
                    yield return wait;
                }
            }

            playCoroutine = null;
            if (!isHovered)
            {
                ApplyExitBehavior();
            }
        }

        private void StopPlayback()
        {
            if (playCoroutine == null)
            {
                return;
            }

            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        private void ApplyExitBehavior()
        {
            if (targetImage == null)
            {
                return;
            }

            switch (exitBehavior)
            {
                case ExitBehavior.ResetToInitialSprite:
                    if (initialSprite != null)
                    {
                        targetImage.sprite = initialSprite;
                    }
                    break;
                case ExitBehavior.SetCustomSprite:
                    if (exitSpriteOverride != null)
                    {
                        targetImage.sprite = exitSpriteOverride;
                    }
                    break;
            }
        }
    }
}
