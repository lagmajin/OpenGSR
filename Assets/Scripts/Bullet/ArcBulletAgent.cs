using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ArcBulletAgent : AbstractBulletAgent
    {
        private Vector2 velocity;
        private float gravity = -9.8f;
        private float damage;

        private void Awake()
        {
            gravity = float.IsFinite(gravity) ? gravity : -9.8f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(gravity)) gravity = -9.8f;
        }

        public override void Launch(Vector2 direction, float speed, float damage = 0)
        {
            var safeDirection = IsFinite(direction) && direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
            var safeSpeed = float.IsFinite(speed) ? Mathf.Max(0f, speed) : 0f;
            velocity = safeDirection * safeSpeed;
            this.damage = float.IsFinite(damage) ? Mathf.Max(0f, damage) : 0f;
            Damage = this.damage;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f || !IsFinite(transform.position))
            {
                Destroy(gameObject);
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            velocity.y += gravity * deltaTime;
            transform.position += (Vector3)(velocity * deltaTime);
            if (!IsFinite(velocity) || !IsFinite(transform.position))
            {
                Destroy(gameObject);
                return;
            }

            if (velocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            if (TryApplyHit(collision, collision.ClosestPoint(transform.position), eDamageType.Bullet, true))
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null)
            {
                return;
            }

            var contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            if (TryApplyHit(collision.collider, contactPoint, eDamageType.Bullet, true))
            {
                Destroy(gameObject);
            }
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
