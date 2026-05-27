using System;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// グレネード系の飛翔・接触・爆発をまとめて扱う共通コントローラ。
    /// Rigidbody2D に依存せず、スクリプトで軌道と衝突を処理する。
    /// </summary>
    [DisallowMultipleComponent]
    public class GrenadeProjectileController : MonoBehaviour
    {
        [Header("Explosion")]
        [SerializeField] private float damage = 120f;
        [SerializeField] private float fuseTime = 3f;
        [SerializeField] private GameObject explosionEffect;

        [Header("Flight")]
        [SerializeField] private float gravity = 18f;
        [SerializeField] private float collisionRadius = 0.08f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float spriteAngleOffset = 0f;
        [SerializeField] private bool alignToVelocity = true;

        private Vector2 velocity;
        private float lifeTime;
        private bool launched;
        private bool exploded;
        private Transform ownerTransform;
        private string ownerPlayerId = string.Empty;
        private string weaponName = "Grenade";
        private ETeam ownerTeam = ETeam.NoTeam;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Launch(
            Vector2 direction,
            float speed,
            string ownerId,
            string weapon,
            ETeam team,
            Transform owner = null)
        {
            ownerTransform = owner;
            ownerPlayerId = ownerId ?? string.Empty;
            weaponName = string.IsNullOrWhiteSpace(weapon) ? "Grenade" : weapon;
            ownerTeam = team;
            velocity = (direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right) * Mathf.Max(0f, speed);
            lifeTime = 0f;
            launched = true;
            exploded = false;
            UpdateRotation();
        }

        private void Update()
        {
            if (!launched || exploded)
            {
                return;
            }

            var dt = Time.deltaTime;
            lifeTime += dt;
            if (fuseTime > 0f && lifeTime >= fuseTime)
            {
                Explode(transform.position);
                return;
            }

            velocity.y -= gravity * dt;
            var currentPosition = (Vector2)transform.position;
            var step = velocity * dt;
            if (step.sqrMagnitude <= Mathf.Epsilon)
            {
                UpdateRotation();
                return;
            }

            var hits = Physics2D.CircleCastAll(currentPosition, GetCollisionRadius(), step.normalized, step.magnitude, hitMask);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (ShouldIgnoreHit(hit.collider))
                {
                    continue;
                }

                if (!ShouldDetonateOn(hit.collider))
                {
                    continue;
                }

                transform.position = hit.point - hit.normal * GetCollisionRadius();
                UpdateRotation();
                Explode(hit.point);
                return;
            }

            transform.position = currentPosition + step;
            UpdateRotation();
        }

        private bool ShouldIgnoreHit(Collider2D collider)
        {
            if (collider == null || ownerTransform == null)
            {
                return false;
            }

            var target = collider.transform;
            return target == ownerTransform || target.IsChildOf(ownerTransform);
        }

        private bool ShouldDetonateOn(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            if (collider.gameObject.layer == LayerMask.NameToLayer("Platforms"))
            {
                return true;
            }

            if (collider.gameObject.TryGetComponent<IMultipleTags>(out var tags))
            {
                return tags.HasPlayerTag() || tags.HasStageObjectTag();
            }

            return false;
        }

        private float GetCollisionRadius()
        {
            if (collisionRadius > 0f)
            {
                return collisionRadius;
            }

            if (spriteRenderer != null)
            {
                var extents = spriteRenderer.bounds.extents;
                return Mathf.Max(0.02f, Mathf.Max(extents.x, extents.y));
            }

            return 0.08f;
        }

        private void UpdateRotation()
        {
            if (!alignToVelocity || velocity.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + spriteAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Explode(Vector2 position)
        {
            if (exploded)
            {
                return;
            }

            exploded = true;

            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, position, Quaternion.identity);
            }

            GrenadeExplosionDamageUtility.ApplyCircularDamage(position, ownerPlayerId, weaponName, ownerTeam, damage / 100f);
            Destroy(gameObject);
        }
    }
}
