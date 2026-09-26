using UnityEngine;
using Zenject;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class GrenadeBulletAgent : AbstractBulletAgent
    {
        private Vector2 velocity;
        private float gravity = -9.8f;
        private float damage = 0;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private LayerMask layerMask;
        [SerializeField, Min(0.1f)] private float maxFlightTime = 10f;
        private IEffectService effectService;
        private bool exploded;

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        private float Speed = 0;
        private float flightTime;

        private void Awake()
        {
            maxFlightTime = float.IsFinite(maxFlightTime) ? Mathf.Max(0.1f, maxFlightTime) : 10f;
            gravity = float.IsFinite(gravity) ? gravity : -9.8f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(maxFlightTime)) maxFlightTime = 10f;
            if (!float.IsFinite(gravity)) gravity = -9.8f;
            maxFlightTime = Mathf.Max(0.1f, maxFlightTime);
        }

        public override void Launch(Vector2 direction, float speed, float damage = 0)
        {
            Speed = float.IsFinite(speed) ? Mathf.Max(0f, speed) : 0f;
            var safeDirection = direction.sqrMagnitude > Mathf.Epsilon && IsFinite(direction)
                ? direction.normalized
                : Vector2.right;
            velocity = safeDirection * Speed;
            this.damage = float.IsFinite(damage) ? Mathf.Max(0f, damage) : 0f;
            Damage = this.damage;
            flightTime = 0f;
            exploded = false;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
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

            flightTime += deltaTime;
            if (!float.IsFinite(flightTime))
            {
                Destroy(gameObject);
                return;
            }
            if (flightTime >= maxFlightTime)
            {
                // A grenade must not live forever when it misses every collider
                // or leaves the playable area.
                Destroy(gameObject);
                return;
            }

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

            OnColision();
        }

        private void OnColision()
        {
            Vector2 direction = transform.right;
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            var hit = Physics2D.Raycast(transform.position, direction, Speed * deltaTime, layerMask);

            if (hit.collider != null)
            {
                if (TryApplyHit(hit.collider, hit.point, eDamageType.Explosion, false))
                {
                    Explosion();
                    Destroy(gameObject);
                    return;
                }

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Platforms"))
                {
                    PlaySound(ESoundEffect.HitStageObject);
                    Explosion();
                }

                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            if (TryApplyHit(collision, collision.ClosestPoint(transform.position), eDamageType.Explosion, false))
            {
                Explosion();
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
            if (TryApplyHit(collision.collider, contactPoint, eDamageType.Explosion, false))
            {
                Explosion();
                Destroy(gameObject);
            }
        }

        private void Explosion()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            if (explosionPrefab != null)
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(explosionPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    var spawnedEffect = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                    Destroy(spawnedEffect, 5f);
                }
            }
        }
    }
}
