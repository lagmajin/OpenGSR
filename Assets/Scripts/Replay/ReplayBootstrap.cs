using UnityEngine;

namespace OpenGS
{
    public static class ReplayBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureReplayOverlay()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                return;
            }

            if (Object.FindFirstObjectByType<ReplayDebugOverlay>() != null)
            {
                return;
            }

            var go = new GameObject("ReplayDebugOverlay");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ReplayDebugOverlay>();
        }
    }
}
