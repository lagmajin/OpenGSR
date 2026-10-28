using UnityEngine;

namespace OpenGS
{
    /// <summary>Camera follow used by isolated recovered-map playtest scenes.</summary>
    public sealed class RecoveredMapCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.3f);
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        private Vector3 velocity;

        public void SetTarget(Transform value)
        {
            target = value;
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var destination = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = smoothTime <= 0f
                ? destination
                : Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime);
        }
    }
}
