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
            if (effectService != null)
            {
                effectService.PlayOneShotEffect(expEffect, gameObject.transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(expEffect).transform.position = gameObject.transform.position;
            }
            SoundManager.Instance.PlayGameSound(EMatchSound.GameStartVoice);
            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var targetTags = collision.gameObject.GetComponent<MultipleTags>();
            if (targetTags == null)
            {
                return;
            }

            if (targetTags.HasPlayerTag() && myTags.HasEnemyAttackTag())
            {
                Exp();
            }
        }
    }
}
