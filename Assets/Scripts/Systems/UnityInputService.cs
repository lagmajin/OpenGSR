using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Unity の標準入力（旧 Legacy Input）を使用した IInputService の実装。
    /// </summary>
    public class UnityInputService : IInputService
    {
        public Vector2 GetAimWorldPosition()
        {
            if (Camera.main == null) return Vector2.zero;
            return Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        public Vector2 GetAimDirection(Vector3 origin)
        {
            Vector2 mousePos = GetAimWorldPosition();
            return (mousePos - (Vector2)origin).normalized;
        }

        public bool IsFirePressed() => Input.GetMouseButton(0);

        public bool IsFireJustPressed() => Input.GetMouseButtonDown(0);

        public bool IsReloadJustPressed() => Input.GetKeyDown(KeyCode.R);

        public float GetHorizontalAxis() => Input.GetAxisRaw("Horizontal");

        public float GetVerticalAxis() => Input.GetAxisRaw("Vertical");

        public bool IsJumpPressed() => Input.GetButton("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);

        public bool IsBoosterPressed() => Input.GetMouseButton(1);
    }
}
