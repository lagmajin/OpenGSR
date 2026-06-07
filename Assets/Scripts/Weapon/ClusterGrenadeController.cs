
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
            if (effectService != null)
            {
                effectService.PlayOneShotEffect(expEffect, gameObject.transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(expEffect, gameObject.transform.position, Quaternion.identity);
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
            var damagePerChild = Mathf.Max(1f, damage / Mathf.Max(1, childGrenadeCount));

            for (var index = 0; index < childGrenadeCount; index++)
            {
                var angleOffset = childGrenadeCount == 1
                    ? 0f
                    : Mathf.Lerp(-childSpreadAngle, childSpreadAngle, (float)index / (childGrenadeCount - 1));
                var finalAngle = baseAngle + angleOffset;
                var direction = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));
                var child = Instantiate(childGrenadePrefab, transform.position, Quaternion.Euler(0f, 0f, finalAngle));

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
                ProjectileHitUtility.IsPlayerHit(collision.collider.gameObject))
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
                ProjectileHitUtility.IsPlayerHit(collision.gameObject))
            {
                Explosion();
            }
        }

    }




}
