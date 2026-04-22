using UnityEngine;
using DG.Tweening;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class FireEffect : MonoBehaviour, IFireGrenadeEffect
    {
        private void Start()
        {

        }

        private void Update()
        {

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tags = collision.gameObject.GetComponent<IMultipleTags>();

            if (tags.HasPlayerTag())
            {

            }

            if (tags.HasStageObjectTag())
            {

            }

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {

        }


    }
}
