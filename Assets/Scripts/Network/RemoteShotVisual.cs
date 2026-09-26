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
            direction = IsFinite(shotDirection) && shotDirection.sqrMagnitude > Mathf.Epsilon
                ? shotDirection.normalized
                : Vector2.right;
            speed = IsFinite(shotSpeed) ? Mathf.Max(0f, shotSpeed) : 0f;
            lifetime = IsFinite(shotLifetime) ? Mathf.Max(0.01f, shotLifetime) : 0.01f;
            age = 0f;
        }

        private void Update()
        {
            if (!IsFinite(transform.position))
            {
                Destroy(gameObject);
                return;
            }

            var dt = Time.deltaTime;
            if (!IsFinite(dt) || dt <= 0f)
            {
                return;
            }
            // Keep a hitch from advancing the visual projectile through an entire room.
            dt = Mathf.Min(dt, 0.1f);
            age += dt;
            if (!IsFinite(age))
            {
                Destroy(gameObject);
                return;
            }

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
            if (!IsFinite(transform.position))
            {
                Destroy(gameObject);
                return;
            }

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

        private static bool IsFinite(float value)
        {
            return float.IsFinite(value);
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }
    }
}
