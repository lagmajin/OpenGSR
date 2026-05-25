using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class CommonCanvas : MonoBehaviour
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
