
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
            const float radius = 2.5f;
            const float minDamageMultiplier = 0.35f;
            const float baseDamage = 100f;
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            var processed = new System.Collections.Generic.HashSet<string>();
            foreach (var hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                var player = hit.GetComponentInParent<AbstractPlayer>();
                if (player == null)
                {
                    continue;
                }

                var playerId = player.UniqueID().ToString();
                if (!processed.Add(playerId))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(resolvedOwnerId) && playerId == resolvedOwnerId)
                {
                    continue;
                }

                if (resolvedTeam != ETeam.NoTeam && player.Team() != ETeam.NoTeam && player.Team() == resolvedTeam)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, player.transform.position);
                float normalized = Mathf.Clamp01(distance / radius);
                float multiplier = Mathf.Lerp(1f, minDamageMultiplier, normalized);
                float finalDamage = Mathf.Max(1f, baseDamage * multiplier * (defaultDamage / 100f));

                PlayerRegistry.Instance?.ApplyDamage(
                    player.UniqueID(),
                    player.transform.position - transform.position,
                    finalDamage,
                    eDamageType.Explosion,
                    resolvedOwnerId,
                    weaponName,
                    false
                );
            }

            Destroy(this.gameObject, 0.1f);

        }


    }
}
