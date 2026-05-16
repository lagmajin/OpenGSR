
using UnityEngine;
using DG.Tweening;



#pragma warning disable 0414

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ShellController : MonoBehaviour
    {
        public AudioClip shellSound;

        float activeTime = 2.0f;

        private void Start()
        {
            //var seq =new  Sequence();
        }

        private void Update()
        {

        }

        private void HitStageObject()
        {
            SoundManager.Instance.PlayOneShotSafe(shellSound, context: nameof(ShellController));
            Destroy(gameObject);
        }



        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tags = collision.gameObject.GetComponent<MultipleTags>();

            if (tags.Contains("StageObject"))
            {
                HitStageObject();
            }

            if (tags.HasBurstAreaTag())
            {

            }


        }


    }
}
