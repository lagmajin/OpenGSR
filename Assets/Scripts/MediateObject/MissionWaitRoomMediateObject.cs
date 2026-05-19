using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionWaitRoomMediateObject : MonoBehaviour, IAbstractMediateObject
    {
        public GeneralSceneMasterData GeneralSceneMasterData()
        {
            return OpenGS.GeneralSceneMasterData.Instance();
        }
    }
}
