using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionLobbySceneMediateObject : MonoBehaviour, IAbstractMediateObject
    {
        public GeneralSceneMasterData GeneralSceneMasterData()
        {
            return OpenGS.GeneralSceneMasterData.Instance();
        }
    }
}
