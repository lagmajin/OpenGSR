using UnityEngine;


namespace OpenGS
{
    public class CameraAspectRatio : MonoBehaviour
    {
        private Camera cachedCamera;
        private int lastWidth;
        private int lastHeight;

        private void Start()
        {
            ApplyAspectIfNeeded(true);
        }

        private void Update()
        {
            ApplyAspectIfNeeded(false);
        }

        private void ApplyAspectIfNeeded(bool force)
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            if (cachedCamera == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            if (!force && lastWidth == Screen.width && lastHeight == Screen.height)
            {
                return;
            }

            cachedCamera.aspect = (float)Screen.width / Screen.height;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }

    }


}
