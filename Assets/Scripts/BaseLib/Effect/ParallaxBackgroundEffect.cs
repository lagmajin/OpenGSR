using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class ParallaxBackgroundEffect : MonoBehaviour
    {
        [SerializeField] private Camera bgCamera;
        [SerializeField] private float parallaxSpeed = 0.5f;

        private Vector3 lastCameraPosition;
        private Vector3 velocity = Vector3.zero;

        private void Awake()
        {
            parallaxSpeed = float.IsFinite(parallaxSpeed) ? parallaxSpeed : 0.5f;
        }

        void Start()
        {
            TryResolveCamera();
        }

        void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }

            if (bgCamera == null && !TryResolveCamera())
            {
                return;
            }

            if (deltaTime > 0.1f)
            {
                lastCameraPosition = bgCamera.transform.position;
                velocity = Vector3.zero;
                return;
            }

            Vector3 delta = bgCamera.transform.position - lastCameraPosition;
            if (!IsFinite(delta) || !IsFinite(transform.position))
            {
                lastCameraPosition = bgCamera.transform.position;
                return;
            }

            Vector3 move = new Vector3(delta.x * parallaxSpeed, delta.y * parallaxSpeed, 0f);

            transform.position = Vector3.SmoothDamp(transform.position, transform.position + move, ref velocity, 0.05f);

            lastCameraPosition = bgCamera.transform.position;
        }

        private bool TryResolveCamera()
        {
            if (bgCamera != null)
            {
                return true;
            }

            bgCamera = Camera.main;
            if (bgCamera == null)
            {
                return false;
            }

            lastCameraPosition = bgCamera.transform.position;
            velocity = Vector3.zero;
            return true;
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }
    }

}
