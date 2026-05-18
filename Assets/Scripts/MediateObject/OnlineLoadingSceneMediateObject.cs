using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OnlineLoadingSceneMediateObject : MonoBehaviour, IAbstractMediateObject
    {
        [SerializeField] private MapSceneMasterData mapSceneMasterData;

        public MapSceneMasterData MapSceneMasterData()
        {
            return mapSceneMasterData;
        }
    }
}
