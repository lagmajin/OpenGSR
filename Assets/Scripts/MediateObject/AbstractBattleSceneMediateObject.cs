using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class AbstractBattleSceneMediateObject : MonoBehaviour, IAbstractMediateObject
    {
        public AbstractMatchMainScript mainscript;

        [SerializeField] private MonoBehaviour uiManagerObject;

        public IInGameUIManager uiManager => uiManagerObject as IInGameUIManager;
    }
}
