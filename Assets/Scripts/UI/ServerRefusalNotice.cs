using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// The one place the server's refusals reach the player.
    /// <para>
    /// The server declines things: a room that is not there, a room that is
    /// full, a match that cannot start because the players are not ready. Each
    /// answer was handled separately and each one ended in a log, so a player
    /// pressed a button and nothing appeared anywhere. Three answers saying the
    /// same thing from three places is three places to keep in step, and the
    /// player is owed one place.
    /// </para>
    /// <para>
    /// It builds its own canvas rather than needing one wired into a scene,
    /// because the answers arrive in the lobby and in the loading scene and in a
    /// match, and there is no single scene to put it in. That is the same reason
    /// the connection overlay builds itself.
    /// </para>
    /// <para>
    /// The wording is the server's. The server knows which room it could not find
    /// and who was not ready; a client inventing a sentence would be inventing
    /// one, and it would be a different sentence for each refusal.
    /// </para>
    /// </summary>
    public sealed class ServerRefusalNotice
    {
        private const float NoticeSeconds = 4f;
        private const int SortingOrder = 600;

        private static readonly ServerRefusalNotice Shared = new ServerRefusalNotice();

        private GameObject holder;
        private CanvasGroup canvasGroup;
        private TMP_Text noticeText;
        private float shownAt;

        /// <summary>
        /// Shows what the server said, for a moment, and takes it away again.
        /// </summary>
        public static void Show(string reason) => Shared.ShowNow(reason);

        private void ShowNow(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return;
            }

            EnsureBuilt();

            noticeText.text = reason;
            canvasGroup.alpha = 1f;
            shownAt = Time.unscaledTime;
        }

        private void EnsureBuilt()
        {
            if (holder != null)
            {
                return;
            }

            // The holder is a component rather than the canvas itself, because the
            // notice outlives the scene that raised it: an answer can arrive
            // during a scene change and the player reads it in the one after.
            holder = new GameObject("ServerRefusalNotice");
            UnityEngine.Object.DontDestroyOnLoad(holder);
            holder.AddComponent<NoticeHolder>().owner = this;

            var canvasObject = new GameObject("NoticeCanvas");
            canvasObject.transform.SetParent(holder.transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("RefusalPanel");
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 48f);
            rect.sizeDelta = new Vector2(560f, 56f);

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.24f, 0.06f, 0.06f, 0.94f);
            canvasGroup = panel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var textObject = new GameObject("RefusalText");
            textObject.transform.SetParent(panel.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 6f);
            textRect.offsetMax = new Vector2(-16f, -6f);
            noticeText = textObject.AddComponent<TextMeshProUGUI>();
            noticeText.alignment = TextAlignmentOptions.Center;
            noticeText.fontSize = 20f;
            noticeText.color = Color.white;
        }

        /// <summary>
        /// Takes the notice away once it has been up long enough to read.
        /// <para>
        /// A timer rather than a coroutine, because the notice is shown from
        /// wherever the answer happened to arrive and has to be taken away again
        /// after a scene change, which a coroutine on the raiser would not
        /// survive.
        /// </para>
        /// </summary>
        private sealed class NoticeHolder : MonoBehaviour
        {
            public ServerRefusalNotice owner;

            private void Update()
            {
                if (owner == null || owner.canvasGroup == null || owner.canvasGroup.alpha <= 0f)
                {
                    return;
                }

                if (Time.unscaledTime - owner.shownAt >= NoticeSeconds)
                {
                    owner.canvasGroup.alpha = 0f;
                }
            }
        }
    }
}
