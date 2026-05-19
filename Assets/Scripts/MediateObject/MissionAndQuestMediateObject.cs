using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionAndQuestMediateObject : MonoBehaviour, IAbstractMediateObject
    {
        public GeneralSceneMasterData GeneralSceneMasterData()
        {
            return OpenGS.GeneralSceneMasterData.Instance();
        }
    }
}
