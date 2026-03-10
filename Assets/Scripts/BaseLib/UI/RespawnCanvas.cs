using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private float respawnTime = 3.0f;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup canvasGroup;
        private float timer;
        private bool isCounting;
        private int lastDisplayedSecond = -1;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            isCounting = false;
        }

        private void OnEnable()
        {
            if (PlayerRegistry.Instance == null) return;
            PlayerRegistry.Instance.OnPlayerDied += HandlePlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            if (PlayerRegistry.Instance == null) return;
            PlayerRegistry.Instance.OnPlayerDied -= HandlePlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void Update()
        {
            if (!isCounting) return;

            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                timer = 0f;
                isCounting = false;
            }

            UpdateDisplay();
        }

        private void HandlePlayerDied(AbstractPlayer player)
        {
            if (player == null || player.PlayerType() != EPlayerType.MyPlayer) return;
            StartCountdown();
        }

        private void HandlePlayerRespawned(AbstractPlayer player)
        {
            if (player == null || player.PlayerType() != EPlayerType.MyPlayer) return;
            HideCanvas();
        }

        public void StartCountdown(float? overrideTime = null)
        {
            timer = overrideTime ?? respawnTime;
            isCounting = true;
            lastDisplayedSecond = -1;

            if (messageText != null)
                messageText.text = "RESPAWNING IN...";

            UpdateDisplay();
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        }

        private void HideCanvas()
        {
            isCounting = false;
            canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
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
                    .OnComplete(() =>
                    {
                        countdownText.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutSine);
                    });
            }
        }
    }
}
