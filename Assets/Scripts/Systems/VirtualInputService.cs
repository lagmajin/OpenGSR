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
        private bool fire;
        private bool jump;
        private bool booster;
        private bool swap;
        private bool drop;
        private bool grenade;
        private bool reload;

        public VirtualInputService(UnityInputService physical) => this.physical = physical;

        public void SetMove(Vector2 value) => move = Vector2.ClampMagnitude(value, 1f);
        public void SetAim(Vector2 worldPosition) => aim = worldPosition;
        public void SetFire(bool value) => fire = value;
        public void SetBooster(bool value) => booster = value;
        public void PressJump() => jump = true;
        public void PressSwapWeapon() => swap = true;
        public void PressDropWeapon() => drop = true;
        public void PressGrenade() => grenade = true;
        public void PressReload() => reload = true;
        public void ReleaseAll() { move = Vector2.zero; fire = false; booster = false; }

        public Vector2 GetAimWorldPosition() => aim == Vector2.zero ? physical.GetAimWorldPosition() : aim;
        public Vector2 GetAimDirection(Vector3 origin) => (GetAimWorldPosition() - (Vector2)origin).normalized;
        public bool IsFirePressed() => fire || physical.IsFirePressed();
        public bool IsFireJustPressed() => physical.IsFireJustPressed();
        public bool IsReloadJustPressed() => Take(ref reload) || physical.IsReloadJustPressed();
        public bool IsSwapWeaponJustPressed() => Take(ref swap) || physical.IsSwapWeaponJustPressed();
        public bool IsDropWeaponJustPressed() => Take(ref drop) || physical.IsDropWeaponJustPressed();
        public bool IsJumpJustPressed() => Take(ref jump) || physical.IsJumpJustPressed();
        public bool IsSitJustPressed() => physical.IsSitJustPressed();
        public bool IsLieDownJustPressed() => physical.IsLieDownJustPressed();
        public int GetInstantItemSlotJustPressed() => physical.GetInstantItemSlotJustPressed();
        public float GetHorizontalAxis() => Mathf.Abs(move.x) > 0.01f ? move.x : physical.GetHorizontalAxis();
        public float GetVerticalAxis() => Mathf.Abs(move.y) > 0.01f ? move.y : physical.GetVerticalAxis();
        public bool IsJumpPressed() => jump || physical.IsJumpPressed();
        public bool IsBoosterPressed() => booster || physical.IsBoosterPressed();

        private static bool Take(ref bool value)
        {
            var result = value;
            value = false;
            return result;
        }
    }
}
