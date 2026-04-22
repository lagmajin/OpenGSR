using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// ダメージ数値を「スプライト画像」で表示する UI コンポーネント。
    /// 0〜9 の数字スプライトを桁ごとの Image に割り当て、
    /// ポップアップ → 上方向に浮きながらフェードアウト → 自己破棄する。
    ///
    /// 【Prefab 構造】
    ///   DamageTextSprite (このスクリプト + CanvasGroup)
    ///   └─ DigitRoot (HorizontalLayoutGroup推奨)
    ///       ├─ Digit_0 (Image) … 最大桁
    ///       ├─ Digit_1 (Image)
    ///       └─ Digit_2 (Image) … 一の桁
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class DamageTextSprite : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────

        [Header("数字スプライト (index 0〜9)")]
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];

        [Header("UI パーツ")]
        [SerializeField] private RectTransform digitRoot; // 数字 Image の親

        [Header("アニメーション")]
        [Range(0, 10)] [SerializeField] private float delay        = 0f;
        [Range(0, 10)] [SerializeField] private float holdTime     = 0.8f;
        [Range(0, 100)][SerializeField] private float moveRange    = 50.0f;
        [Range(0, 10)] [SerializeField] private float fadeDuration = 0.3f;

        [Header("色設定")]
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color criticalColor  = Color.red;

        // ─── 内部状態 ────────────────────────────────────────────────

        private CanvasGroup canvasGroup;
        private readonly List<Image> digitImages = new();
        private Sequence currentSequence;

        // ─── Unity ライフサイクル ─────────────────────────────────────

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            if (digitRoot != null)
            {
                digitRoot.GetComponentsInChildren<Image>(digitImages);
            }
        }

        // ─── 公開メソッド ─────────────────────────────────────────────

        /// <summary>
        /// ダメージ量をスプライトでセットし、アニメーションを再生する。
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="isCritical">クリティカルか (スプライトの色が変わる)</param>
        public void SetDamage(int damage, bool isCritical = false)
        {
            SetDigits(damage);
            ApplyColor(isCritical ? criticalColor : normalColor);
            PlayAnimation();
        }

        // ─── 内部実装 ─────────────────────────────────────────────────

        /// <summary>
        /// ダメージ量を文字列化し、各桁のスプライトを Image に右詰めで割り当てる。
        /// </summary>
        private void SetDigits(int count)
        {
            if (digitImages.Count == 0) return;
            if (digitSprites == null || digitSprites.Length < 10) return;

            string digits = Mathf.Abs(count).ToString();

            int imageCount = digitImages.Count;
            int digitCount = digits.Length;

            for (int i = 0; i < imageCount; i++)
            {
                int digitIndex = digitCount - (imageCount - i);

                if (digitIndex < 0)
                {
                    // 桁が足りない → 不要な Image を非表示
                    digitImages[i].gameObject.SetActive(false);
                }
                else
                {
                    int num = digits[digitIndex] - '0';
                    digitImages[i].sprite = digitSprites[num];
                    digitImages[i].gameObject.SetActive(true);
                }
            }
        }

        private void ApplyColor(Color color)
        {
            foreach (var img in digitImages)
            {
                img.color = color;
            }
        }

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
