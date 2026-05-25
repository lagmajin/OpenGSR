using UnityEngine;
using Autofac;


namespace OpenGS
{
    public static class GameInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Initialize.Init();
            Debug.Log("[GameInitializer] BeforeSceneLoad init complete");
        }
    }
}
