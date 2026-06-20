using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class PlayerNameSprite : MonoBehaviour
    {
        [SerializeField] public Transform target; // キャラのTransform
        [SerializeField] public Vector3 offset = new Vector3(0, 1.0f, 0); // 頭の上

        void LateUpdate()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            // 名前札の正面をカメラ方向へ向ける
            transform.forward = cam.transform.forward;
        }
    }

}
