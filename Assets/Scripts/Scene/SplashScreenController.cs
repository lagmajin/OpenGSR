using System.Collections;
using System.Threading;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace OpenGS
{
    /// <summary>
    /// スプラッシュ画面のシーケンス制御を担当するクラス。
    /// </summary>
    public class SplashScreenController : AbstractScene
    {
        [SerializeField, Required] private SplashSceneMediateObject splashMediate;

        public override SynchronizationContext MainThread() => SynchronizationContext.Current;

        protected override void Awake()
        {
            base.Awake();
            if (splashMediate == null) splashMediate = GetComponentInChildren<SplashSceneMediateObject>();
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

            var cg = splashMediate.SplashCanvasGroup;
            cg.alpha = 0f;

            // フェードイン
            yield return cg.DOFade(1f, splashMediate.FadeDuration).WaitForCompletion();

            // 表示待機
            yield return new WaitForSeconds(splashMediate.DisplayDuration);

            // フェードアウト
            yield return cg.DOFade(0f, splashMediate.FadeDuration).WaitForCompletion();

            // 次のシーンへ
            splashMediate.TransitionToTitle();
        }

        protected override void Update()
        {
            base.Update();

            // キー入力でスキップ
            if (Input.anyKeyDown)
            {
                StopAllCoroutines();
                splashMediate.TransitionToTitle();
            }
        }
    }
}
