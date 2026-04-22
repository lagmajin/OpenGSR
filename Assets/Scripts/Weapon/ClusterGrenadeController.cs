
using UnityEngine;

namespace OpenGS
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    class ClusterGrenadeController: AbstractGrenadeController
    {
        Coroutine c;

        public GameObject childGrenadePrefab;

        public static string Description()
        {
            return " Grenade.";
        }

        private void Start()
        {
           //c= StartCoroutine(Functions.WaitAfterAction(Explosion, expTime));
        }

        void Update()
        {

        }
        private void Explosion()
        {
            var obj=Instantiate(expEffect,gameObject.transform.position,Quaternion.identity);
            
            
            Destroy(this.gameObject,0.3f);

      

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //StopCoroutine(c);

            Explosion();
        }

    }




}
