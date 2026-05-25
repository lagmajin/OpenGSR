using UnityEngine;
using Sirenix.OdinInspector;

namespace OpenGS
{
    public class GameFlagManagerControlPanel:MonoBehaviour 
    {
        [SerializeField] private bool visibleOnStart = false;

        private void Start()
        {
            gameObject.SetActive(visibleOnStart);
        }

        public void ShowPanel()
        {
            gameObject.SetActive(true);
        }

        public void HidePanel()
        {
            gameObject.SetActive(false);
        }

        public void TogglePanel()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void ResetFlags()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = string.Empty;
            Debug.Log("[GameFlagManagerControlPanel] BeforeSceneName reset");
        }


    }
}
