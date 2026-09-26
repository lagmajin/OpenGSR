using System;
using UnityEngine;

namespace OpenGS.UI
{
    [CreateAssetMenu(menuName = "OpenGS/UI/Embedded WebView Settings", fileName = "EmbeddedWebViewSettings")]
    public sealed class EmbeddedWebViewSettings : ScriptableObject
    {
        [SerializeField] private string newsUrl = "";
        [SerializeField] private string clanUrl = "";

        public string NewsUrl => newsUrl;
        public string ClanUrl => clanUrl;

        private void OnValidate()
        {
            ValidateUrl(nameof(newsUrl), newsUrl);
            ValidateUrl(nameof(clanUrl), clanUrl);
        }

        private static void ValidateUrl(string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[EmbeddedWebViewSettings] {fieldName} must be an absolute HTTPS URL.");
            }
        }
    }
}
