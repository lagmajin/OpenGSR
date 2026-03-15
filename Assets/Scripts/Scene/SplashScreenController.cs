using System.Collections;
using System.Threading;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Zenject;

namespace OpenGS
{
    /// <summary>
    /// スプラッシュ画面のシーケンス制御を担当するクラス。
    /// 数値設定、遷移ロジック、および BGM 再生を管理。
    /// </summary>
    public class SplashScreenController : AbstractScene
    {
        [Header("References")]
        [SerializeField, Required] private SplashSceneMediateObject splashMediate;

        [Header("Sequence Settings")]
        [SerializeField] private float preDelayDuration = 0.5f;
        [SerializeField] private float displayDuration = 2.0f;
        [SerializeField] private float fadeDuration = 1.0f;

        [Header("Audio Settings")]
        [SerializeField] private string bgmName = ""; // 空の場合は再生しない
        [SerializeField] private bool stopBgmOnTransition = true;

        private ISoundService soundService;

        public override SynchronizationContext MainThread() => SynchronizationContext.Current;

        [Inject]
        public void Construct(ISoundService soundService)
        {
            this.soundService = soundService;
        }

        protected override void Awake()
        {
            base.Awake();
            if (splashMediate == null) splashMediate = GetComponentInChildren<SplashSceneMediateObject>();
            
            if (splashMediate != null && splashMediate.SplashCanvasGroup != null)
            {
                splashMediate.SplashCanvasGroup.alpha = 0f;
            }
        }

        private void Start()
        {
            StartCoroutine(SplashScreenSequence());
        }

        private IEnumerator SplashScreenSequence()
        {
            if (splashMediate == null || splashMediate.SplashCanvasGroup == null)
            {
                Debug.LogError("SplashSceneMediateObject or CanvasGroup is missing!");
                GoToTitleScene();
                yield break;
            }

            // 1. プリディレイ
            if (preDelayDuration > 0)
            {
                yield return new WaitForSeconds(preDelayDuration);
            }

            // BGM 再生開始
            if (!string.IsNullOrEmpty(bgmName) && soundService != null)
            {
                soundService.PlayBGM(bgmName);
            }

            var cg = splashMediate.SplashCanvasGroup;

            // 2. フェードイン
            yield return cg.DOFade(1f, fadeDuration).WaitForCompletion();

            // 3. 表示待機
            yield return new WaitForSeconds(displayDuration);

            // 4. フェードアウト
            yield return cg.DOFade(0f, fadeDuration).WaitForCompletion();

            // 次のシーンへ
            TransitionToTitle();
        }

        protected override void Update()
        {
            base.Update();

            // キー入力でスキップ
            if (Input.anyKeyDown)
            {
                StopAllCoroutines();
                TransitionToTitle();
            }
        }

        private void TransitionToTitle()
        {
            // BGM を止める設定の場合
            if (stopBgmOnTransition && soundService != null)
            {
                soundService.StopBGM(fadeDuration);
            }

            // AbstractScene の機能を使って遷移
            GoToTitleScene();
        }
    }
}
