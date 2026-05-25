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

            var local = GetComponent<IWaitRoomUiManager>();
            if (local != null)
            {
                return local;
            }

            foreach (var behaviour in GetComponentsInParent<MonoBehaviour>(true))
            {
                if (behaviour is IWaitRoomUiManager parentTyped)
                {
                    return parentTyped;
                }
            }

            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is IWaitRoomUiManager sceneTyped)
                {
                    return sceneTyped;
                }
            }

            Debug.LogWarning("[WaitRoomMediateObject] IWaitRoomUiManager was not found.");
            return null;
        }
    }
}
