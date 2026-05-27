
using UnityEngine;



namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SmokeGrenadeController : AbstractGrenadeController
    {
        public GameObject smokePrefab;

        public override void Exp()
        {
            var effectPrefab = smokePrefab != null ? smokePrefab : expEffect;
            if (effectPrefab != null)
            {
                Instantiate(effectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tags = collision.gameObject.GetComponent<MultipleTags>();
            if (tags == null)
            {
                return;
            }

            if (tags.HasPlayerTag())
            {
                Exp();
            }

            if(tags.HasStageObjectTag())
            {
                Exp();
            }

            if(tags.HasBurstAreaTag())
            {
                Destroy(gameObject);
            }


        }


    }



}
