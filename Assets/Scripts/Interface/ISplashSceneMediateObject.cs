using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// スプラッシュ画面のリファレンスを抽象化するインターフェース。
    /// 数値設定（時間等）はロジックを担う Controller 側で管理する。
    /// </summary>
    public interface ISplashSceneMediateObject : IAbstractMediateObject
    {
        /// <summary>
        /// フェード対象となる CanvasGroup
        /// </summary>
        CanvasGroup SplashCanvasGroup { get; }
    }
}
