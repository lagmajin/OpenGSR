using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// スプラッシュ画面のリファレンスと制御を抽象化するインターフェース。
    /// </summary>
    public interface ISplashSceneMediateObject : IAbstractMediateObject
    {
        CanvasGroup SplashCanvasGroup { get; }
        float DisplayDuration { get; }
        float FadeDuration { get; }
        
        /// <summary>
        /// 次のシーン（タイトル）へ遷移する。
        /// </summary>
        void TransitionToTitle();
    }
}
