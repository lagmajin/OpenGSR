
using UnityEngine;
using Zenject;



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
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(effectPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    Instantiate(effectPrefab, transform.position, Quaternion.identity);
                }
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
