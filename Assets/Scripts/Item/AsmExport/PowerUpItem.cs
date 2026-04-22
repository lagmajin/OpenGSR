
using System;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class PowerUpItem : AbstractFieldItem
    {
        public float time = 30.0f;
        private int heal = 25;

        private float fHeal = 0.25f;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<IMultipleTags>(out var tags))
            {

                if (tags.HasPlayerTag())
                {
                    if (collision.gameObject.TryGetComponent<IPowerupable>(out var powerupable))
                    {

                        powerupable.IncreaseAttack(30f);



                        Destroy(gameObject);

                    }

                }

                if (tags.HasMyPlayerTag())
                {

                }

            }


        }


        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent<IMultipleTags>(out var tags))
            {



                if (tags.HasPlayerTag())
                {

                }

                if (tags.HasMyPlayerTag())
                {
                    Debug.Log("tagok");
                    if (collision.gameObject.TryGetComponent<IPowerupable>(out var powerupable))
                    {
                        Debug.Log("tagok2");

                        powerupable.IncreaseAttack(30.0f);


                        Destroy(gameObject);

                    }

                }





            }

        }
    }

}