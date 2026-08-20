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
    }
}
