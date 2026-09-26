
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class HealItem : WorldItem
    {
        public AudioClip takeSound;
        [SerializeField] private float healAmount = 25f;

        // Start is called before the first frame update

        static public float defalutTime()
        {
            return 30.0f;
        }

        public float cantTakeTime=3.0f;
        public float time = 30.0f;
        private bool consumed;
        //private int heal = 25;

       // private float fHeal = 0.25f;

        public void OnTriggerEnter2D(Collider2D collision)
        {
            TryConsume(collision != null ? collision.GetComponentInParent<AbstractPlayer>() : null,
                collision != null ? collision.GetComponentInParent<IMultipleTags>() : null);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var collider = collision != null ? collision.collider : null;
            TryConsume(collider != null ? collider.GetComponentInParent<AbstractPlayer>() : null,
                collider != null ? collider.GetComponentInParent<IMultipleTags>() : null);
        }

        private void TryConsume(AbstractPlayer player, IMultipleTags tags)
        {
            if (consumed || player == null || !float.IsFinite(healAmount) || healAmount <= 0f)
            {
                return;
            }

            if (tags == null || (!tags.HasPlayerTag() && !tags.HasMyPlayerTag() && !tags.HasBotTag()))
            {
                return;
            }

            consumed = true;
            player.Heal(healAmount);
            SendPickupToNetwork(player);
            if (takeSound != null)
            {
                AudioSource.PlayClipAtPoint(takeSound, transform.position);
            }

            Destroy(gameObject);
        }

        private void SendPickupToNetwork(AbstractPlayer player)
        {
            var spawnPoint = point as ItemSpawnPoint ?? GetComponentInParent<ItemSpawnPoint>();
            var spawnPointId = spawnPoint != null ? spawnPoint.SpawnPointId : -1;
            var playerId = player.UniqueID().ToString();

            NetworkEventSerializer.SerializeAndSend(new ItemPickupEvent(
                playerId,
                nameof(HealItem),
                spawnPointId,
                transform.position,
                healAmount,
                0f));

            NetworkEventSerializer.SerializeAndSend(new BuffEvent(
                playerId,
                "HpRecovery",
                0,
                healAmount,
                false));
        }


    }

}
