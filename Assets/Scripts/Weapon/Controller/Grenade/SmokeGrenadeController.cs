
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
            if (explosionTriggered)
            {
                return;
            }

            explosionTriggered = true;
            var effectPrefab = smokePrefab != null ? smokePrefab : expEffect;
            if (effectPrefab != null)
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(effectPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    var spawnedEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
                    Destroy(spawnedEffect, 5f);
                }
            }

            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tags = collision != null && collision.collider != null
                ? collision.collider.GetComponentInParent<IMultipleTags>()
                : null;
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
