using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class GameUICursorController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform cursorRectTransform;
        [SerializeField] private Image cursorImage;
        private Camera cachedMainCamera;

        [Inject] private VirtualInputService virtualInputService;

        [Header("Behavior")]
        [SerializeField] private bool hideHardwareCursor = true;
        [SerializeField] private bool hideWhenCursorMissing = false;

        private void Awake()
        {
            ResolveVirtualInputService();
            AutoBind();
        }

        private void OnEnable()
        {
            ResolveVirtualInputService();
            AutoBind();
            ApplyHardwareCursorState();
            UpdateCursorPosition();
        }

        private void ResolveVirtualInputService()
        {
            if (virtualInputService != null)
            {
                return;
            }

            try
            {
                virtualInputService = DependencyInjectionConfig.Resolve<VirtualInputService>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameUICursorController] VirtualInputService is unavailable: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            if (hideHardwareCursor)
            {
                Cursor.visible = true;
            }

            virtualInputService?.ClearAim();
        }

        private void Update()
        {
            UpdateCursorPosition();
        }

        private void AutoBind()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if (cursorRectTransform == null)
            {
                cursorRectTransform = GetComponent<RectTransform>();
            }

            if (cursorImage == null)
            {
                cursorImage = GetComponent<Image>();
            }
        }

        private void ApplyHardwareCursorState()
        {
            if (!hideHardwareCursor)
            {
                return;
            }

            if (hideWhenCursorMissing && cursorImage == null)
            {
                Cursor.visible = true;
                return;
            }

            Cursor.visible = false;
        }

        private void UpdateCursorPosition()
        {
            if (cursorRectTransform == null || targetCanvas == null)
            {
                return;
            }

            var pointer = Pointer.current;
            var screenPosition = pointer != null ? pointer.position.ReadValue() : (Vector2)Input.mousePosition;
            if (!float.IsFinite(screenPosition.x) || !float.IsFinite(screenPosition.y))
            {
                return;
            }

            var canvasRect = targetCanvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera,
                    out var localPoint))
            {
                cursorRectTransform.anchoredPosition = localPoint;
            }

            UpdateVirtualAim(screenPosition);
        }

        private void UpdateVirtualAim(Vector2 screenPosition)
        {
            if (virtualInputService == null)
            {
                return;
            }

            var camera = targetCanvas.worldCamera;
            if (camera == null)
            {
                if (cachedMainCamera == null)
                {
                    cachedMainCamera = Camera.main;
                }

                camera = cachedMainCamera;
            }
            if (camera == null)
            {
                return;
            }

            var cameraDepth = -camera.transform.position.z;
            var worldPosition = camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, cameraDepth));
            if (!float.IsFinite(worldPosition.x) || !float.IsFinite(worldPosition.y) ||
                !float.IsFinite(worldPosition.z))
            {
                return;
            }

            virtualInputService.SetAim(worldPosition);
        }
    }
}
