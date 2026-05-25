
using UnityEngine;



namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SmokeGrenadeController : AbstractGrenadeController
    {
        public GameObject smokePrefab;

        private void Start()
        {

        }

        private void Update()
        {

        }

        public override void Exp()
        {
            Instantiate(expEffect);

            //Instantiate(smokePrefab);

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tags = collision.gameObject.GetComponent<MultipleTags>();

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
