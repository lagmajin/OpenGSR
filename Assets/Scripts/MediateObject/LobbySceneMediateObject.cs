using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class LobbySceneMediateObject : AbstractMediateObject, ILobbyMediateObject
    {
        [SerializeField] public AbstractCreateNewRoomDialog createNewRoomDialog;

        public GeneralSceneMasterData GeneralSceneMasterData()
        {
            return OpenGS.GeneralSceneMasterData.Instance();
        }
    }
}
