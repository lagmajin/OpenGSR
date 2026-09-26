using UnityEngine;

namespace OpenGS
{
    public class GrenadeController : AbstractGrenadeController
    {
        private void Start()
        {
            body = gameObject.GetComponent<Rigidbody2D>();
            myTags = gameObject.GetComponent<MultipleTags>();
        }

        public override void Exp()
        {
            if (explosionTriggered)
            {
                return;
            }

            explosionTriggered = true;
            if (expEffect != null && effectService != null)
            {
                effectService.PlayOneShotEffect(expEffect, gameObject.transform.position, Quaternion.identity);
            }
            else if (expEffect != null)
            {
                var spawnedEffect = Instantiate(expEffect, gameObject.transform.position, Quaternion.identity);
                Destroy(spawnedEffect, 5f);
            }
            SoundManager.Instance?.PlayGameSound(EMatchSound.GameStartVoice);
            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var targetTags = collision != null && collision.collider != null
                ? collision.collider.GetComponentInParent<IMultipleTags>()
                : null;
            if (targetTags == null)
            {
                return;
            }

            if (targetTags.HasPlayerTag() && myTags != null && myTags.HasEnemyAttackTag())
            {
                Exp();
            }
        }
    }
}
