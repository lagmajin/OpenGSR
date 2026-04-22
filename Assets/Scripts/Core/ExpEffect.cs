
//using Cinemachine;
using UnityEngine;

namespace OpenGS
{
    //#Explosion
    [DisallowMultipleComponent]
    public class ExpEffect : MonoBehaviour
    {
        public float time = 1.0f;

        [SerializeField] private Rigidbody2D body;

        private void Start()
        {
            Destroy(gameObject, time);
        }

        private void Update()
        {

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.TryGetComponent<MultipleTags>(out var tags))
            {
                if (tags.HasPlayerTag())
                {
                    if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
                    {
                        damageable.AddDamageAndForce(120, new Vector3(0, 0, 0), 1.0f);
                    }
                }

            }


        }

    }

}
