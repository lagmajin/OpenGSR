using DG.Tweening;
using TMPro;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ダメージ数値を TextMeshProUGUI でテキスト表示する UI コンポーネント。
    /// Prefab として生成し、SetDamage() で数値を渡すと
    /// ポップアップ → 上方向に浮きながらフェードアウト → 自己破棄する。
    ///
    /// 【Prefab 構造】
    ///   DamageTextUI (このスクリプト + CanvasGroup)
    ///   └─ DamageLabel (TextMeshProUGUI)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class DamageTextUI : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────

        [Header("表示テキスト")]
        [SerializeField] private TextMeshProUGUI damageText;

        [Header("アニメーション")]
        [Range(0, 10)] [SerializeField] private float delay      = 0f;
        [Range(0, 10)] [SerializeField] private float holdTime   = 0.8f;
        [Range(0, 100)][SerializeField] private float moveRange  = 50.0f;
        [Range(0, 10)] [SerializeField] private float fadeDuration = 0.3f;

        [Header("色設定")]
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color criticalColor  = Color.red;

        [Header("フォントサイズ演出")]
        [SerializeField] private float normalFontSize   = 36f;
        [SerializeField] private float criticalFontSize = 48f;

        // ─── 内部状態 ────────────────────────────────────────────────

        private CanvasGroup canvasGroup;
        private Sequence currentSequence;

        // ─── Unity ライフサイクル ─────────────────────────────────────

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        // ─── 公開メソッド ─────────────────────────────────────────────

        /// <summary>
        /// ダメージ量をテキストでセットし、アニメーションを再生する。
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="isCritical">クリティカルヒットか (色・フォントサイズが変わる)</param>
        public void SetDamage(int damage, bool isCritical = false)
        {
            if (damageText == null) return;

            // テキストと色をセット
            damageText.text = damage.ToString();
            damageText.color = isCritical ? criticalColor : normalColor;
            damageText.fontSize = isCritical ? criticalFontSize : normalFontSize;

            PlayAnimation();
        }

        // ─── 内部実装 ─────────────────────────────────────────────────

        private void PlayAnimation()
        {
            currentSequence?.Kill();

            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one * 0.5f;

            Vector3 startPos = transform.localPosition;
            Vector3 endPos = startPos + new Vector3(0, moveRange, 0);

            currentSequence = DOTween.Sequence()
                .SetLink(gameObject)
                .SetDelay(delay)
                // ポップアップ
                .Append(transform.DOScale(1.2f, 0.12f).SetEase(Ease.OutBack))
                .Append(transform.DOScale(1.0f, 0.08f).SetEase(Ease.InOutSine))
                // 保持
                .AppendInterval(holdTime)
                // 浮上 + フェードアウト
                .Append(transform.DOLocalMove(endPos, fadeDuration).SetEase(Ease.OutCubic))
                .Join(canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad))
                // 完了で破棄
                .OnComplete(() => Destroy(gameObject));

            currentSequence.Play();
        }

        private void OnDestroy()
        {
            currentSequence?.Kill();
        }
    }
}
