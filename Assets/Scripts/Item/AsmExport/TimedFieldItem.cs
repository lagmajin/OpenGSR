using System;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public abstract class TimedWorldItem : WorldItem
    {
        private bool consumed;

        protected virtual float GetEffectDuration() => 30f;

        protected float GetSafeEffectDuration()
        {
            var duration = GetEffectDuration();
            return float.IsFinite(duration) ? Mathf.Clamp(duration, 0.01f, 86400f) : 30f;
        }

        protected bool TryApplyToPlayer(Collider2D collision, Action<IPowerupable> apply)
        {
            if (consumed || collision == null || apply == null)
            {
                return false;
            }

            var tags = collision.GetComponentInParent<IMultipleTags>();
            if (tags == null)
            {
                return false;
            }

            if (!tags.HasPlayerTag() && !tags.HasMyPlayerTag() && !tags.HasBotTag())
            {
                return false;
            }

            var powerupable = collision.GetComponentInParent<IPowerupable>();
            if (powerupable == null)
            {
                return false;
            }

            consumed = true;

            // The effect is applied optimistically so the pickup feels instant,
            // but it is registered as pending: if the server refuses the claim
            // the revert runs and takes the buff back. The server also decides
            // the duration, and the grant re-applies with its value.
            var player = collision.GetComponentInParent<AbstractPlayer>();
            var playerId = player != null ? player.UniqueID().ToString() : string.Empty;
            var itemId = ClaimItemId(playerId);
            var duration = GetSafeEffectDuration();

            var manager = WorldItemNetworkManager.Instance;
            if (manager == null)
            {
                // No networking available, so there is nobody to answer and
                // nothing to take back: apply and move on.
                apply(powerupable);
                SendPickupToNetwork(collision, duration);
                Destroy(gameObject);
                return true;
            }

            if (!manager.BeginPendingPickup(
                    itemId,
                    playerId,
                    ResolveNetworkItemType(),
                    () => UndoBuff(powerupable)))
            {
                return false;
            }

            apply(powerupable);
            SendPickupToNetwork(collision, duration);
            Destroy(gameObject);
            return true;
        }

        protected bool TryApplyToPlayer(Collision2D collision, Action<IPowerupable> apply)
        {
            return collision != null && TryApplyToPlayer(collision.collider, apply);
        }

        /// <summary>
        /// Takes back a buff the client applied before the server answered.
        /// </summary>
        private static void UndoBuff(IPowerupable powerupable)
        {
            // Only a timed buff can be taken back. A refill or a heal has
            // already changed a number that cannot be un-applied, which is why
            // only the timed items register a revert.
            if (powerupable is AbstractPlayer player)
            {
                player.CancelTimedBuffs();
            }
        }

        private string ClaimItemId(string playerId)
        {
            var manager = WorldItemNetworkManager.Instance;
            if (manager != null && !string.IsNullOrEmpty(playerId))
            {
                return manager.SpawnItem(ResolveNetworkItemType(), transform.position);
            }

            return Guid.NewGuid().ToString("N");
        }
        private void SendPickupToNetwork(Collider2D collision, float duration)
        {
            var player = collision != null ? collision.GetComponentInParent<AbstractPlayer>() : null;
            if (player == null)
            {
                return;
            }

            var spawnPoint = GetComponentInParent<AbstractItemSpawnPoint>();
            var spawnPointId = spawnPoint is ItemSpawnPoint itemSpawnPoint ? itemSpawnPoint.SpawnPointId : -1;
            var itemType = ResolveNetworkItemType();

            NetworkEventSerializer.SerializeAndSend(new ItemPickupEvent(
                player.UniqueID().ToString(),
                FieldItemTypeNames.ToWireName(itemType),
                spawnPointId,
                (Vector2)transform.position,
                0f,
                duration));
        }

        /// <summary>
        /// The shared item type for this component, resolved from the class
        /// name so a new timed item does not need another switch arm.
        /// </summary>
        private EFieldItemType ResolveNetworkItemType()
        {
            return GetType().Name switch
            {
                nameof(PowerUpItem) => EFieldItemType.PowerUpItem,
                nameof(DefenceUpItem) => EFieldItemType.DefenceUpItem,
                nameof(SpeedUpItem) => EFieldItemType.SpeedUpItem,
                nameof(StealthItem) => EFieldItemType.StealthItem,
                nameof(NormalGrenadePackItem) => EFieldItemType.GrenadePack,
                _ => EFieldItemType.PowerUpItem
            };
        }
    }

    [System.Obsolete("Use TimedWorldItem instead.")]
    public abstract class TimedFieldItem : TimedWorldItem
    {
    }
}
