using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WaitRoomMediateObject : AbstractMediateObject
    {
        [SerializeField] private MonoBehaviour waitRoomUiManagerBehaviour;

        public IWaitRoomUiManager WaitRoomUiManager()
        {
            if (waitRoomUiManagerBehaviour is IWaitRoomUiManager typed)
            {
                return typed;
            }

            return GetComponent<IWaitRoomUiManager>();
        }
    }
}
