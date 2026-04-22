using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class PlayerNameSprite : MonoBehaviour
    {
        [SerializeField]public Transform target; // キャラのTransform
        public Vector3 offset = new Vector3(0, 1.0f, 0); // 頭の上

        void Update()
        {
            if (target)
                transform.position = target.position + offset;

            var cam = Camera.main;
            transform.forward = cam.transform.forward;

            transform.forward = cam.transform.forward;
        }

        void LateUpdate()
        {
            //if (Camera.main != null)
                //transform.LookAt(Camera.main.transform);
        }
    }

}