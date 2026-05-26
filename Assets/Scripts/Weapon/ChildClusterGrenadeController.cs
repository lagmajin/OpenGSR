
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class ChildClusterGrenadeController : AbstractGrenadeController
    {
       public float defaultDamage = 30.0f;
       private bool exploded;
       [SerializeField] private string ownerPlayerId = "";
       [SerializeField] private string weaponName = "ChildClusterGrenade";
       [SerializeField] private Rigidbody2D childBody;
       [SerializeField] private float launchSpeed = 8.0f;

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerId, string weapon)
        {
            var launchDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            launchSpeed = initSpeed;
            defaultDamage = initDamage;
            ownerPlayerId = ownerId ?? string.Empty;
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
            Explosion();

        }

        private void Explosion()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            Instantiate(expEffect, gameObject.transform.position, Quaternion.identity);
            var owner = GetOwnerPlayer();
            var resolvedOwnerId = !string.IsNullOrWhiteSpace(ownerPlayerId)
                ? ownerPlayerId
                : owner != null ? owner.UniqueID().ToString() : string.Empty;
            var resolvedTeam = owner != null ? owner.Team() : ETeam.NoTeam;
            GrenadeExplosionDamageUtility.ApplyCircularDamage((Vector2)transform.position, resolvedOwnerId, weaponName, resolvedTeam, defaultDamage / 100f);

            Destroy(this.gameObject, 0.1f);

        }


    }
}
