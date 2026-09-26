using System;
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
            apply(powerupable);
            SendPickupToNetwork(collision, GetSafeEffectDuration());
            Destroy(gameObject);
            return true;
        }

        protected bool TryApplyToPlayer(Collision2D collision, Action<IPowerupable> apply)
        {
            return collision != null && TryApplyToPlayer(collision.collider, apply);
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
                itemType,
                spawnPointId,
                (Vector2)transform.position,
                0f,
                duration));
        }

        private string ResolveNetworkItemType()
        {
            return GetType().Name switch
            {
                nameof(PowerUpItem) => OpenGSCore.EFieldItemType.PowerUpItem.ToString(),
                nameof(DefenceUpItem) => OpenGSCore.EFieldItemType.DefenceUpItem.ToString(),
                nameof(SpeedUpItem) => OpenGSCore.EFieldItemType.SpeedUpItem.ToString(),
                nameof(StealthItem) => OpenGSCore.EFieldItemType.StealthItem.ToString(),
                nameof(NormalGrenadePackItem) => OpenGSCore.EFieldItemType.GrenadePack.ToString(),
                _ => GetType().Name
            };
        }
    }

    [System.Obsolete("Use TimedWorldItem instead.")]
    public abstract class TimedFieldItem : TimedWorldItem
    {
    }
}
