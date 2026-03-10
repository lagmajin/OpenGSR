using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class BattleSceneMediateObject : AbstractBattleSceneMediateObject
    {
        public AbstractMatchMainScript mainscript;

        [SerializeField] private MonoBehaviour uiManagerObject;

        public IInGameUIManager uiManager => uiManagerObject as IInGameUIManager;
    }
}
