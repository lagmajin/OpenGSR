using System;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public abstract class TimedFieldItem : AbstractFieldItem
    {
        protected virtual float GetEffectDuration() => 30f;

        protected bool TryApplyToPlayer(Collider2D collision, Action<IPowerupable> apply)
        {
            if (collision == null || apply == null)
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

            apply(powerupable);
            Destroy(gameObject);
            return true;
        }

        protected bool TryApplyToPlayer(Collision2D collision, Action<IPowerupable> apply)
        {
            return collision != null && TryApplyToPlayer(collision.collider, apply);
        }
    }
}
