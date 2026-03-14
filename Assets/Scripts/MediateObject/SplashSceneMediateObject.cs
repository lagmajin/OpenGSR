using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

namespace OpenGS
{
    /// <summary>
    /// スプラッシュ画面のリファレンス保持とシーン遷移の仲介を担当するクラス。
    /// </summary>
    [DisallowMultipleComponent]
    public class SplashSceneMediateObject : AbstractMediateObject, ISplashSceneMediateObject
    {
        [Header("UI References")]
        [SerializeField, Required] private CanvasGroup splashCanvasGroup;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 2.0f;
        [SerializeField] private float fadeDuration = 1.0f;

        public CanvasGroup SplashCanvasGroup => splashCanvasGroup;
        public float DisplayDuration => displayDuration;
        public float FadeDuration => fadeDuration;

        public GeneralSceneMasterData GeneralSceneMasterData()
        {
            return OpenGS.GeneralSceneMasterData.Instance();
        }

        public void TransitionToTitle()
        {
            string titleSceneName = GeneralSceneMasterData().TitleScene();
            if (!string.IsNullOrEmpty(titleSceneName))
            {
                SceneManager.LoadScene(titleSceneName);
            }
            else
            {
                Debug.LogError("TitleScene name is not set in GeneralSceneMasterData!");
            }
        }
    }
}
