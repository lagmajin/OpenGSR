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
        private IEffectService effectService;

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        private float Speed = 0;

        public override void Launch(Vector2 direction, float speed, float damage = 0)
        {
            Speed = speed;
            velocity = direction.normalized * speed;
            this.damage = damage;
        }

        private void Start()
        {
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

            OnColision();
        }

        private void OnColision()
        {
            Vector2 direction = transform.up;
            var hit = Physics2D.Raycast(transform.position, direction, Speed * Time.deltaTime, layerMask);

            if (hit.collider != null)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Platforms"))
                {
                    PlaySound(ESoundEffect.HitStageObject);
                    Debug.Log("衝突した: " + hit.collider.gameObject.name);
                    Explosion();
                }

                Destroy(gameObject);
            }
        }

        private void Explosion()
        {
            if (explosionPrefab != null)
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(explosionPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    Instantiate(explosionPrefab);
                }
            }
        }
    }
}
