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
            Speed = speed;
            velocity = direction.normalized * Speed;
            Damage = damage;
        }

        private void Start()
        {
            Destroy(gameObject, 5f);
        }

        private void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
            OnColision();
        }

        private void OnColision()
        {
            Vector2 direction = transform.right;
            var hit = Physics2D.Raycast(transform.position, direction, Speed * Time.deltaTime, layerMask);

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
                    Debug.Log("衝突した: " + hit.collider.gameObject.name);

                    if (effectService != null)
                    {
                        effectService.PlayOneShotEffect(hitEffect, hit.point, Quaternion.identity);
                    }
                    else
                    {
                        Instantiate(hitEffect, hit.point, Quaternion.identity);
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
