
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    class ClusterGrenadeController: AbstractGrenadeController
    {
        Coroutine c;
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

        private void Start()
        {
           //c= StartCoroutine(Functions.WaitAfterAction(Explosion, expTime));
        }

        void Update()
        {

        }
        private void Explosion()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            var obj=Instantiate(expEffect,gameObject.transform.position,Quaternion.identity);
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
                float finalDamage = Mathf.Max(1f, baseDamage * multiplier);

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
            //StopCoroutine(c);

            Explosion();
        }

    }




}
