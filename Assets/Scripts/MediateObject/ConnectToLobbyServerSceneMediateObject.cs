using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ConnectToLobbyServerSceneMediateObject : MonoBehaviour, IAbstractMediateObject
    {
        [SerializeField] public ConnectToLobbyNetworkManager networkManager;
    }
}
