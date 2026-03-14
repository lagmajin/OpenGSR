using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 銃を装備してターゲットを狙撃するAI。
    /// 一定距離を保ちつつ射撃を行う。
    /// </summary>
    public class GunnerEnemyBrain : EnemyBrainBase
    {
        [Header("Gunner Specific")]
        [SerializeField] private float stopDistance = 7f; // 立ち止まる距離
        [SerializeField] private float shootAngleThreshold = 5f; // 射撃を開始する角度誤差

        protected override void OnUpdate()
        {
            if (target == null)
            {
                aiInput.Clear();
                return;
            }

            // エイム（ターゲットの方向を向く）
            aiInput.AimWorldPosition = target.position;

            // 移動ロジック
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > stopDistance + 1f)
            {
                // 近づく
                aiInput.Horizontal = target.position.x > transform.position.x ? 1f : -1f;
            }
            else if (distance < stopDistance - 1f)
            {
                // 離れる
                aiInput.Horizontal = target.position.x > transform.position.x ? -1f : 1f;
            }
            else
            {
                aiInput.Horizontal = 0f;
            }

            // 射撃ロジック
            if (distance <= attackRange && IsTargetVisible())
            {
                // 向きが合っているかチェック
                Vector2 dirToTarget = (target.position - transform.position).normalized;
                float angle = Vector2.Angle(transform.right, dirToTarget); // 注意：武器の向きに合わせる必要あり

                aiInput.FirePressed = true;
            }
            else
            {
                aiInput.FirePressed = false;
            }
        }
    }
}
