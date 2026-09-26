
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class ChildClusterGrenadeController : AbstractGrenadeController
    {
       public float defaultDamage = 30.0f;
        private bool exploded;
       // Child cluster grenades are projectiles only; they are never pickup items.
       [SerializeField] private string ownerPlayerId = "";
       [SerializeField] private ETeam ownerTeam = ETeam.NoTeam;
       [SerializeField] private string weaponName = "ChildClusterGrenade";
       [SerializeField] private Rigidbody2D childBody;
       [SerializeField] private float launchSpeed = 8.0f;

        private void OnValidate()
        {
            if (!float.IsFinite(defaultDamage)) defaultDamage = 30f;
            if (!float.IsFinite(launchSpeed)) launchSpeed = 8f;
            defaultDamage = Mathf.Max(0f, defaultDamage);
            launchSpeed = Mathf.Max(0f, launchSpeed);
        }

        private void Awake()
        {
            defaultDamage = Mathf.Max(0f, float.IsFinite(defaultDamage) ? defaultDamage : 30f);
            launchSpeed = Mathf.Max(0f, float.IsFinite(launchSpeed) ? launchSpeed : 8f);
        }

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerId, string weapon, ETeam team)
        {
            var launchDirection = float.IsFinite(direction.x) && float.IsFinite(direction.y) && direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
            launchSpeed = float.IsFinite(initSpeed) ? Mathf.Max(0f, initSpeed) : 0f;
            defaultDamage = float.IsFinite(initDamage) ? Mathf.Max(0f, initDamage) : 0f;
            ownerPlayerId = ownerId ?? string.Empty;
            ownerTeam = team;
            weaponName = string.IsNullOrWhiteSpace(weapon) ? "ChildClusterGrenade" : weapon;

            var body = childBody != null ? childBody : GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.linearVelocity = launchDirection * launchSpeed;
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
            var resolvedTeam = owner != null ? owner.Team() : ownerTeam;
            GrenadeExplosionDamageUtility.ApplyCircularDamage((Vector2)transform.position, resolvedOwnerId, weaponName, resolvedTeam, defaultDamage / 100f);

            Destroy(this.gameObject, 0.1f);

        }


    }
}
