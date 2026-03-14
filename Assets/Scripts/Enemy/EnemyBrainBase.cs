using UnityEngine;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// 敵AIの思考ロジックのベースクラス。
    /// ターゲットの検知や入力サービスへの書き込みを担当する。
    /// </summary>
    [RequireComponent(typeof(EnemyInputService))]
    public abstract class EnemyBrainBase : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] protected float detectionRange = 10f;
        [SerializeField] protected float attackRange = 5f;
        [SerializeField] protected LayerMask targetLayer;
        [SerializeField] protected float thinkInterval = 0.2f;

        protected EnemyInputService aiInput;
        protected Transform target;
        private float nextThinkTime = 0f;

        protected virtual void Awake()
        {
            aiInput = GetComponent<EnemyInputService>();
        }

        protected virtual void Update()
        {
            if (Time.time >= nextThinkTime)
            {
                Think();
                nextThinkTime = Time.time + thinkInterval;
            }

            // 毎フレームの更新（滑らかな移動やエイムなど）
            OnUpdate();
        }

        /// <summary>
        /// 一定間隔で行われる重い思考処理（ターゲット検索など）
        /// </summary>
        protected virtual void Think()
        {
            if (target == null || Vector2.Distance(transform.position, target.position) > detectionRange * 1.5f)
            {
                target = FindNearestTarget();
            }
        }

        /// <summary>
        /// 毎フレーム行われる軽量な更新処理（移動入力の決定など）
        /// </summary>
        protected abstract void OnUpdate();

        protected Transform FindNearestTarget()
        {
            // シンプルにプレイヤーを探す（将来的に抽象化可能）
            var hit = Physics2D.OverlapCircle(transform.position, detectionRange, targetLayer);
            return hit != null ? hit.transform : null;
        }

        protected bool IsTargetVisible()
        {
            if (target == null) return false;
            
            // 視線チェック
            var dir = (target.position - transform.position).normalized;
            var dist = Vector2.Distance(transform.position, target.position);
            var hit = Physics2D.Raycast(transform.position, dir, dist, LayerMask.GetMask("Default", "Map")); // 壁チェック
            
            return hit.collider == null || hit.transform == target;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
