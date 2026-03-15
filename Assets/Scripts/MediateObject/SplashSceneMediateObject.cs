using UnityEngine;
using Sirenix.OdinInspector;

namespace OpenGS
{
    /// <summary>
    /// スプラッシュ画面のUIリファレンスを保持するクラス。
    /// </summary>
    [DisallowMultipleComponent]
    public class SplashSceneMediateObject : AbstractMediateObject, ISplashSceneMediateObject
    {
        [Header("UI References")]
        [SerializeField, Required] private CanvasGroup splashCanvasGroup;

        public CanvasGroup SplashCanvasGroup => splashCanvasGroup;
    }
}
