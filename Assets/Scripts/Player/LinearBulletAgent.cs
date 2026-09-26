using System.Collections;
using UnityEngine;
using Zenject;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class LinearBulletAgent : AbstractBulletAgent
    {
        public float Speed;
        private Vector2 velocity;
        [SerializeField] LayerMask layerMask;
        private IEffectService effectService;

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        public override void Launch(Vector2 direction, float speed, float damage = 0)
        {
            Speed = float.IsFinite(speed) ? Mathf.Max(0f, speed) : 0f;
            var launchDirection = direction.sqrMagnitude > 0f &&
                                  float.IsFinite(direction.x) && float.IsFinite(direction.y)
                ? direction.normalized
                : Vector2.right;
            velocity = launchDirection * Speed;
            Damage = float.IsFinite(damage) ? Mathf.Max(0f, damage) : 0f;
        }

        private void Start()
        {
            Destroy(gameObject, 5f);
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

            transform.position += (Vector3)(velocity * deltaTime);
            OnColision();
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        private void OnColision()
        {
            Vector2 direction = transform.right;
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            var hit = Physics2D.Raycast(transform.position, direction, Speed * deltaTime, layerMask);

            if (hit.collider != null)
            {
                if (TryApplyHit(hit.collider, hit.point, eDamageType.Bullet, true))
                {
                    Destroy(gameObject);
                    return;
                }

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Platforms"))
                {
                    PlaySound(ESoundEffect.HitStageObject);
                    if (hitEffect != null && effectService != null)
                    {
                        effectService.PlayOneShotEffect(hitEffect, hit.point, Quaternion.identity);
                    }
                    else if (hitEffect != null)
                    {
                        var spawnedEffect = Instantiate(hitEffect, hit.point, Quaternion.identity);
                        Destroy(spawnedEffect, 5f);
                    }
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
