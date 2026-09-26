using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// AIによって制御される入力サービス。
    /// Brain クラスがこのプロパティを書き換えることで、キャラクターを操作する。
    /// </summary>
    public class EnemyInputService : IInputService
    {
        public Vector2 AimWorldPosition { get; set; }
        public float Horizontal { get; set; }
        public float Vertical { get; set; }
        public bool FirePressed { get; set; }
        public bool FireJustPressed { get; set; }
        public bool ReloadJustPressed { get; set; }
        public bool SwapWeaponJustPressed { get; set; }
        public bool DropWeaponJustPressed { get; set; }
        public bool GrenadeJustPressed { get; set; }
        public bool GrenadePressed { get; set; }
        public bool JumpJustPressed { get; set; }
        public bool SitJustPressed { get; set; }
        public bool LieDownJustPressed { get; set; }
        public int InstantItemSlotJustPressed { get; set; }
        public bool JumpPressed { get; set; }
        public bool BoosterPressed { get; set; }

        public Vector2 GetAimWorldPosition() => IsFinite(AimWorldPosition) ? AimWorldPosition : Vector2.zero;

        public Vector2 GetAimDirection(Vector3 origin)
        {
            var aim = GetAimWorldPosition();
            var origin2D = new Vector2(origin.x, origin.y);
            var delta = aim - origin2D;
            return IsFinite(delta) && delta.sqrMagnitude > 0f ? delta.normalized : Vector2.right;
        }

        public bool IsFirePressed() => FirePressed;
        public bool IsFireJustPressed() => FireJustPressed;
        public bool IsReloadJustPressed() => ReloadJustPressed;
        public bool IsSwapWeaponJustPressed() => SwapWeaponJustPressed;
        public bool IsDropWeaponJustPressed() => DropWeaponJustPressed;
        public bool IsGrenadeJustPressed() => GrenadeJustPressed;
        public bool IsGrenadePressed() => GrenadePressed;
        public bool IsGrenadeJustReleased() => false;
        public bool IsJumpJustPressed() => JumpJustPressed;
        public bool IsSitJustPressed() => SitJustPressed;
        public bool IsLieDownJustPressed() => LieDownJustPressed;
        public int GetInstantItemSlotJustPressed() => InstantItemSlotJustPressed;
        public float GetHorizontalAxis() => float.IsFinite(Horizontal) ? Mathf.Clamp(Horizontal, -1f, 1f) : 0f;
        public float GetVerticalAxis() => float.IsFinite(Vertical) ? Mathf.Clamp(Vertical, -1f, 1f) : 0f;
        public bool IsJumpPressed() => JumpPressed;
        public bool IsBoosterPressed() => BoosterPressed;
        public bool IsScoreboardJustPressed() => false;
        public bool IsScoreboardJustReleased() => false;
        public bool IsMoveRightJustPressed() => false;
        public bool IsMoveLeftJustPressed() => false;
        public bool IsCrouchJustReleased() => false;
        public bool IsLieDownJustReleased() => false;
        public bool IsSprintPressed() => false;
        public bool IsDashJustPressed() => false;

        /// <summary>
        /// 全入力をリセットする
        /// </summary>
        public void Clear()
        {
            AimWorldPosition = Vector2.zero;
            Horizontal = 0;
            Vertical = 0;
            FirePressed = false;
            FireJustPressed = false;
            ReloadJustPressed = false;
            SwapWeaponJustPressed = false;
            DropWeaponJustPressed = false;
            GrenadeJustPressed = false;
            GrenadePressed = false;
            JumpJustPressed = false;
            SitJustPressed = false;
            LieDownJustPressed = false;
            InstantItemSlotJustPressed = 0;
            JumpPressed = false;
            BoosterPressed = false;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }
    }
}
