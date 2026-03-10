using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 指定したターゲット(3D/2DのTransform)の頭上等に追従する Canvas UI コンポーネント。
    /// （エイムカーソルやプレイヤー名表示などに使用）
    /// カメラのワールド座標 → スクリーン座標の変換を毎フレーム行い、画面上の適切な位置に UI を移動させます。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class AimText : MonoBehaviour
    {
        [Header("追従対象")]
        [Required]
        [Tooltip("追従したい対象の Transform (プレイヤーや敵など)")]
        [SerializeField] private Transform target;

        [Header("オフセット")]
        [Tooltip("ターゲットの position に対して加算するワールド座標上のオフセット (例: 頭上なら Y=+2)")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.2f, 0f);

        [Tooltip("変換後のスクリーン座標(ピクセル)上のオフセット")]
        [SerializeField] private Vector2 screenOffset = Vector2.zero;

        [Header("カメラ設定 (未指定時は Camera.main)")]
        [SerializeField] private Camera targetCamera;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            // ターゲットがいなくなったら何もしない（または自己破棄）
            if (target == null || targetCamera == null)
            {
                return;
            }

            // ターゲットの現在地 + ワールド空間でのオフセット
            Vector3 worldPos = target.position + worldOffset;

            // ワールド座標 → スクリーン座標に変換
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            // Z が 0 未満の場合はカメラの背後（画面に映っていない）
            if (screenPos.z < 0f)
            {
                // カメラの後ろにいる場合はUIを非表示（スケール0）
                rectTransform.localScale = Vector3.zero;
                return;
            }

            // 正面にある場合は表示（スケール1）
            if (rectTransform.localScale == Vector3.zero)
            {
                rectTransform.localScale = Vector3.one;
            }

            // 最終的にな RectTransform の位置を更新
            rectTransform.position = new Vector3(screenPos.x + screenOffset.x, screenPos.y + screenOffset.y, screenPos.z);
        }

        /// <summary>
        /// スクリプトから動的にターゲットを設定する
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            
            // セットされた瞬間に1度位置を更新してチラつきを防ぐ
            LateUpdate(); 
        }
    }
}
