using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Minimal in-game UI interface used by BattleSceneMediateObject.
    /// </summary>
    public interface IInGameUIManager
    {
        void ShowRespawnGauge(float duration);
    }
}
