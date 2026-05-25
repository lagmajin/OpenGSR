using UnityEngine.EventSystems;

namespace OpenGS
{
    /// <summary>
    /// MetalBreaker ゲームモードのメインスクリプトインターフェース。
    /// IEventSystemHandler を継承し、UnityEvent によるメッセージ受信に対応する。
    /// </summary>
    public interface IMetalBreakerMainScript : IEventSystemHandler
    {
        void OnPlayerDead();
        void OnGameFinished();
    }
}
