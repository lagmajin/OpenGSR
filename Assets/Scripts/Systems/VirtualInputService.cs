using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 物理入力と、UI/ゲームパッド/AIから注入される仮想入力を同じ入力口にまとめる。
    /// UI側は Press/Release/SetAxis を呼ぶだけでよい。
    /// </summary>
    public sealed class VirtualInputService : IInputService
    {
        private readonly UnityInputService physical;
        private Vector2 move;
        private Vector2 aim;
        private bool hasVirtualAim;
        private bool fire;
        private bool fireJustPressed;
        private bool jump;
        private bool booster;
        private bool swap;
        private bool drop;
        private bool grenade;
        private bool reload;

        public VirtualInputService(UnityInputService physical) => this.physical = physical;

        public void SetMove(Vector2 value)
        {
            if (!IsFinite(value))
            {
                move = Vector2.zero;
                return;
            }

            move = Vector2.ClampMagnitude(value, 1f);
        }
        public void SetAim(Vector2 worldPosition)
        {
            if (!IsFinite(worldPosition))
            {
                ClearAim();
                return;
            }

            aim = worldPosition;
            hasVirtualAim = true;
        }
        public void ClearAim()
        {
            aim = Vector2.zero;
            hasVirtualAim = false;
        }
        public void SetFire(bool value)
        {
            if (value && !fire)
            {
                fireJustPressed = true;
            }
            fire = value;
        }
        public void SetBooster(bool value) => booster = value;
        public void PressJump() => jump = true;
        public void PressSwapWeapon() => swap = true;
        public void PressDropWeapon() => drop = true;
        public void PressGrenade() => grenade = true;
        public void PressReload() => reload = true;
        public void ReleaseAll()
        {
            move = Vector2.zero;
            ClearAim();
            fire = false;
            fireJustPressed = false;
            jump = false;
            booster = false;
            swap = false;
            drop = false;
            grenade = false;
            reload = false;
        }

        public Vector2 GetAimWorldPosition() => hasVirtualAim ? aim : physical.GetAimWorldPosition();
        public Vector2 GetAimDirection(Vector3 origin)
        {
            if (!float.IsFinite(origin.x) || !float.IsFinite(origin.y) || !float.IsFinite(origin.z))
            {
                return Vector2.right;
            }

            var direction = GetAimWorldPosition() - (Vector2)origin;
            return IsFinite(direction) && direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized
                : Vector2.right;
        }
        public bool IsFirePressed() => fire || physical.IsFirePressed();
        public bool IsFireJustPressed() => Take(ref fireJustPressed) || physical.IsFireJustPressed();
        public bool IsReloadJustPressed() => Take(ref reload) || physical.IsReloadJustPressed();
        public bool IsSwapWeaponJustPressed() => Take(ref swap) || physical.IsSwapWeaponJustPressed();
        public bool IsDropWeaponJustPressed() => Take(ref drop) || physical.IsDropWeaponJustPressed();
        public bool IsGrenadeJustPressed() => Take(ref grenade) || physical.IsGrenadeJustPressed();
        public bool IsGrenadePressed() => grenade || physical.IsGrenadePressed();
        public bool IsGrenadeJustReleased() => physical.IsGrenadeJustReleased();
        public bool IsJumpJustPressed() => Take(ref jump) || physical.IsJumpJustPressed();
        public bool IsSitJustPressed() => physical.IsSitJustPressed();
        public bool IsLieDownJustPressed() => physical.IsLieDownJustPressed();
        public int GetInstantItemSlotJustPressed() => physical.GetInstantItemSlotJustPressed();
        public float GetHorizontalAxis() => Mathf.Abs(move.x) > 0.01f ? move.x : physical.GetHorizontalAxis();
        public float GetVerticalAxis() => Mathf.Abs(move.y) > 0.01f ? move.y : physical.GetVerticalAxis();
        public bool IsJumpPressed() => jump || physical.IsJumpPressed();
        public bool IsBoosterPressed() => booster || physical.IsBoosterPressed();
        public bool IsScoreboardJustPressed() => physical.IsScoreboardJustPressed();
        public bool IsScoreboardJustReleased() => physical.IsScoreboardJustReleased();
        public bool IsMoveRightJustPressed() => physical.IsMoveRightJustPressed();
        public bool IsMoveLeftJustPressed() => physical.IsMoveLeftJustPressed();
        public bool IsCrouchJustReleased() => physical.IsCrouchJustReleased();
        public bool IsLieDownJustReleased() => physical.IsLieDownJustReleased();
        public bool IsSprintPressed() => physical.IsSprintPressed();
        public bool IsDashJustPressed() => physical.IsDashJustPressed();

        private static bool Take(ref bool value)
        {
            var result = value;
            value = false;
            return result;
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }
    }
}
