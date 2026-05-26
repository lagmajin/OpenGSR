using UnityEngine;

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

        private void Start()
        {
            StartCoroutine(Functions.WaitAfterAction(Exp, expTime));
        }

        public virtual void Exp()
        {
            Instantiate(expEffect, gameObject.transform);
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
