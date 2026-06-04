using System;
using OpenGSCore;
using UnityEngine;
using Zenject;

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
        [Header("Cluster")]
        [SerializeField] private bool spawnChildProjectiles;
        [SerializeField] private GameObject childProjectilePrefab;
        [SerializeField] private int childProjectileCount = 0;
        [SerializeField] private float childLaunchSpeed = 8f;
        [SerializeField] private float childSpreadAngle = 45f;
        [SerializeField] private float childDamageMultiplier = 0.35f;
        [SerializeField] private float childFuseTime = 1.25f;

        private float lifeTime;
        private bool launched;
        private bool exploded;
        private Transform ownerTransform;
        private string ownerPlayerId = string.Empty;
        private string weaponName = "Grenade";
        private ETeam ownerTeam = ETeam.NoTeam;
        private EGrenadeType grenadeType = EGrenadeType.Normal;
        private SpriteRenderer spriteRenderer;
        private IEffectService effectService;
        private readonly ProjectileBallistics2D ballistics = new ProjectileBallistics2D();

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

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
            Launch(direction, speed, ownerId, weapon, team, owner, EGrenadeType.Normal);
        }

        public void Launch(
            Vector2 direction,
            float speed,
            string ownerId,
            string weapon,
            ETeam team,
            Transform owner,
            EGrenadeType type)
        {
            ownerTransform = owner;
            ownerPlayerId = ownerId ?? string.Empty;
            weaponName = string.IsNullOrWhiteSpace(weapon) ? "Grenade" : weapon;
            ownerTeam = team;
            grenadeType = type;
            ballistics.Configure(direction, speed, true, gravity, alignToVelocity, spriteAngleOffset);
            lifeTime = 0f;
            launched = true;
            exploded = false;
            UpdateRotation();
        }

        public void SetDamage(float value)
        {
            damage = Mathf.Max(0f, value);
        }

        public void SetFuseTime(float value)
        {
            fuseTime = Mathf.Max(0f, value);
        }

        public void SetExplosionEffect(GameObject effect)
        {
            explosionEffect = effect;
        }

        public void SetGrenadeType(EGrenadeType type)
        {
            grenadeType = type;
        }

        public void ConfigureChildProjectiles(
            bool enabled,
            GameObject projectilePrefab,
            int projectileCount,
            float launchSpeed,
            float spreadAngle,
            float damageMultiplier,
            float childFuse)
        {
            spawnChildProjectiles = enabled;
            childProjectilePrefab = projectilePrefab;
            childProjectileCount = Mathf.Max(0, projectileCount);
            childLaunchSpeed = Mathf.Max(0f, launchSpeed);
            childSpreadAngle = Mathf.Max(0f, spreadAngle);
            childDamageMultiplier = Mathf.Max(0f, damageMultiplier);
            childFuseTime = Mathf.Max(0f, childFuse);
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

            var currentPosition = (Vector2)transform.position;
            var step = ballistics.Step(dt);
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
            transform.rotation = ballistics.GetRotation();
        }

        private void Explode(Vector2 position)
        {
            if (exploded)
            {
                return;
            }

            exploded = true;

            var explosionSound = grenadeType == EGrenadeType.Fire
                ? EGrenadeSound.ExplosionFireGrenade
                : EGrenadeSound.ExplosionGrenade;
            SoundManager.Instance.PlayGrenadeExplosionSound(explosionSound);

            if (explosionEffect == null)
            {
                explosionEffect = GrenadeVisualResolver.GetExplosionEffect(grenadeType);
            }

            if (explosionEffect != null)
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(explosionEffect, position, Quaternion.identity);
                }
                else
                {
                    Instantiate(explosionEffect, position, Quaternion.identity);
                }
            }

            GrenadeExplosionDamageUtility.ApplyCircularDamage(position, ownerPlayerId, weaponName, ownerTeam, damage / 100f);
            SpawnChildProjectiles(position);
            Destroy(gameObject);
        }

        private void SpawnChildProjectiles(Vector2 position)
        {
            if (!spawnChildProjectiles || childProjectilePrefab == null || childProjectileCount <= 0)
            {
                return;
            }

            var baseAngle = UnityEngine.Random.Range(0f, 360f);
            for (var index = 0; index < childProjectileCount; index++)
            {
                var angleOffset = childProjectileCount == 1
                    ? 0f
                    : Mathf.Lerp(-childSpreadAngle, childSpreadAngle, (float)index / (childProjectileCount - 1));
                var angle = baseAngle + angleOffset;
                var direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var child = Instantiate(childProjectilePrefab, position, Quaternion.Euler(0f, 0f, angle));
                var controller = child.GetComponent<GrenadeProjectileController>();
                if (controller == null)
                {
                    continue;
                }

                controller.SetDamage(damage * childDamageMultiplier);
                controller.SetFuseTime(childFuseTime);
                controller.Launch(direction, childLaunchSpeed, ownerPlayerId, weaponName, ownerTeam, ownerTransform);
            }
        }
    }
}
