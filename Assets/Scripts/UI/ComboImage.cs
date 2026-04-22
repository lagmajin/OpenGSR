using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// コンボカウンター背景のエフェクト担当コンポーネント。
    /// ComboText と同じ GameObject にアタッチし、
    /// ShowCombo() → ComboText が呼び出す。
    /// </summary>
    [DisallowMultipleComponent]
    public class ComboImage : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────

        [Header("背景画像")]
        [SerializeField] private Image backgroundImage;

        [Header("アニメーション設定")]
        [SerializeField] private float flashDuration = 0.08f;  // フラッシュにかかる秒数
        [SerializeField] private Color flashColor    = Color.white;
        [SerializeField] private Color normalColor   = new Color(0f, 0f, 0f, 0.6f);

        // ─── Unity ライフサイクル ─────────────────────────────────────

        private void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
        }

        // ─── 公開メソッド ─────────────────────────────────────────────

        /// <summary>
        /// コンボ取得時に呼ぶ。背景画像をフラッシュさせる。
        /// </summary>
        public void PlayFlash()
        {
            if (backgroundImage == null) return;

            backgroundImage.color = normalColor;

            DOTween.Sequence()
                .SetLink(gameObject)
                .Append(backgroundImage.DOColor(flashColor, flashDuration).SetEase(Ease.OutQuad))
                .Append(backgroundImage.DOColor(normalColor, flashDuration).SetEase(Ease.InQuad));
        }
    }
}