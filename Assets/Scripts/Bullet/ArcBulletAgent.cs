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

        public override void Launch(Vector2 direction, float speed, float damage = 0)
        {
            velocity = direction.normalized * speed;
            this.damage = damage;
            Damage = damage;
        }

        private void Update()
        {
            velocity.y += gravity * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);

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
    }
}
