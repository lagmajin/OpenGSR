using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace OpenGS
{
    /// <summary>
    /// プレイヤー死亡時にリスポーンまでのカウントダウンを画面中央に表示する UI コンポーネント。
    /// PlayerRegistry の OnPlayerDied / OnPlayerRespawned を監視し、
    /// 自プレイヤーの死亡 → カウントダウン → リスポーンの流れを担当する。
    ///
    /// 【Prefab構成】
    /// RespawnCanvas (Canvas + CanvasGroup)
    ///  ├─ CountdownText (TextMeshProUGUI) … "3", "2", "1"
    ///  └─ MessageText   (TextMeshProUGUI) … "RESPAWNING IN..."
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class RespawnCanvas : MonoBehaviour
    {
        [Header("UI Parts")]
        [SerializeField] private Text countdownText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Settings")]
        [SerializeField] private float respawnTime = 5.0f;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup canvasGroup;
        private float timer;
        private bool isCounting;
        private int lastDisplayedSecond = -1;
        private IDisposable respawnCountdownSubscription;

        private void OnValidate()
        {
            if (!float.IsFinite(respawnTime) || respawnTime < 0f) respawnTime = 0f;
            if (!float.IsFinite(fadeDuration) || fadeDuration < 0f) fadeDuration = 0f;
        }

        private void Awake()
        {
            respawnTime = float.IsFinite(respawnTime) ? Mathf.Max(0f, respawnTime) : 0f;
            fadeDuration = float.IsFinite(fadeDuration) ? Mathf.Max(0f, fadeDuration) : 0f;
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            isCounting = false;
        }

        private void OnEnable()
        {
            respawnCountdownSubscription = GameEventBroker.Subscribe<RespawnCountdownEvent>(HandleRespawnCountdownEvent);
            if (PlayerRegistry.Instance == null) return;
            PlayerRegistry.Instance.OnPlayerDied += HandlePlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            DOTween.Kill(canvasGroup);
            if (countdownText != null) DOTween.Kill(countdownText.transform);
            respawnCountdownSubscription?.Dispose();
            respawnCountdownSubscription = null;
            if (PlayerRegistry.Instance == null) return;
            PlayerRegistry.Instance.OnPlayerDied -= HandlePlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void Update()
        {
            if (!isCounting) return;

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            timer = Mathf.Max(0f, timer - deltaTime);

            if (timer <= 0f)
            {
                isCounting = false;
            }

            UpdateDisplay();
        }

        private void HandlePlayerDied(AbstractPlayer player)
        {
            if (player == null || player.PlayerType() != EPlayerType.MyPlayer) return;

            if (!MatchModeResolver.CanRespawnCurrentMatch())
            {
                HideCanvas();
                return;
            }

            StartCountdown();
        }

        private void HandlePlayerRespawned(AbstractPlayer player)
        {
            if (player == null || player.PlayerType() != EPlayerType.MyPlayer) return;
            HideCanvas();
        }

        private void HandleRespawnCountdownEvent(RespawnCountdownEvent evt)
        {
            if (evt == null || PlayerRegistry.Instance == null || !Guid.TryParse(evt.PlayerID(), out var id))
            {
                return;
            }

            if (!PlayerRegistry.Instance.TryGetPlayer(id, out var player) ||
                player == null || player.PlayerType() != EPlayerType.MyPlayer)
            {
                return;
            }

            StartCountdown(Mathf.Max(0f, evt.CountdownSeconds()));
        }

        public void StartCountdown(float? overrideTime = null)
        {
            var requestedTime = overrideTime ?? respawnTime;
            timer = float.IsFinite(requestedTime) ? Mathf.Max(0f, requestedTime) : 0f;
            isCounting = true;
            lastDisplayedSecond = -1;

            if (messageText != null)
                messageText.text = "RESPAWNING IN...";

            UpdateDisplay();
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad).SetTarget(canvasGroup);
        }

        private void HideCanvas()
        {
            isCounting = false;
            canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).SetTarget(canvasGroup);
        }

        private void UpdateDisplay()
        {
            if (countdownText == null) return;

            int seconds = Mathf.CeilToInt(timer);

            if (seconds != lastDisplayedSecond)
            {
                lastDisplayedSecond = seconds;
                countdownText.text = seconds.ToString();

                // Pop animation on second change
                countdownText.transform.DOScale(1.3f, 0.1f)
                    .SetEase(Ease.OutBack)
                    .SetTarget(countdownText.transform)
                    .OnComplete(() =>
                    {
                        if (countdownText != null && isActiveAndEnabled)
                        {
                            countdownText.transform.DOScale(1f, 0.1f)
                                .SetEase(Ease.InOutSine)
                                .SetTarget(countdownText.transform);
                        }
                    });
            }
        }
    }
}
