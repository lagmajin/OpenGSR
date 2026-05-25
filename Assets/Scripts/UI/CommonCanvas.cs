using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class CommonCanvas : MonoBehaviour
    {
        public bool IsVisible => gameObject.activeSelf;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ShowUI()
        {
            SetVisible(true);
        }

        public void HideUI()
        {
            SetVisible(false);
        }

        public void ToggleUI()
        {
            SetVisible(!IsVisible);
        }

        public void DisableUI()
        {
            HideUI();
        }

        public void EnableUI()
        {
            ShowUI();
        }
    }
}
