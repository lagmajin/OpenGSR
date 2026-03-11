using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SplashScreenScene : AbstractScene
    {
        [Header("Timing")]
        [SerializeField] private float displayTime = 3.0f;
        [SerializeField] private float fadeTime = 1.2f;

        [Header("References")]
        [SerializeField] private Canvas splashCanvas;
        [SerializeField] private Image logoImage;

        [Header("Overlay")]
        [SerializeField] private Color overlayColor = Color.black;

        private Image overlayImage;

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }

        private void Start()
        {
            ResolveReferences();
            CreateOverlayIfNeeded();
            StartCoroutine(SplashSequence());
        }

        private void ResolveReferences()
        {
            if (splashCanvas == null)
            {
                splashCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (logoImage == null)
            {
                var logoTransform = transform.Find("SplashScreenCanvas/Logo");
                if (logoTransform != null)
                {
                    logoImage = logoTransform.GetComponent<Image>();
                }
            }
        }

        private void CreateOverlayIfNeeded()
        {
            if (splashCanvas == null)
            {
                return;
            }

            var overlayObj = new GameObject("SplashFadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.SetParent(splashCanvas.transform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();

            overlayImage = overlayObj.GetComponent<Image>();
            overlayImage.raycastTarget = false;
            overlayImage.color = overlayColor;
        }

        private IEnumerator SplashSequence()
        {
            if (logoImage != null)
            {
                var c = logoImage.color;
                c.a = 0f;
                logoImage.color = c;
            }

            float safeFadeTime = Mathf.Max(0.01f, fadeTime);
            float t = 0f;

            while (t < safeFadeTime)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / safeFadeTime);

                if (logoImage != null)
                {
                    var logoColor = logoImage.color;
                    logoColor.a = normalized;
                    logoImage.color = logoColor;
                }

                if (overlayImage != null)
                {
                    var coverColor = overlayImage.color;
                    coverColor.a = 1f - normalized;
                    overlayImage.color = coverColor;
                }

                yield return null;
            }

            if (overlayImage != null)
            {
                Destroy(overlayImage.gameObject);
            }

            float remaining = Mathf.Max(0f, displayTime - safeFadeTime);
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }

            GoToTitleScene();
        }
    }
}
