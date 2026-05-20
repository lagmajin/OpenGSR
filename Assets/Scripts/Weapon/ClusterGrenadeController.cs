
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    class ClusterGrenadeController: AbstractGrenadeController
    {
        Coroutine c;

        public GameObject childGrenadePrefab;
        [SerializeField] private GrenadeExplosionMasterData explosionMasterData;
        [SerializeField] private string ownerPlayerId = "";
        [SerializeField] private string weaponName = "ClusterGrenade";

        public static string Description()
        {
            return " Grenade.";
        }

        private void Start()
        {
           //c= StartCoroutine(Functions.WaitAfterAction(Explosion, expTime));
        }

        void Update()
        {

        }
        private void Explosion()
        {
            var obj=Instantiate(expEffect,gameObject.transform.position,Quaternion.identity);
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
                resolvedTeam
            );

            Destroy(this.gameObject,0.3f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //StopCoroutine(c);

            Explosion();
        }

    }




}
