
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShellController : MonoBehaviour
    {
        [SerializeField] private AudioClip shellSound;
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;
        [SerializeField, Min(0f)] private float destroyDelayAfterImpact = 0.15f;

        private bool hasImpacted;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (hasImpacted)
            {
                return;
            }

            if (collision == null || collision.gameObject == null)
            {
                return;
            }

            if (IsGroundLike(collision.gameObject))
            {
                hasImpacted = true;
                PlayImpactSound();
                Destroy(gameObject, destroyDelayAfterImpact);
            }
        }

        private bool IsGroundLike(GameObject target)
        {
            if (target.CompareTag("StageObject") || target.CompareTag("BurstArea"))
            {
                return true;
            }

            if (target.TryGetComponent<MultipleTags>(out var tags))
            {
                return tags.Contains("StageObject") || tags.HasBurstAreaTag();
            }

            return target.layer == LayerMask.NameToLayer("Platforms");
        }

        private void PlayImpactSound()
        {
            if (shellSound == null)
            {
                return;
            }

            SoundManager.Instance?.PlayOneShotSafe(shellSound, context: nameof(ShellController));
        }
    }
}
