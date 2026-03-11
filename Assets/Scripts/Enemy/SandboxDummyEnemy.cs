using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class SandboxDummyEnemy : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private bool autoResetWhenDead = true;
        [SerializeField] private float resetDelaySeconds = 2.0f;

        [Header("Damage Input")]
        [SerializeField] private float defaultBulletDamage = 25f;
        [SerializeField] private bool destroyBulletOnHit = true;
        [SerializeField] private bool invulnerable;

        [Header("Feedback")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Overlay")]
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private bool showDamageLog = true;
        [SerializeField] private Vector3 overlayOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private float overlayWidth = 140f;
        [SerializeField] private float overlayHeight = 40f;
        [SerializeField] private int maxDamageLogEntries = 5;
        [SerializeField] private float damageLogLifetime = 1.5f;

        private float currentHealth;
        private Coroutine resetCoroutine;

        private struct DamageLogEntry
        {
            public float damage;
            public float expiresAt;
        }

        private readonly List<DamageLogEntry> damageLogEntries = new List<DamageLogEntry>();

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            EnsureEnemyTag();
            ResetHealth();
        }

        public void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            ApplyDamage(damage, source, type.ToString());
        }

        public void AddDamageAndForce(float damage, Vector3 vec, float force = 1.0f)
        {
            ApplyDamage(damage, vec, "Force");
        }

        public void AddDamageAndForce2(float damage, Vector2 point)
        {
            ApplyDamage(damage, point, "Force2");
        }

        public void Heal(float heal = 0)
        {
            if (heal <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + heal);
        }

        public void TakeLavaDamage()
        {
            ApplyDamage(defaultBulletDamage, transform.position, "Lava");
        }

        public void AddSlipDamage(float v, string id)
        {
            ApplyDamage(Mathf.Max(1f, v), transform.position, $"Slip:{id}");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryProcessHit(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            TryProcessHit(other.gameObject);
        }

        [ContextMenu("Reset Health")]
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            damageLogEntries.Clear();
        }

        private void TryProcessHit(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return;
            }

            float damage = 0f;

            var bullet = sourceObject.GetComponent<BulletController>();
            if (bullet != null)
            {
                damage = bullet.Damage > 0 ? bullet.Damage : defaultBulletDamage;
            }
            else
            {
                var abstractBullet = sourceObject.GetComponent<AbstractBulletAgent>();
                if (abstractBullet != null)
                {
                    damage = abstractBullet.Damage > 0 ? abstractBullet.Damage : defaultBulletDamage;
                }
            }

            if (damage <= 0f)
            {
                return;
            }

            ApplyDamage(damage, sourceObject.transform.position, "BulletHit");

            if (destroyBulletOnHit)
            {
                Destroy(sourceObject);
            }
        }

        private void ApplyDamage(float damage, Vector3 sourcePosition, string reason)
        {
            if (invulnerable || damage <= 0f)
            {
                return;
            }

            currentHealth -= damage;
            AddDamageLog(damage);

            if (hitSound != null)
            {
                PlaySound.PlaySE(hitSound);
            }

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, sourcePosition, Quaternion.identity);
            }

            Debug.Log($"[SandboxDummyEnemy] damage={damage}, hp={Mathf.Max(0f, currentHealth)}/{maxHealth}, reason={reason}");

            if (currentHealth > 0f)
            {
                return;
            }

            Debug.Log("[SandboxDummyEnemy] Dummy defeated.");

            if (!autoResetWhenDead)
            {
                gameObject.SetActive(false);
                return;
            }

            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }

            resetCoroutine = StartCoroutine(ResetAfterDelay());
        }

        private void LateUpdate()
        {
            if (!showOverlay)
            {
                return;
            }

            PruneDamageLog();
        }

        private IEnumerator ResetAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, resetDelaySeconds));
            ResetHealth();
            resetCoroutine = null;
        }

        private void EnsureEnemyTag()
        {
            var tags = GetComponent<MultipleTags>();
            if (tags == null)
            {
                return;
            }

            if (tags.tags == null)
            {
                tags.tags = new List<string>();
            }

            if (!tags.HasEnemyTag())
            {
                tags.AddTag("Enemy");
            }
        }

        private void AddDamageLog(float damage)
        {
            if (!showDamageLog)
            {
                return;
            }

            damageLogEntries.Add(new DamageLogEntry
            {
                damage = damage,
                expiresAt = Time.time + Mathf.Max(0.1f, damageLogLifetime)
            });

            if (damageLogEntries.Count > maxDamageLogEntries)
            {
                damageLogEntries.RemoveAt(0);
            }
        }

        private void PruneDamageLog()
        {
            if (damageLogEntries.Count == 0)
            {
                return;
            }

            float now = Time.time;
            for (int i = damageLogEntries.Count - 1; i >= 0; i--)
            {
                if (damageLogEntries[i].expiresAt <= now)
                {
                    damageLogEntries.RemoveAt(i);
                }
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || Camera.main == null)
            {
                return;
            }

            Vector3 worldPosition = transform.position + overlayOffset;
            Vector3 screen = Camera.main.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
            {
                return;
            }

            float x = screen.x - (overlayWidth * 0.5f);
            float y = Screen.height - screen.y - overlayHeight;

            var backgroundRect = new Rect(x, y, overlayWidth, overlayHeight);
            GUI.Box(backgroundRect, $"Dummy HP {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}");

            float ratio = maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
            var hpRect = new Rect(x + 6f, y + 20f, (overlayWidth - 12f) * ratio, 10f);

            Color oldColor = GUI.color;
            GUI.color = Color.Lerp(Color.red, Color.green, ratio);
            GUI.Box(hpRect, string.Empty);
            GUI.color = oldColor;

            if (!showDamageLog || damageLogEntries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < damageLogEntries.Count; i++)
            {
                var entry = damageLogEntries[damageLogEntries.Count - 1 - i];
                float alpha = Mathf.Clamp01((entry.expiresAt - Time.time) / Mathf.Max(0.1f, damageLogLifetime));

                Color logColor = new Color(1f, 0.5f, 0.5f, alpha);
                GUI.color = logColor;
                GUI.Label(new Rect(x + overlayWidth + 8f, y + (i * 16f), 120f, 16f), $"-{Mathf.CeilToInt(entry.damage)}");
            }

            GUI.color = oldColor;
        }
    }
}
