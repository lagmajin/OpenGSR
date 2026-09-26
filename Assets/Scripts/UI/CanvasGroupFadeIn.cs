using UnityEngine;
using DG.Tweening;

namespace OpenGS
{
    /// <summary>
    /// アタッチされた CanvasGroup をフェードインさせる汎用コンポーネント。
    /// OnEnable() 時に自動実行することも、スクリプトから手動実行することも可能。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class CanvasGroupFadeIn : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private float fadeTime = 0.5f;
        [SerializeField] private float delayTime = 0.0f;
        [SerializeField] private bool playOnEnable = true;

        [Header("対象")]
        [SerializeField] private CanvasGroup group;

        private void Awake()
        {
            fadeTime = NormalizeNonNegative(fadeTime);
            delayTime = NormalizeNonNegative(delayTime);
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            group?.DOKill();
        }

        public void Play()
        {
            if (group == null) return;

            group.DOKill();

            // 再生前の初期状態をセット（アルファ0）
            group.alpha = 0f;

            // フェードイン実行
            group.DOFade(1f, fadeTime)
                 .SetDelay(delayTime)
                 .SetEase(Ease.OutCubic);
        }

        private static float NormalizeNonNegative(float value)
        {
            return float.IsFinite(value) ? Mathf.Max(0f, value) : 0f;
        }
    }
}
