using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class GameUICursorController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform cursorRectTransform;
        [SerializeField] private Image cursorImage;

        [Header("Behavior")]
        [SerializeField] private bool hideHardwareCursor = true;
        [SerializeField] private bool hideWhenCursorMissing = false;

        private void Awake()
        {
            AutoBind();
        }

        private void OnEnable()
        {
            AutoBind();
            ApplyHardwareCursorState();
            UpdateCursorPosition();
        }

        private void OnDisable()
        {
            if (hideHardwareCursor)
            {
                Cursor.visible = true;
            }
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

            var pointer = Mouse.current;
            var screenPosition = pointer != null ? pointer.position.ReadValue() : (Vector2)Input.mousePosition;

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
        }
    }
}
