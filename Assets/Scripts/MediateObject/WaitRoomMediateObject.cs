using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WaitRoomMediateObject : AbstractMediateObject
    {
        [SerializeField] private MonoBehaviour waitRoomUiManagerBehaviour;
        private IWaitRoomUiManager cachedWaitRoomUiManager;

        public IWaitRoomUiManager WaitRoomUiManager()
        {
            if (cachedWaitRoomUiManager is MonoBehaviour cachedBehaviour && cachedBehaviour == null)
            {
                cachedWaitRoomUiManager = null;
            }

            if (cachedWaitRoomUiManager != null)
            {
                return cachedWaitRoomUiManager;
            }

            if (waitRoomUiManagerBehaviour is IWaitRoomUiManager typed)
            {
                cachedWaitRoomUiManager = typed;
                return cachedWaitRoomUiManager;
            }

            var local = GetComponent<IWaitRoomUiManager>();
            if (local != null)
            {
                cachedWaitRoomUiManager = local;
                return cachedWaitRoomUiManager;
            }

            foreach (var behaviour in GetComponentsInParent<MonoBehaviour>(true))
            {
                if (behaviour is IWaitRoomUiManager parentTyped)
                {
                    cachedWaitRoomUiManager = parentTyped;
                    return cachedWaitRoomUiManager;
                }
            }

            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is IWaitRoomUiManager sceneTyped)
                {
                    cachedWaitRoomUiManager = sceneTyped;
                    return cachedWaitRoomUiManager;
                }
            }

            Debug.LogWarning("[WaitRoomMediateObject] IWaitRoomUiManager was not found.");
            return null;
        }
    }
}
