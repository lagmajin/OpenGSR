using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// Small runtime-created status banner used by online match scenes.
    /// It deliberately owns only its overlay and does not alter existing HUD prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchConnectionStatusOverlay : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private TMP_Text statusText;

        private void Awake()
        {
            BuildOverlay();
            SetConnected();
        }

        public void SetConnected()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void SetDisconnected(string reason)
        {
            if (canvasGroup == null) return;
            statusText.text = string.IsNullOrWhiteSpace(reason)
                ? "ネットワーク接続が切断されました\n再接続しています…"
                : $"ネットワーク接続が切断されました\n再接続しています… ({reason})";
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("MatchConnectionStatusCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("StatusPanel");
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(520f, 72f);

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            canvasGroup = panel.AddComponent<CanvasGroup>();

            var textObject = new GameObject("StatusText");
            textObject.transform.SetParent(panel.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);
            statusText = textObject.AddComponent<TextMeshProUGUI>();
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 20f;
            statusText.color = Color.white;
        }
    }
}
