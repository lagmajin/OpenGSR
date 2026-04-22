using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// コンボカウンターを「スプライト画像」で表示する UI コンポーネント。
    ///
    /// 【GameObject 構造 (Prefab)】
    ///   ComboText (このスクリプト + CanvasGroup)
    ///   ├─ DigitRoot          … 数字 Image を並べる親 (HorizontalLayoutGroup推奨)
    ///   │   ├─ Digit_0 (Image) … 最大桁 (不要なら非表示)
    ///   │   ├─ Digit_1 (Image)
    ///   │   └─ Digit_2 (Image) … 一の桁
    ///   └─ LabelImage (Image) … "Combo!" スプライト
    ///
    /// 【使用方法】
    ///   comboText.ShowCombo(count);  // コンボ数を渡すだけ
    /// </summary>
    [DisallowMultipleComponent]
    public class ComboText : MonoBehaviour, IComboText
    {
        // ─── Inspector ───────────────────────────────────────────────

        [Header("数字スプライト (index 0〜9)")]
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];

        [Header("「Combo!」スプライト")]
        [SerializeField] private Sprite comboLabelSprite;

        [Header("UI パーツ")]
        [SerializeField] private RectTransform digitRoot;    // 数字 Image の親 GameObject
        [SerializeField] private Image labelImage;           // "Combo!" スプライトを表示する Image

        [Header("表示時間 (秒)")]
        [SerializeField] private float displayDuration = 2.0f;

        [Header("フェード設定")]
        [SerializeField] private float fadeInDuration  = 0.1f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        [Header("ポップアニメーション")]
        [SerializeField] private float peakScale = 1.25f;
        [SerializeField] private float popDuration = 0.1f;

        // ─── 内部状態 ────────────────────────────────────────────────

        private CanvasGroup canvasGroup;
        private readonly List<Image> digitImages = new();
        private Sequence currentSequence;

        // ─── Unity ライフサイクル ─────────────────────────────────────

        private void Awake()
        {
            // CanvasGroup を取得 or 追加
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;

            // digitRoot 配下のすべての Image を収集しておく
            if (digitRoot != null)
            {
                digitRoot.GetComponentsInChildren<Image>(digitImages);
            }

            // "Combo!" ラベルのスプライトをセット
            if (labelImage != null && comboLabelSprite != null)
            {
                labelImage.sprite = comboLabelSprite;
            }
        }

        // ─── IComboText の実装 ────────────────────────────────────────

        /// <summary>
        /// コンボ数をスプライトで表示して、<see cref="displayDuration"/> 後に消える。
        /// 既に表示中の場合はタイマーをリセットして再表示する。
        /// </summary>
        public void ShowCombo(int count)
        {
            if (count <= 1)
            {
                Hide();
                return;
            }

            SetDigits(count);
            PlayAnimation();
        }

        /// <summary>即座に非表示にする。</summary>
        public void Hide()
        {
            currentSequence?.Kill();
            canvasGroup.alpha = 0f;
        }

        // ─── 数字スプライト割り当て ──────────────────────────────────

        /// <summary>
        /// count を文字列に変換し、各桁のスプライトを digitImages に割り当てる。
        /// 余った Image は非表示にする。
        /// </summary>
        private void SetDigits(int count)
        {
            if (digitImages.Count == 0) return;
            if (digitSprites == null || digitSprites.Length < 10) return;

            var digits = count.ToString(); // 例: 12 → "12"

            // Image の枚数に対して右詰めで桁を割り当てる
            int imageCount = digitImages.Count;
            int digitCount = digits.Length;

            for (int i = 0; i < imageCount; i++)
            {
                // 右から何番目の桁か
                int digitIndex = digitCount - (imageCount - i);

                if (digitIndex < 0)
                {
                    // 桁が足りない → 非表示
                    digitImages[i].gameObject.SetActive(false);
                }
                else
                {
                    int num = digits[digitIndex] - '0'; // char → int
                    digitImages[i].sprite = digitSprites[num];
                    digitImages[i].gameObject.SetActive(true);
                }
            }
        }

        // ─── アニメーション ───────────────────────────────────────────

        private void PlayAnimation()
        {
            currentSequence?.Kill();

            // 初期状態
            transform.localScale = Vector3.one * 0.7f;
            canvasGroup.alpha = 0f;

            currentSequence = DOTween.Sequence()
                .SetLink(gameObject)
                // フェードイン + ポップアップ
                .Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad))
                .Join(transform.DOScale(peakScale, popDuration).SetEase(Ease.OutBack))
                // 通常スケールに戻す
                .Append(transform.DOScale(1f, popDuration * 0.5f).SetEase(Ease.InOutSine))
                // 表示を保つ
                .AppendInterval(displayDuration)
                // フェードアウト
                .Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad))
                // 自己破棄
                .OnComplete(() => Destroy(gameObject));

            currentSequence.Play();
        }

        private void OnDestroy()
        {
            currentSequence?.Kill();
        }
    }
}