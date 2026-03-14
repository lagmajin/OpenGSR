using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ターゲットに向かって突進し、近接攻撃を行うモンスター型AI。
    /// </summary>
    public class MonsterEnemyBrain : EnemyBrainBase
    {
        [Header("Monster Specific")]
        [SerializeField] private bool canJump = true;
        [SerializeField] private float jumpCheckInterval = 0.5f;

        private float nextJumpCheck = 0f;

        protected override void OnUpdate()
        {
            if (target == null)
            {
                aiInput.Clear();
                return;
            }

            // モンスターなのでターゲットの方向に常にエイム（攻撃方向）
            aiInput.AimWorldPosition = target.position;

            // ひたすら近づく
            aiInput.Horizontal = target.position.x > transform.position.x ? 1f : -1f;

            // ジャンプロジック（段差やプレイヤーが上にいる場合）
            if (canJump && Time.time >= nextJumpCheck)
            {
                if (target.position.y > transform.position.y + 1f || IsBlockedByWall())
                {
                    aiInput.JumpPressed = true;
                }
                else
                {
                    aiInput.JumpPressed = false;
                }
                nextJumpCheck = Time.time + jumpCheckInterval;
            }

            // 攻撃ロジック
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance <= attackRange)
            {
                aiInput.FirePressed = true;
            }
            else
            {
                aiInput.FirePressed = false;
            }
        }

        private bool IsBlockedByWall()
        {
            // 前方に壁があるかチェック
            var dir = new Vector2(aiInput.Horizontal, 0);
            var hit = Physics2D.Raycast(transform.position, dir, 1f, LayerMask.GetMask("Map"));
            return hit.collider != null;
        }
    }
}
