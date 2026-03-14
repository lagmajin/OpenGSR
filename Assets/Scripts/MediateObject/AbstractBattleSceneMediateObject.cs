using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class AbstractBattleSceneMediateObject : AbstractMediateObject
    {
        public AbstractMatchMainScript mainscript;

        [SerializeField] private MonoBehaviour uiManagerObject;

        public IInGameUIManager uiManager => uiManagerObject as IInGameUIManager;
    }
}
