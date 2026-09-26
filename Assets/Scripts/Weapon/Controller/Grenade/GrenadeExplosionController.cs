using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class GrenadeExplosionController : MonoBehaviour
    {
        public float time = 1.0f;
        public float damage = 120.0f;
        public float force = 3.0f;
        public AudioClip expSound;

        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Vector2 size = new Vector2(1.0f, 1.0f);
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private AudioSource audioSource;

        private readonly HashSet<int> affectedObjects = new HashSet<int>();

        private void Awake()
        {
            time = Mathf.Max(0.01f, float.IsFinite(time) ? time : 1f);
            damage = Mathf.Max(0f, float.IsFinite(damage) ? damage : 0f);
            force = Mathf.Max(0f, float.IsFinite(force) ? force : 0f);
            size = new Vector2(
                Mathf.Max(0.01f, float.IsFinite(size.x) ? size.x : 1f),
                Mathf.Max(0.01f, float.IsFinite(size.y) ? size.y : 1f));
        }

        private void OnValidate()
        {
            if (!float.IsFinite(time)) time = 1f;
            if (!float.IsFinite(damage)) damage = 0f;
            if (!float.IsFinite(force)) force = 0f;
            if (!float.IsFinite(size.x)) size.x = 1f;
            if (!float.IsFinite(size.y)) size.y = 1f;

            time = Mathf.Max(0.01f, time);
            damage = Mathf.Max(0f, damage);
            force = Mathf.Max(0f, force);
            size = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        }

        private void Start()
        {
            if (audioSource != null && expSound != null)
            {
                audioSource.PlayOneShot(expSound);
            }

            Explosion();
            Destroy(gameObject, time);
        }

        private void Explosion()
        {
            foreach (var hit in Physics2D.OverlapBoxAll(transform.position, size, 0f))
            {
                ApplyDamage(hit);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            ApplyDamage(collision);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ApplyDamage(collision.collider);
        }

        private void ApplyDamage(Collider2D hit)
        {
            if (hit == null)
            {
                return;
            }

            var root = hit.transform.root;
            var hitLayerMask = 1 << hit.gameObject.layer;
            if (root != null)
            {
                hitLayerMask |= 1 << root.gameObject.layer;
            }
            if (targetMask.value != 0 && (targetMask.value & hitLayerMask) == 0)
            {
                return;
            }

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
            {
                foreach (var behaviour in hit.GetComponentsInParent<MonoBehaviour>())
                {
                    if (behaviour is IDamageable candidate)
                    {
                        damageable = candidate;
                        break;
                    }
                }
            }

            var damageableComponent = damageable as Component;
            var targetId = damageableComponent != null
                ? damageableComponent.gameObject.GetInstanceID()
                : hit.gameObject.GetInstanceID();
            if (damageable == null || !affectedObjects.Add(targetId))
            {
                return;
            }

            var directionOffset = hit.transform.position - transform.position;
            var direction = directionOffset.sqrMagnitude > 0.000001f
                ? directionOffset.normalized
                : Vector2.zero;
            if (damageable is AbstractPlayer player && PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.ApplyDamage(
                    player.UniqueID(),
                    direction,
                    damage,
                    eDamageType.Explosion,
                    string.Empty,
                    nameof(GrenadeExplosionController),
                    false,
                    true);
            }
            else
            {
                damageable.AddDamageAndForce(damage, direction, force);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
