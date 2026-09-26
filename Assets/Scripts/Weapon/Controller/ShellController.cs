
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

        private void Awake()
        {
            lifetime = Mathf.Max(0.1f, float.IsFinite(lifetime) ? lifetime : 2.5f);
            destroyDelayAfterImpact = Mathf.Max(0f,
                float.IsFinite(destroyDelayAfterImpact) ? destroyDelayAfterImpact : 0.15f);
        }

        private void OnValidate()
        {
            if (!float.IsFinite(lifetime)) lifetime = 2.5f;
            if (!float.IsFinite(destroyDelayAfterImpact)) destroyDelayAfterImpact = 0.15f;
            lifetime = Mathf.Max(0.1f, lifetime);
            destroyDelayAfterImpact = Mathf.Max(0f, destroyDelayAfterImpact);
        }

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
            if (target == null)
            {
                return false;
            }

            var root = target.transform.root;
            if (target.CompareTag("StageObject") || target.CompareTag("BurstArea") ||
                (root != null && (root.CompareTag("StageObject") || root.CompareTag("BurstArea"))))
            {
                return true;
            }

            var tags = target.GetComponentInParent<MultipleTags>();
            if (tags != null)
            {
                return tags.Contains("StageObject") || tags.HasBurstAreaTag();
            }

            return target.layer == LayerMask.NameToLayer("Platforms") ||
                   (root != null && root.gameObject.layer == LayerMask.NameToLayer("Platforms"));
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
