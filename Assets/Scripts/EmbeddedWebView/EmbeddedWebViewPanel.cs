using System;
using UnityEngine;
using Gree.UnityWebView;

namespace OpenGS.UI
{
    /// <summary>
    /// Native WebView overlay for news, clan, and other read-only web surfaces.
    /// Attach this component to a persistent UI object and wire the public
    /// methods to Unity Buttons.
    /// </summary>
    public sealed class EmbeddedWebViewPanel : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private EmbeddedWebViewSettings settings;
        [SerializeField] private string newsUrl = "";
        [SerializeField] private string clanUrl = "";

        [Header("Layout")]
        [SerializeField] private int leftMargin = 80;
        [SerializeField] private int topMargin = 80;
        [SerializeField] private int rightMargin = 80;
        [SerializeField] private int bottomMargin = 80;
        [SerializeField] private bool openOnStart;

        private WebViewObject webView;
        private string currentUrl;
        private bool isOpen;

        public bool IsOpen => isOpen;

        private void OnValidate()
        {
            leftMargin = Mathf.Max(0, leftMargin);
            topMargin = Mathf.Max(0, topMargin);
            rightMargin = Mathf.Max(0, rightMargin);
            bottomMargin = Mathf.Max(0, bottomMargin);

            ValidateUrl("News", newsUrl);
            ValidateUrl("Clan", clanUrl);
            if (settings != null)
            {
                ValidateUrl("Configured News", settings.NewsUrl);
                ValidateUrl("Configured Clan", settings.ClanUrl);
            }
        }

        private static void ValidateUrl(string label, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[EmbeddedWebViewPanel] {label} URL must be an absolute HTTPS URL.");
            }
        }

        private void Start()
        {
            if (openOnStart) OpenNews();
        }

        public void OpenNews() => Open(ResolveUrl(newsUrl, settings != null ? settings.NewsUrl : null));
        public void OpenClan() => Open(ResolveUrl(clanUrl, settings != null ? settings.ClanUrl : null));

        private static string ResolveUrl(string localUrl, string configuredUrl)
        {
            return string.IsNullOrWhiteSpace(localUrl) ? configuredUrl : localUrl;
        }

        public void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("[EmbeddedWebViewPanel] URL is empty.");
                return;
            }

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[EmbeddedWebViewPanel] Only absolute HTTPS URLs are allowed.");
                return;
            }

            EnsureWebView();
            currentUrl = uri.AbsoluteUri;
            webView.SetMargins(leftMargin, topMargin, rightMargin, bottomMargin);
            webView.LoadURL(currentUrl);
            webView.SetVisibility(true);
            isOpen = true;
        }

        public void Close()
        {
            if (webView != null) webView.SetVisibility(false);
            isOpen = false;
        }

        public void Reload()
        {
            if (webView != null && !string.IsNullOrWhiteSpace(currentUrl)) webView.LoadURL(currentUrl);
        }

        private void EnsureWebView()
        {
            if (webView != null) return;

            var webViewObject = new GameObject("EmbeddedWebView");
            webViewObject.transform.SetParent(transform, false);
            webView = webViewObject.AddComponent<WebViewObject>();
            webView.Init(
                cb: message => Debug.Log($"[EmbeddedWebViewPanel] {message}"),
                err: message => Debug.LogWarning($"[EmbeddedWebViewPanel] {message}"),
                ld: message => Debug.Log($"[EmbeddedWebViewPanel] Loaded: {message}"),
                enableWKWebView: true);
            webView.SetVisibility(false);
        }

        private void OnDestroy()
        {
            if (webView != null) Destroy(webView.gameObject);
            webView = null;
            isOpen = false;
        }
    }
}
