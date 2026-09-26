
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

        private void Awake()
        {
            time = Mathf.Max(0.01f, float.IsFinite(time) ? time : 1f);
            damage = Mathf.Max(0f, float.IsFinite(damage) ? damage : 0f);
            force = Mathf.Max(0f, float.IsFinite(force) ? force : 0f);
        }

        private void OnValidate()
        {
            if (!float.IsFinite(time)) time = 1f;
            if (!float.IsFinite(damage)) damage = 0f;
            if (!float.IsFinite(force)) force = 0f;
            time = Mathf.Max(0.01f, time);
            damage = Mathf.Max(0f, damage);
            force = Mathf.Max(0f, force);
        }

        private void Start()
        {
            Destroy(gameObject, time);
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

            var abstractPlayer = target.GetComponentInParent<AbstractPlayer>();
            if (abstractPlayer != null && PlayerRegistry.Instance != null)
            {
                var source = (Vector2)(abstractPlayer.transform.position - transform.position);
                PlayerRegistry.Instance.ApplyDamage(
                    abstractPlayer.UniqueID(),
                    source,
                    damage,
                    eDamageType.Explosion,
                    string.Empty,
                    nameof(ExpEffect),
                    false,
                    true);
                detonated = true;
                Destroy(gameObject, 0.1f);
                return;
            }

            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.AddDamageAndForce(damage, Vector3.zero, force);
                detonated = true;
                Destroy(gameObject, 0.1f);
                return;
            }

            var tags = target.GetComponentInParent<MultipleTags>();
            if (tags != null && tags.HasPlayerTag())
            {
                var playerDamageable = target.GetComponentInParent<IDamageable>();
                if (playerDamageable != null)
                {
                    playerDamageable.AddDamageAndForce(damage, Vector3.zero, force);
                    detonated = true;
                    Destroy(gameObject, 0.1f);
                }
            }
        }

    }

}
