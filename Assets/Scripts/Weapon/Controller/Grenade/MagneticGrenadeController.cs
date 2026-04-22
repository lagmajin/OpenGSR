using System.Collections;
using UnityEngine;


namespace OpenGS
{
    public class MagneticGrenadeController : AbstractGrenadeController
    {


        void Start()
        {


            StartCoroutine(Functions.WaitAfterAction(Explosion, expTime));


        }

        private void Explosion()
        {
            Destroy(this.gameObject);

            Instantiate(expEffect);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //var tags=collision.gameObject.GetComponent<MultipleTags>();

            if (collision.gameObject.TryGetComponent<MultipleTags>(out var tag))
            {
                if (tag)
                {
                    if (tag.HasPlayerAndEnemyTags())
                    {


                    }
                    else if (tag.HasStageObjectTag())
                    {


                    }


                }


            }




            /*
            if(tags.HasStageObjectTag())
            {
                StopMoving();
                DisableGravity();
            }

            

            if(tags.HasPlayerTag())
            {
                Explosion();
            }

            if(tags.HasBurstAreaTag())
            {

            }
            */

        }




    }


}
