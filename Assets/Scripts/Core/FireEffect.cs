using UnityEngine;
using DG.Tweening;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class FireEffect : MonoBehaviour, IFireGrenadeEffect
    {
        [SerializeField] private float lifetime = 4.0f;
        [SerializeField] private float rotationSpeed = 90f;
        private bool triggered;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TriggerImpact(collision != null ? collision.gameObject : null);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TriggerImpact(collision != null ? collision.gameObject : null);
        }

        private void TriggerImpact(GameObject other)
        {
            if (triggered || other == null)
            {
                return;
            }

            var tags = other.GetComponent<MultipleTags>();
            if (tags == null)
            {
                return;
            }

            if (tags.HasPlayerTag() || tags.HasStageObjectTag())
            {
                triggered = true;
                Destroy(gameObject, 0.1f);
            }
        }


    }
}
