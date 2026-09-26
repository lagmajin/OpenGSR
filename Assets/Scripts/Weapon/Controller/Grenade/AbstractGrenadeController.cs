using UnityEngine;
using Zenject;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class AbstractGrenadeController : MonoBehaviour
    {
        [SerializeField]
        public float damage = 0;
        [SerializeField]
        public float expTime = 3.0f;
        [SerializeField]
        public GameObject expEffect;
        [SerializeField]
        public Rigidbody2D body;

        public MultipleTags myTags;
        protected IEffectService effectService;
        protected bool explosionTriggered;

        private void OnValidate()
        {
            if (!float.IsFinite(damage) || damage < 0f) damage = 0f;
            if (!float.IsFinite(expTime) || expTime < 0.01f) expTime = 3f;
        }

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        private void Start()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
            var safeExpTime = float.IsFinite(expTime) ? Mathf.Max(0.01f, expTime) : 3f;
            StartCoroutine(Functions.WaitAfterAction(Exp, safeExpTime));
        }

        public virtual void Exp()
        {
            if (explosionTriggered)
            {
                return;
            }

            explosionTriggered = true;
            if (expEffect != null && effectService != null)
            {
                effectService.PlayOneShotEffect(expEffect, transform.position, Quaternion.identity);
            }
            else if (expEffect != null)
            {
                var spawnedEffect = Instantiate(expEffect, gameObject.transform);
                Destroy(spawnedEffect, 5f);
            }
            Destroy(this.gameObject);
        }

        public void StopMoving()
        {
            var rigidbody = body != null ? body : GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector2.zero;
            }
        }

        public void EnableGravity()
        {
            var rigidbody = body != null ? body : GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        public void DisableGravity()
        {
            var rigidbody = body != null ? body : GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        protected AbstractPlayer GetOwnerPlayer()
        {
            return GetComponentInParent<AbstractPlayer>();
        }
    }
}
