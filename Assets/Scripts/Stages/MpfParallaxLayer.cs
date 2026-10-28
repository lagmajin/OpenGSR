using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Keeps one recovered MPF background at its source transform while the
    /// camera moves, using the source map's normalized parallax factors.
    /// </summary>
    [DefaultExecutionOrder(101)]
    public sealed class MpfParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float factorX = 1f;
        [SerializeField] private float factorY = 1f;

        private Vector3 sourcePosition;
        private Vector3 cameraStartPosition;
        private Camera targetCamera;

        public void Configure(float x, float y)
        {
            factorX = x;
            factorY = y;
        }

        private void Awake()
        {
            sourcePosition = transform.position;
            targetCamera = Camera.main;
            if (targetCamera != null)
            {
                cameraStartPosition = targetCamera.transform.position;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    return;
                }

                cameraStartPosition = targetCamera.transform.position;
            }

            var cameraDelta = targetCamera.transform.position - cameraStartPosition;
            transform.position = new Vector3(
                sourcePosition.x + cameraDelta.x * (1f - factorX),
                sourcePosition.y + cameraDelta.y * (1f - factorY),
                sourcePosition.z);
        }
    }
}
