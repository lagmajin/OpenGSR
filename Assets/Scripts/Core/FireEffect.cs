using UnityEngine;
using DG.Tweening;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class FireEffect : MonoBehaviour, IFireGrenadeEffect
    {
        private void Start()
        {
            Destroy(gameObject, 4.0f);
        }

        private void Update()
        {
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.gameObject == null)
            {
                return;
            }

            var tags = collision.gameObject.GetComponent<MultipleTags>();
            if (tags == null)
            {
                return;
            }

            if (tags.HasPlayerTag() || tags.HasStageObjectTag())
            {
                Destroy(gameObject, 0.1f);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision != null && collision.gameObject != null && collision.gameObject.CompareTag("StageObject"))
            {
                Destroy(gameObject, 0.1f);
            }
        }


    }
}
