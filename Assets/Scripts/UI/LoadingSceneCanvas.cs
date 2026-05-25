using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class LoadingSceneCanvas : MonoBehaviour
    {
        public void DisableUI()
        {
            gameObject.SetActive(false);
        }

        public void EnableUI()
        {
            gameObject.SetActive(true);
        }
    }
}
