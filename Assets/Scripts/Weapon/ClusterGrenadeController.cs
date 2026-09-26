
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    class ClusterGrenadeController: AbstractGrenadeController
    {
        private bool exploded;

        public GameObject childGrenadePrefab;
        [SerializeField] private int childGrenadeCount = 3;
        [SerializeField] private float childLaunchSpeed = 8f;
        [SerializeField] private float childSpreadAngle = 45f;
        [SerializeField] private string ownerPlayerId = "";
        [SerializeField] private string weaponName = "ClusterGrenade";

        private void OnValidate()
        {
            if (!float.IsFinite(childLaunchSpeed)) childLaunchSpeed = 8f;
            if (!float.IsFinite(childSpreadAngle)) childSpreadAngle = 45f;
            childGrenadeCount = Mathf.Clamp(childGrenadeCount, 0, 32);
            childLaunchSpeed = Mathf.Max(0f, childLaunchSpeed);
            childSpreadAngle = Mathf.Clamp(childSpreadAngle, 0f, 180f);
        }

        private void Awake()
        {
            childGrenadeCount = Mathf.Clamp(childGrenadeCount, 0, 32);
            childLaunchSpeed = Mathf.Max(0f, float.IsFinite(childLaunchSpeed) ? childLaunchSpeed : 8f);
            childSpreadAngle = Mathf.Clamp(float.IsFinite(childSpreadAngle) ? childSpreadAngle : 45f, 0f, 180f);
        }

        public static string Description()
        {
            return " Grenade.";
        }

        private void Explosion()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            if (expEffect != null && effectService != null)
            {
                effectService.PlayOneShotEffect(expEffect, gameObject.transform.position, Quaternion.identity);
            }
            else
            {
                if (expEffect != null)
                {
                    var spawnedEffect = Instantiate(expEffect, gameObject.transform.position, Quaternion.identity);
                    Destroy(spawnedEffect, 5f);
                }
            }
            var owner = GetOwnerPlayer();
            var resolvedOwnerId = !string.IsNullOrWhiteSpace(ownerPlayerId)
                ? ownerPlayerId
                : owner != null ? owner.UniqueID().ToString() : string.Empty;
            var resolvedTeam = owner != null ? owner.Team() : ETeam.NoTeam;
            GrenadeExplosionDamageUtility.ApplyCircularDamage((Vector2)transform.position, resolvedOwnerId, weaponName, resolvedTeam);

            SpawnChildGrenades(resolvedOwnerId, resolvedTeam);

            Destroy(this.gameObject,0.1f);
        }

        private void SpawnChildGrenades(string resolvedOwnerId, ETeam resolvedTeam)
        {
            if (childGrenadePrefab == null || childGrenadeCount <= 0)
            {
                return;
            }

            var baseAngle = Random.Range(0f, 360f);
            var damagePerChild = float.IsFinite(damage) ? Mathf.Max(1f, damage / Mathf.Max(1, childGrenadeCount)) : 1f;

            for (var index = 0; index < childGrenadeCount; index++)
            {
                var angleOffset = childGrenadeCount == 1
                    ? 0f
                    : Mathf.Lerp(-childSpreadAngle, childSpreadAngle, (float)index / (childGrenadeCount - 1));
                var finalAngle = baseAngle + angleOffset;
                var direction = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));
                var child = Instantiate(childGrenadePrefab, transform.position, Quaternion.Euler(0f, 0f, finalAngle));
                if (child == null)
                {
                    continue;
                }

                var childController = child.GetComponent<ChildClusterGrenadeController>();
                if (childController != null)
                {
                    childController.Init(direction, childLaunchSpeed, damagePerChild, resolvedOwnerId, weaponName, resolvedTeam);
                }

                var childRigidbody = child.GetComponent<Rigidbody2D>();
                if (childRigidbody != null)
                {
                    childRigidbody.bodyType = RigidbodyType2D.Dynamic;
                    childRigidbody.linearVelocity = direction * childLaunchSpeed;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider == null)
            {
                return;
            }

            if (ProjectileHitUtility.IsStageHit(collision.collider.gameObject) ||
                ProjectileHitUtility.TryGetTargetPlayer(collision.collider, out _))
            {
                Explosion();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            if (ProjectileHitUtility.IsStageHit(collision.gameObject) ||
                ProjectileHitUtility.TryGetTargetPlayer(collision, out _))
            {
                Explosion();
            }
        }

    }




}
