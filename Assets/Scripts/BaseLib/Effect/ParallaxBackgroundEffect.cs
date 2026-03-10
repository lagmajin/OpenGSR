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

        void Start()
        {
            if (bgCamera == null) bgCamera = Camera.main;
            lastCameraPosition = bgCamera.transform.position;
        }

        void LateUpdate()
        {
            Vector3 delta = bgCamera.transform.position - lastCameraPosition;
            Vector3 move = new Vector3(delta.x * parallaxSpeed, delta.y * parallaxSpeed, 0f);

            transform.position = Vector3.SmoothDamp(transform.position, transform.position + move, ref velocity, 0.05f);

            lastCameraPosition = bgCamera.transform.position;
        }
    }

}