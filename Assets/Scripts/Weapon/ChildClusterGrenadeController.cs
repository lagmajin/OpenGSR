
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class ChildClusterGrenadeController : AbstractGrenadeController
    {
       public float defaultDamage = 30.0f;
       [SerializeField] private GrenadeExplosionMasterData explosionMasterData;
       [SerializeField] private string ownerPlayerId = "";
       [SerializeField] private string weaponName = "ChildClusterGrenade";


        private void Start()
        {

        }

        void Update()
        {

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Explosion();

        }

        private void Explosion()
        {
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

            Destroy(this.gameObject, 0.3f);

        }


    }
}
