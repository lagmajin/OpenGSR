using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// プレイヤーの接地判定を行うコンポーネント。
    /// </summary>
    public class GroundCheck : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Vector2 checkSize = new Vector2(0.5f, 0.1f);
        [SerializeField] private Vector2 offset = new Vector2(0f, -0.5f);

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;

        private bool isGround;

        public bool IsGround => isGround;

        private void FixedUpdate()
        {
            // 指定した位置・サイズで地面レイヤーと重なっているかチェック
            Vector2 checkPos = (Vector2)transform.position + offset;
            var hit = Physics2D.OverlapBox(checkPos, checkSize, 0f, groundLayer);
            
            isGround = hit != null;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = isGround ? Color.green : Color.red;
            Vector2 checkPos = (Vector2)transform.position + offset;
            Gizmos.DrawWireCube(checkPos, checkSize);
        }
    }
}
