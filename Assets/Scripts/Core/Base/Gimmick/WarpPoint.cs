using UnityEngine;

namespace OpenGS
{
    public interface IWarpPoint
    {
    }

    [DisallowMultipleComponent]
    public class WarpPoint : MonoBehaviour, IWarpPoint
    {
        [SerializeField] private float warpCooldown = 2.0f;
        [SerializeField] private float effectLifetime = 2.0f;

        public bool enableWarp = true;
        public GameObject point1;
        public GameObject point2;
        public GameObject warpEffectPosition;
        public GameObject warpEffect;
        public AudioClip warpsound;

        private void Awake()
        {
            warpCooldown = float.IsFinite(warpCooldown) ? Mathf.Max(0.1f, warpCooldown) : 2f;
            effectLifetime = float.IsFinite(effectLifetime) && effectLifetime > 0f ? Mathf.Min(effectLifetime, 5f) : 2f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryWarp(collision != null ? collision.collider : null);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryWarp(collision);
        }

        private void TryWarp(Collider2D collider)
        {
            if (!enableWarp || collider == null)
            {
                return;
            }

            var player = collider.GetComponentInParent<AbstractPlayer>();
            var destination = ResolveDestination();
            if (player == null || destination == null || !player.CanWarp())
            {
                return;
            }

            player.Warp(Mathf.Max(0.1f, warpCooldown));
            player.transform.position = destination.position;
            SpawnWarpEffect(destination.position);

            if (warpsound != null)
            {
                AudioSource.PlayClipAtPoint(warpsound, destination.position);
            }
        }

        private Transform ResolveDestination()
        {
            if (point1 == null || point2 == null || point1 == point2)
            {
                return null;
            }

            var point1Distance = (transform.position - point1.transform.position).sqrMagnitude;
            var point2Distance = (transform.position - point2.transform.position).sqrMagnitude;
            return point1Distance <= point2Distance ? point2.transform : point1.transform;
        }

        private void SpawnWarpEffect(Vector3 position)
        {
            if (warpEffect == null)
            {
                return;
            }

            var spawnPosition = warpEffectPosition != null
                ? warpEffectPosition.transform.position
                : position;
            var effect = Instantiate(warpEffect, spawnPosition, Quaternion.identity);
            Destroy(effect, effectLifetime);
        }
    }
}
