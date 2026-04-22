using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Background camera for 2D games. Minimal side effects:
    /// - Does not create a Camera unless Initialize is called with createIfMissing=true.
    /// - Moves only when a follow target is set.
    /// - Exposes simple API to control culling mask, depth, background color and parallax.
    /// </summary>
    [DisallowMultipleComponent]
    public class BackgroundCamera : MonoBehaviour
    {
        [SerializeField] private Camera unityCamera;
        [SerializeField] private LayerMask cullingMask = ~0;
        [SerializeField] [Range(0f, 1f)] private float parallaxFactor = 0.5f;
        [SerializeField] private Color backgroundColor = Color.black;
        [SerializeField] private float depth = -100f;

        // Optional follow target used to create parallax effect. No follow by default.
        [SerializeField] private Transform followTarget = null;

        private Vector3 initialCameraPosition;

        public Camera UnityCamera => unityCamera;

        public float ParallaxFactor
        {
            get => parallaxFactor;
            set => parallaxFactor = Mathf.Clamp01(value);
        }

        public bool IsEnabled => unityCamera != null && unityCamera.enabled;

        void Awake()
        {
            if (unityCamera != null)
            {
                ApplyCameraSettings();
                initialCameraPosition = unityCamera.transform.position;
            }
        }

        void LateUpdate()
        {
            if (unityCamera == null) return;

            if (followTarget != null)
            {
                // Move camera to follow target with reduced motion for parallax.
                var targetPos = followTarget.position;
                var camPos = unityCamera.transform.position;
                var desired = new Vector3(targetPos.x * parallaxFactor, targetPos.y * parallaxFactor, camPos.z);
                unityCamera.transform.position = desired;
            }
        }

        /// <summary>
        /// Initialize the background camera. If camera is null and createIfMissing is true, creates a child Camera.
        /// This method is explicit to avoid accidental side effects on scene load.
        /// </summary>
        public void Initialize(Camera camera = null, bool createIfMissing = false)
        {
            if (camera != null)
            {
                unityCamera = camera;
            }
            else if (unityCamera == null)
            {
                unityCamera = GetComponent<Camera>();
                if (unityCamera == null && createIfMissing)
                {
                    var go = new GameObject("BackgroundCamera");
                    go.transform.SetParent(transform, false);
                    unityCamera = go.AddComponent<Camera>();
                }
            }

            if (unityCamera != null)
            {
                // For 2D background camera, prefer orthographic and apply configured settings.
                unityCamera.orthographic = true;
                ApplyCameraSettings();
                initialCameraPosition = unityCamera.transform.position;
            }
        }

        private void ApplyCameraSettings()
        {
            if (unityCamera == null) return;
            unityCamera.cullingMask = (int)cullingMask;
            unityCamera.backgroundColor = backgroundColor;
            unityCamera.depth = depth;
            // Background camera should render before UI and main cameras; depth default is low.
        }

        public void SetActive(bool enabled)
        {
            if (unityCamera == null) return;
            unityCamera.enabled = enabled;
        }

        public void SetCullingMask(LayerMask mask)
        {
            cullingMask = mask;
            if (unityCamera != null) unityCamera.cullingMask = (int)mask;
        }

        public void SetBackgroundColor(Color color)
        {
            backgroundColor = color;
            if (unityCamera != null) unityCamera.backgroundColor = color;
        }

        public void SetDepth(float d)
        {
            depth = d;
            if (unityCamera != null) unityCamera.depth = d;
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            if (unityCamera != null && followTarget == null)
            {
                // reset to initial position when clearing follow target
                unityCamera.transform.position = initialCameraPosition;
            }
        }

        /// <summary>
        /// Attach a background GameObject as a child of this camera for convenience.
        /// This will not modify the provided object's scene other than parenting it under this transform.
        /// </summary>
        public GameObject AttachBackground(GameObject background)
        {
            if (background == null) return null;
            background.transform.SetParent(transform, false);
            return background;
        }
    }
}
