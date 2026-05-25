using Sirenix.OdinInspector;
//using Unity.VisualScripting;
using UnityEngine;

namespace OpenGS
{
    public class FireGrenadeController : AbstractGrenadeController
    {
        [SerializeField] [Required] [AssetsOnly] public GameObject fireEffect;

        void Start()
        {

        }

        void Update()
        {

        }

        public override void Exp()
        {
            var fire = Instantiate(fireEffect, gameObject.transform);

            //fire.TryGetComponent(FireEffect)



            Destroy(gameObject, 0.3f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.tag == "StageObject")
            {

            }



            Exp();
        }

        //OnTriggerEnter2D(co)

    }


}
