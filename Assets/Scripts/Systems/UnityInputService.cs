using System;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Unity の標準入力（旧 Legacy Input）を使用した IInputService の実装。
    /// </summary>
    public class UnityInputService : IInputService
    {
        private SettingsManager boundSettingsManager;
        private float aimSensitivity = 1f;
        private bool invertAimY;

        public Vector2 GetAimWorldPosition()
        {
            if (Camera.main == null) return Vector2.zero;
            EnsureAimSettingsBound();
            var mousePosition = ApplyAimSettings(Input.mousePosition);
            var position = Camera.main.ScreenToWorldPoint(mousePosition);
            return IsFinite(position) ? position : Vector2.zero;
        }

        private void EnsureAimSettingsBound()
        {
            var settingsManager = SettingsManager.Instance;
            if (settingsManager == boundSettingsManager)
            {
                return;
            }

            if (boundSettingsManager != null)
            {
                boundSettingsManager.OnControlSettingsChanged -= OnControlSettingsChanged;
            }

            boundSettingsManager = settingsManager;
            if (boundSettingsManager != null)
            {
                boundSettingsManager.OnControlSettingsChanged += OnControlSettingsChanged;
                OnControlSettingsChanged(boundSettingsManager.GetControlSettings());
            }
        }

        private void OnControlSettingsChanged(ControlSettings settings)
        {
            if (settings == null)
            {
                aimSensitivity = 1f;
                invertAimY = false;
                return;
            }

            aimSensitivity = float.IsFinite(settings.MouseSensitivity)
                ? Mathf.Clamp(settings.MouseSensitivity, 0.1f, 5f)
                : 1f;
            invertAimY = settings.InvertMouseY;
        }

        private Vector3 ApplyAimSettings(Vector3 screenPosition)
        {
            if (!IsFinite(screenPosition))
            {
                return Vector3.zero;
            }

            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var offset = (Vector2)screenPosition - screenCenter;
            offset *= aimSensitivity;
            if (invertAimY)
            {
                offset.y = -offset.y;
            }

            return new Vector3(screenCenter.x + offset.x, screenCenter.y + offset.y, screenPosition.z);
        }

        public Vector2 GetAimDirection(Vector3 origin)
        {
            if (!IsFinite(origin)) return Vector2.right;
            Vector2 mousePos = GetAimWorldPosition();
            var direction = mousePos - (Vector2)origin;
            return IsFinite(direction) && direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : Vector2.right;
        }

        public bool IsFirePressed() => IsMouseBindingPressed("Fire", 0);

        public bool IsFireJustPressed() => IsMouseBindingDown("Fire", 0);

        public bool IsReloadJustPressed() => IsBindingDown("Reload", KeyCode.R);

        public bool IsSwapWeaponJustPressed() => IsBindingDown("SwapWeapon", KeyCode.Q);

        // Tab is reserved for the scoreboard.  Keep the drop action on a
        // separate fallback key so a default/unconfigured profile cannot
        // drop the equipped weapon while opening the scoreboard.
        public bool IsDropWeaponJustPressed() => IsBindingDown("Inventory", KeyCode.G);
        public bool IsGrenadeJustPressed() => IsBindingDown("Grenade", KeyCode.V);
        public bool IsGrenadePressed() => IsBindingPressed("Grenade", KeyCode.V);
        public bool IsGrenadeJustReleased() => IsBindingUp("Grenade", KeyCode.V);

        // Jump is not guaranteed to exist in the legacy InputManager; use the configurable binding/fallback.
        public bool IsJumpJustPressed() => IsBindingDown("Jump", KeyCode.Space);

        public bool IsSitJustPressed() => IsBindingDown("Crouch", KeyCode.S);

        public bool IsLieDownJustPressed() => IsBindingDown("LieDown", KeyCode.X);

        public int GetInstantItemSlotJustPressed()
        {
            if (IsBindingDown("InstantItem1", KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 1;
            if (IsBindingDown("InstantItem2", KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 2;
            if (IsBindingDown("InstantItem3", KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 3;
            return 0;
        }

        public float GetHorizontalAxis()
        {
            var axis = Input.GetAxisRaw("Horizontal");
            if (IsFinite(axis) && Mathf.Abs(axis) > 0.0001f) return Mathf.Clamp(axis, -1f, 1f);
            return Mathf.Clamp((IsBindingPressed("MoveRight", KeyCode.D) ? 1f : 0f)
                - (IsBindingPressed("MoveLeft", KeyCode.A) ? 1f : 0f), -1f, 1f);
        }

        public float GetVerticalAxis()
        {
            var axis = Input.GetAxisRaw("Vertical");
            if (IsFinite(axis) && Mathf.Abs(axis) > 0.0001f) return Mathf.Clamp(axis, -1f, 1f);
            return Mathf.Clamp((IsBindingPressed("MoveForward", KeyCode.W) ? 1f : 0f)
                - (IsBindingPressed("MoveBackward", KeyCode.S) ? 1f : 0f), -1f, 1f);
        }

        public bool IsJumpPressed() => IsBindingPressed("Jump", KeyCode.Space);

        public bool IsBoosterPressed() => IsMouseBindingPressed("Aim", 1);

        public bool IsSprintPressed() => IsBindingPressed("Sprint", KeyCode.LeftShift);

        public bool IsMoveLeftJustPressed() => IsBindingDown("MoveLeft", KeyCode.A);
        public bool IsMoveRightJustPressed() => IsBindingDown("MoveRight", KeyCode.D);
        public bool IsCrouchJustReleased() => IsBindingUp("Crouch", KeyCode.S);
        public bool IsLieDownJustReleased() => IsBindingUp("LieDown", KeyCode.X);
        public bool IsDashJustPressed() => IsBindingDown("Dash", KeyCode.F);
        public bool IsScoreboardJustPressed() => IsBindingDown("Scoreboard", KeyCode.Tab);
        public bool IsScoreboardJustReleased() => IsBindingUp("Scoreboard", KeyCode.Tab);

        private static bool IsBindingPressed(string action, KeyCode fallback)
        {
            return Input.GetKey(ResolveBinding(action, fallback));
        }

        private static bool IsBindingDown(string action, KeyCode fallback)
        {
            return Input.GetKeyDown(ResolveBinding(action, fallback));
        }

        private static bool IsBindingUp(string action, KeyCode fallback)
        {
            return Input.GetKeyUp(ResolveBinding(action, fallback));
        }

        private static bool IsMouseBindingPressed(string action, int fallbackButton)
        {
            var binding = GetBinding(action);
            return TryParseMouseButton(binding, out var button)
                ? Input.GetMouseButton(button)
                : Input.GetMouseButton(fallbackButton);
        }

        private static bool IsMouseBindingDown(string action, int fallbackButton)
        {
            var binding = GetBinding(action);
            return TryParseMouseButton(binding, out var button)
                ? Input.GetMouseButtonDown(button)
                : Input.GetMouseButtonDown(fallbackButton);
        }

        private static KeyCode ResolveBinding(string action, KeyCode fallback)
        {
            var binding = GetBinding(action);
            return Enum.TryParse(binding, true, out KeyCode key)
                && Enum.IsDefined(typeof(KeyCode), key)
                ? key
                : fallback;
        }

        private static string GetBinding(string action)
        {
            var settingsManager = SettingsManager.Instance;
            if (settingsManager == null) return string.Empty;
            var settings = settingsManager.GetControlSettings();
            return settings?.KeyBindings != null && settings.KeyBindings.TryGetValue(action, out var binding)
                ? binding
                : string.Empty;
        }

        private static bool TryParseMouseButton(string binding, out int button)
        {
            button = 0;
            if (string.IsNullOrWhiteSpace(binding) || !binding.StartsWith("Mouse", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return int.TryParse(binding.Substring(5), out button) && button >= 0 && button <= 6;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }
    }
}
