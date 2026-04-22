using System;
using UnityEngine;


namespace OpenGS
{
    public interface IUICamera
    {
        Camera UnityCamera { get; }
        bool IsPixelPerfect { get; set; }
        event Action OnInitialized;

        void Initialize(Camera camera = null);
        void SetActive(bool enabled);

        Vector3 ScreenToWorld(Vector2 screenPos);
        Vector2 WorldToScreen(Vector3 worldPos);

        void SetCullingMask(LayerMask mask);

        Canvas AttachCanvas(Canvas canvas = null, bool worldSpace = false);
        bool TryGetCanvas(out Canvas canvas);
    }

    public class UICamera : MonoBehaviour, IUICamera
    {
        [SerializeField] private Camera unityCamera;
        [SerializeField] private bool isPixelPerfect = false;

        public Camera UnityCamera => unityCamera;

        public bool IsPixelPerfect
        {
            get => isPixelPerfect;
            set
            {
                isPixelPerfect = value;
                if (unityCamera != null)
                {
                    // For 2D games ensure orthographic when pixel perfect requested
                    unityCamera.orthographic = true;
                    // More advanced pixel-perfect support (PixelPerfectCamera) is out of scope.
                }
            }
        }

        public event Action OnInitialized;

        public void Initialize(Camera camera = null)
        {
            // Prefer explicitly provided camera. Otherwise keep serialized one or try to find an existing camera.
            if (camera != null)
            {
                unityCamera = camera;
            }
            else if (unityCamera == null)
            {
                unityCamera = GetComponent<Camera>() ?? Camera.main;
            }

            if (unityCamera == null)
            {
                Debug.LogWarning("UICamera: No Camera assigned or found. Call Initialize with a Camera to assign one.");
                return;
            }

            // For 2D games force orthographic camera to avoid unexpected perspective rendering.
            unityCamera.orthographic = true;

            // Apply pixel perfect setting if requested
            if (isPixelPerfect)
            {
                unityCamera.orthographic = true;
            }

            OnInitialized?.Invoke();
        }

        public void SetActive(bool enabled)
        {
            if (unityCamera != null)
            {
                unityCamera.enabled = enabled;
            }
        }

        public Vector3 ScreenToWorld(Vector2 screenPos)
        {
            if (unityCamera == null) Initialize();
            if (unityCamera == null) return Vector3.zero;

            var v = unityCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, unityCamera.nearClipPlane));
            // In 2D we generally care about XY plane
            v.z = 0f;
            return v;
        }

        public Vector2 WorldToScreen(Vector3 worldPos)
        {
            if (unityCamera == null) Initialize();
            if (unityCamera == null) return Vector2.zero;

            var v = unityCamera.WorldToScreenPoint(worldPos);
            return new Vector2(v.x, v.y);
        }

        public void SetCullingMask(LayerMask mask)
        {
            if (unityCamera == null) Initialize();
            if (unityCamera == null) return;

            unityCamera.cullingMask = (int)mask;
        }

        public Canvas AttachCanvas(Canvas canvas = null, bool worldSpace = false)
        {
            if (unityCamera == null) Initialize();

            Canvas target = canvas;
            if (target == null)
            {
                // Try to find an existing child canvas
                target = GetComponentInChildren<Canvas>();
                if (target == null)
                {
                    // Create a new Canvas as a child (side-effect: creation only when necessary)
                    var go = new GameObject("UICanvas");
                    go.transform.SetParent(transform, false);
                    target = go.AddComponent<Canvas>();
                }
            }

            if (worldSpace)
            {
                target.renderMode = RenderMode.WorldSpace;
                target.worldCamera = unityCamera;
            }
            else
            {
                // Screen space - camera is preferred so that UI respects camera settings
                target.renderMode = RenderMode.ScreenSpaceCamera;
                target.worldCamera = unityCamera;
            }

            return target;
        }

        public bool TryGetCanvas(out Canvas canvas)
        {
            canvas = GetComponentInChildren<Canvas>();
            return canvas != null;
        }
    }

}
