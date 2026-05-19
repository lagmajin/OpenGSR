using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class DefenceUpItem : TimedFieldItem
    {
        public float time = 30.0f;

        protected override float GetEffectDuration()
        {
            return time > 0f ? time : 30f;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryApplyToPlayer(collision, powerupable => powerupable.IncreaseDefense(GetEffectDuration()));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryApplyToPlayer(collision, powerupable => powerupable.IncreaseDefense(GetEffectDuration()));
        }
    }
}
