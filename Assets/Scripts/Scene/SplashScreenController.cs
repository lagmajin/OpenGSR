using System.Collections;
using System.Threading;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Zenject;

namespace OpenGS
{
    /// <summary>
    /// スプラッシュ画面のシーケンス制御を担当するコントローラー。
    /// DI コンテナからの手動解決をサポート。
    /// </summary>
    public class SplashScreenController : AbstractSceneController
    {
        [Header("References")]
        [SerializeField, Required] private SplashSceneMediateObject splashMediate;

        [Header("Sequence Settings")]
        [SerializeField] private float preDelayDuration = 0.5f;
        [SerializeField] private float displayDuration = 2.0f;
        [SerializeField] private float fadeDuration = 1.0f;

        [Header("Audio Settings")]
        [SerializeField] private bool playBgm = true;
        [SerializeField, ShowIf("playBgm")] private EBgm bgm = EBgm.SplashScreen;
        [SerializeField] private bool stopBgmOnTransition = true;

        private ISoundService soundService;
        private AbstractScene parentScene;

        [Inject]
        public void Construct(ISoundService soundService)
        {
            this.soundService = soundService;
        }

        private void Awake()
        {
            // 自動注入に失敗している場合、手動で解決を試みる
            if (soundService == null)
            {
                try {
                    soundService = DependencyInjectionConfig.Resolve<ISoundService>();
                    Debug.Log("[SplashScreen] SoundService manually resolved via DependencyInjectionConfig.");
                } catch (System.Exception e) {
                    Debug.LogWarning("[SplashScreen] Failed to manually resolve SoundService: " + e.Message);
                }
            }

            parentScene = GetComponentInParent<AbstractScene>();
            
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
                Debug.LogError("[SplashScreen] MediateObject or CanvasGroup is missing!");
                TransitionToNextScene();
                yield break;
            }

            // 1. プリディレイ
            if (preDelayDuration > 0)
            {
                yield return new WaitForSeconds(preDelayDuration);
            }

            // BGM 再生開始
            if (playBgm && soundService != null)
            {
                Debug.Log($"[SplashScreen] Playing BGM: {bgm}");
                soundService.PlayBGM(bgm);
            }
            else
            {
                Debug.LogWarning($"[SplashScreen] BGM Skip. playBgm={playBgm}, soundService={(soundService != null ? "Valid" : "Null")}");
            }

            var cg = splashMediate.SplashCanvasGroup;

            // 2. フェードイン
            yield return cg.DOFade(1f, fadeDuration).WaitForCompletion();

            // 3. 表示待機
            yield return new WaitForSeconds(displayDuration);

            // 4. フェードアウト
            yield return cg.DOFade(0f, fadeDuration).WaitForCompletion();

            // 次のシーンへ
            TransitionToNextScene();
        }

        private void Update()
        {
            // キー入力でスキップ
            if (Input.anyKeyDown)
            {
                StopAllCoroutines();
                TransitionToNextScene();
            }
        }

        private void TransitionToNextScene()
        {
            if (stopBgmOnTransition && soundService != null)
            {
                soundService.StopBGM(fadeDuration);
            }

            if (parentScene != null)
            {
                parentScene.GoToTitleScene();
            }
            else
            {
                var title = GeneralSceneMasterData.Instance().TitleScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(title);
            }
        }
    }
}
