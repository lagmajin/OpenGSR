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

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        private void Start()
        {
            StartCoroutine(Functions.WaitAfterAction(Exp, expTime));
        }

        public virtual void Exp()
        {
            if (effectService != null)
            {
                effectService.PlayOneShotEffect(expEffect, transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(expEffect, gameObject.transform);
            }
            Destroy(this.gameObject);
        }

        public void StopMoving()
        {
            body.linearVelocity = new Vector2();
        }

        public void EnableGravity()
        {
            body.bodyType = RigidbodyType2D.Dynamic;
        }

        public void DisableGravity()
        {
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        protected AbstractPlayer GetOwnerPlayer()
        {
            return GetComponentInParent<AbstractPlayer>();
        }
    }
}
