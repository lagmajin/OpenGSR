using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class PowerUpItem : TimedWorldItem
    {
        public float time = 30.0f;

        protected override float GetEffectDuration()
        {
            return float.IsFinite(time) && time > 0f ? time : 30f;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryApplyToPlayer(collision, powerupable => powerupable.IncreaseAttack(GetEffectDuration()));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryApplyToPlayer(collision, powerupable => powerupable.IncreaseAttack(GetEffectDuration()));
        }
    }
}
