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

        private void Awake()
        {
            lifetime = Mathf.Max(0.01f, float.IsFinite(lifetime) ? lifetime : 4f);
            rotationSpeed = float.IsFinite(rotationSpeed) ? rotationSpeed : 90f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(lifetime)) lifetime = 4f;
            if (!float.IsFinite(rotationSpeed)) rotationSpeed = 90f;
            lifetime = Mathf.Max(0.01f, lifetime);
        }

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            transform.Rotate(0f, 0f, rotationSpeed * deltaTime);
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

            var tags = other.GetComponentInParent<IMultipleTags>();
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
