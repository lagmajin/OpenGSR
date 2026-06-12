using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public sealed class RemoteShotVisual : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private float lifetime;
        private float age;
        private float collisionRadius = 0.05f;
        private LayerMask hitMask = ~0;

        public void Initialize(Vector2 shotDirection, float shotSpeed, float shotLifetime)
        {
            direction = shotDirection.sqrMagnitude > Mathf.Epsilon ? shotDirection.normalized : Vector2.right;
            speed = Mathf.Max(0f, shotSpeed);
            lifetime = Mathf.Max(0.01f, shotLifetime);
            age = 0f;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            age += dt;

            if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
            {
                if (age >= lifetime)
                {
                    Destroy(gameObject);
                }

                return;
            }

            var step = direction * speed * dt;
            var hits = Physics2D.CircleCastAll(transform.position, collisionRadius, direction, step.magnitude, hitMask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (ProjectileHitUtility.IsStageHit(hit.collider.gameObject) || ProjectileHitUtility.TryGetTargetPlayer(hit.collider, out _))
                {
                    transform.position = hit.point;
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position += (Vector3)step;

            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
