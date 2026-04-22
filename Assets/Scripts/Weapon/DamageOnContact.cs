using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Attach to objects that should deal damage when they collide or overlap targets.
    /// Minimal, configurable and with small side-effects.
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageOnContact : MonoBehaviour
    {
        [Header("Damage")]
        public float damage = 10f;
        public eDamageType damageType = eDamageType.Bullet;

        [Header("Behavior")]
        [Tooltip("If true apply damage only once per target.")]
        public bool oneShotPerTarget = true;
        [Tooltip("If true destroy this GameObject after damaging a target.")]
        public bool destroyOnHit = false;

        [Header("Continuous")]
        [Tooltip("If > 0 and target stays in trigger, apply damage every interval seconds.")]
        public float damageInterval = 0f;

        [Header("Filtering")]
        public LayerMask targetLayers = ~0;
        [Tooltip("Optional tag filter. Empty means ignore tag filter.")]
        public string targetTag = "";

        [Header("Force")]
        [Tooltip("If true will try to call AddDamageAndForce on the target when available.")]
        public bool applyForce = false;
        public float forceMagnitude = 1f;

        // last time we damaged a specific instance id
        private Dictionary<int, float> lastHitTime = new Dictionary<int, float>();

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            damageInterval = Mathf.Max(0f, damageInterval);
            forceMagnitude = Mathf.Max(0f, forceMagnitude);
        }

        private bool IsValidTarget(GameObject go)
        {
            if (go == null) return false;
            if (!string.IsNullOrEmpty(targetTag) && !go.CompareTag(targetTag)) return false;
            if ((targetLayers.value & (1 << go.layer)) == 0) return false;
            return true;
        }

        private void HandleHit(GameObject other)
        {
            if (!IsValidTarget(other)) return;

            var id = other.GetInstanceID();
            var now = Time.time;

            if (oneShotPerTarget && lastHitTime.ContainsKey(id))
            {
                // already hit
                return;
            }

            if (damageInterval > 0f)
            {
                if (lastHitTime.TryGetValue(id, out var t) && now - t < damageInterval)
                {
                    return; // still in cooldown for this target
                }
            }

            // record hit time
            lastHitTime[id] = now;

            // apply damage using IDamageable if present
            var dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
            {
                var dir = (other.transform.position - transform.position);
                Vector2 source = new Vector2(dir.x, dir.y);

                if (applyForce)
                {
                    // Prefer AddDamageAndForce if available
                    try
                    {
                        dmg.AddDamageAndForce(damage, new Vector3(source.x, source.y, 0f), forceMagnitude);
                    }
                    catch
                    {
                        // fallback to simple damage
                        dmg.AddDamage(source, damage, damageType);
                    }
                }
                else
                {
                    dmg.AddDamage(source, damage, damageType);
                }
            }
            else
            {
                // try IDamagableObject marker - no action by default
                var marker = other.GetComponent<IDamagableObject>();
                if (marker != null)
                {
                    // marker exists but no IDamageable methods - nothing to call
                }
            }

            if (destroyOnHit)
            {
                // destroy self safely
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            HandleHit(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (damageInterval > 0f)
            {
                HandleHit(collision.gameObject);
            }
        }
    }
}
