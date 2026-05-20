
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class ChildClusterGrenadeController : AbstractGrenadeController
    {
       public float defaultDamage = 30.0f;
       private bool exploded;
       [SerializeField] private GrenadeExplosionMasterData explosionMasterData;
       [SerializeField] private string ownerPlayerId = "";
       [SerializeField] private string weaponName = "ChildClusterGrenade";
       [SerializeField] private Rigidbody2D childBody;
       [SerializeField] private float launchSpeed = 8.0f;
       private Vector2 launchDirection = Vector2.right;


        private void Start()
        {

        }

        void Update()
        {

        }

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerId, string weapon, ETeam team)
        {
            launchDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
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
            var obj = Instantiate(expEffect, gameObject.transform.position, Quaternion.identity);
            var owner = GetComponentInParent<AbstractPlayer>();
            var resolvedOwnerId = !string.IsNullOrWhiteSpace(ownerPlayerId)
                ? ownerPlayerId
                : owner != null ? owner.UniqueID().ToString() : string.Empty;
            var resolvedTeam = owner != null ? owner.Team() : ETeam.NoTeam;
            GrenadeExplosionDamageUtility.ApplyCircularDamage(
                (Vector2)transform.position,
                explosionMasterData,
                resolvedOwnerId,
                weaponName,
                resolvedTeam,
                defaultDamage / Mathf.Max(1f, explosionMasterData != null ? explosionMasterData.BaseDamage() : defaultDamage)
            );

            Destroy(this.gameObject, 0.1f);

        }


    }
}
