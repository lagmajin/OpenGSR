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
        public bool JumpPressed { get; set; }
        public bool BoosterPressed { get; set; }

        public Vector2 GetAimWorldPosition() => AimWorldPosition;

        public Vector2 GetAimDirection(Vector3 origin)
        {
            return (AimWorldPosition - (Vector2)origin).normalized;
        }

        public bool IsFirePressed() => FirePressed;
        public bool IsFireJustPressed() => FireJustPressed;
        public bool IsReloadJustPressed() => ReloadJustPressed;
        public float GetHorizontalAxis() => Horizontal;
        public float GetVerticalAxis() => Vertical;
        public bool IsJumpPressed() => JumpPressed;
        public bool IsBoosterPressed() => BoosterPressed;

        /// <summary>
        /// 全入力をリセットする
        /// </summary>
        public void Clear()
        {
            Horizontal = 0;
            Vertical = 0;
            FirePressed = false;
            FireJustPressed = false;
            ReloadJustPressed = false;
            JumpPressed = false;
            BoosterPressed = false;
        }
    }
}
