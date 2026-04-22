using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MineGrenadeController : AbstractGrenadeController
    {
        private void Start()
        {
            //StartCoroutine(ExpCor(expTime));
        }

        void Update()
        {
            var time = Time.deltaTime;


        }

        private void Explosion()
        {
            var obj = Instantiate(expEffect, gameObject.transform);

            Destroy(this.gameObject);

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tag=collision.gameObject.GetComponent<MultipleTags>();

           
            

        }


    }
}
