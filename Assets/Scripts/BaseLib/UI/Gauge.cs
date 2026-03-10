using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace OpenGS
{
    public interface IGauge
    {
        void Init(float current, float max);
        void UpdateGauge(float current, float max);
    }

    /// <summary>
    /// Slider を使用して、HP やブースターの残量をなめらかに表示する汎用ゲージコンポーネント。
    /// DOTween を用いることで、値が急激に変化してもバーが滑らかに追従する。
    /// </summary>
    [RequireComponent(typeof(Slider))]
    [DisallowMultipleComponent]
    public class Gauge : MonoBehaviour, IGauge
    {
        [Header("対象スライダー")]
        [SerializeField] private Slider targetSlider;

        [Header("アニメーション設定")]
        [SerializeField] private float animationDuration = 0.2f; // 滑らかに動く時間
        [SerializeField] private Ease animationEase = Ease.OutQuad;

        private Tween currentTween;

        private void Reset()
        {
            targetSlider = GetComponent<Slider>();
        }

        private void Awake()
        {
            if (targetSlider == null)
            {
                targetSlider = GetComponent<Slider>();
            }
        }

        /// <summary>
        /// 初期化: DOTweenアニメーションなしで即座に値を反映する。
        /// 画面遷移時やリスポーン時のために使用する。
        /// </summary>
        public void Init(float current, float max)
        {
            currentTween?.Kill();

            if (targetSlider == null) return;
            
            float targetValue = CalculateRatio(current, max);
            targetSlider.value = targetValue;
        }

        /// <summary>
        /// ゲージを更新する。現在の値から目標値へ滑らかにアニメーション変化させる。
        /// HPの増減時などに毎フレーム / イベント駆動で呼ぶことを想定。
        /// </summary>
        public void UpdateGauge(float current, float max)
        {
            if (targetSlider == null) return;

            float targetValue = CalculateRatio(current, max);

            // 以前のアニメーションが動いていればキャンセル
            currentTween?.Kill();

            // スライダーのValueをTweenで変化させる
            currentTween = targetSlider.DOValue(targetValue, animationDuration).SetEase(animationEase);
        }

        private float CalculateRatio(float current, float max)
        {
            if (max <= 0f) return 0f;
            return Mathf.Clamp01(current / max);
        }

        private void OnDestroy()
        {
            currentTween?.Kill(); // GameObject破棄時にTweenが残らないように
        }
    }
}