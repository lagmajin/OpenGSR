
//using Cinemachine;
using UnityEngine;

namespace OpenGS
{
    //#Explosion
    [DisallowMultipleComponent]
    public class ExpEffect : MonoBehaviour
    {
        public float time = 1.0f;

        [SerializeField] private Rigidbody2D body;
        [SerializeField] private float damage = 120f;
        [SerializeField] private float force = 1.0f;
        private bool detonated;

        private void Start()
        {
            Destroy(gameObject, time);
        }

        private void Update()
        {
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Detonate(collision != null ? collision.gameObject : null);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Detonate(collision != null ? collision.gameObject : null);
        }

        private void Detonate(GameObject target)
        {
            if (detonated || target == null)
            {
                return;
            }

            if (target.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.AddDamageAndForce(damage, Vector3.zero, force);
                detonated = true;
                Destroy(gameObject, 0.1f);
                return;
            }

            if (target.TryGetComponent<MultipleTags>(out var tags) && tags.HasPlayerTag())
            {
                if (target.TryGetComponent<IDamageable>(out var playerDamageable))
                {
                    playerDamageable.AddDamageAndForce(damage, Vector3.zero, force);
                    detonated = true;
                    Destroy(gameObject, 0.1f);
                }
            }
        }

    }

}
