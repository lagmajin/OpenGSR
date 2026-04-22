
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;


namespace OpenGS
{
    
    public class GrenadeController : AbstractGrenadeController
    {



        private void Start()
        {
            //StartCoroutine(ExpCor(expTime));

            SetVariables();
        }




        private void Update()
        {

        }


        private void OnValidate()
        {

        }


        [Button("テスト")]
        public void Exp()
        {
            Debug.Log("Exp2");
            var explosion = Instantiate(expEffect);
            explosion.transform.position = gameObject.transform.position;

            var soundManagerInstance = SoundManager.Instance;

            soundManagerInstance.PlayGameSound(EMatchSound.GameStartVoice);


            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var targetTags = collision.gameObject.GetComponent<MultipleTags>();

            Debug.Log(targetTags.ToJson().ToString());
            Debug.Log(myTags.ToJson().ToString());

            if ("StageObject" == collision.gameObject.tag)
            {
                Debug.Log("collision");
            }

            if (targetTags.HasPlayerTag())
            {
                if (myTags.HasEnemyAttackTag())
                {


                    Exp();

                    if (collision.gameObject.TryGetComponent<IDamageable>(out var t))
                    {
                        Debug.LogError("Test");

                    }
                }


            }


            //if(tags.HasPlayerTag())



        }

    }




}
