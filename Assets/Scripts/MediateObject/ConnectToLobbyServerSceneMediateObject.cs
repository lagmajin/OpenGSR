using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ConnectToLobbyServerSceneMediateObject : AbstractMediateObject, IAbstractMediateObject
    {
        [SerializeField] public ConnectToLobbyNetworkManager networkManager;
    }
}
