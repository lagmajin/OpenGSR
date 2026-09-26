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
        private SynchronizationContext mainThread;
        private Coroutine splashCoroutine;

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        protected override void Awake()
        {
            base.Awake();
            displayTime = NormalizeNonNegative(displayTime, 3f);
            fadeTime = NormalizePositive(fadeTime, 1.2f);
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current;
        }

        private static float NormalizeNonNegative(float value, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Max(0f, value) : fallback;
        }

        private static float NormalizePositive(float value, float fallback)
        {
            return float.IsFinite(value) && value > 0f ? value : fallback;
        }

        private void Start()
        {
            ResolveReferences();
            CreateOverlayIfNeeded();
            splashCoroutine = StartCoroutine(SplashSequence());
        }

        protected override void OnDestroy()
        {
            if (splashCoroutine != null)
            {
                StopCoroutine(splashCoroutine);
                splashCoroutine = null;
            }

            base.OnDestroy();
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
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    yield return null;
                    continue;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                t = Mathf.Min(safeFadeTime, t + deltaTime);
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
