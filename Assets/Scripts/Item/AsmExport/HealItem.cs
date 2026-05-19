
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class HealItem : AbstractFieldItem
    {
        public AudioClip takeSound;
        [SerializeField] private float healAmount = 25f;

        // Start is called before the first frame update

        static public float defalutTime()
        {
            return 30.0f;
        }

        public float cantTakeTime=3.0f;
        public float time = 30.0f;
        //private int heal = 25;

       // private float fHeal = 0.25f;

        void Start()
        {

        }

        void Remove()
        {

        }

        public void OnTriggerEnter2D(Collider2D collision)
        {
            var tags=collision.GetComponent<MultipleTags>();

            if(tags != null && (tags.HasPlayerTag() || tags.HasMyPlayerTag() || tags.HasBotTag()))
            {
                var player = collision.GetComponent<AbstractPlayer>();
                if (player != null)
                {
                    player.Heal(healAmount);
                }

                Destroy(gameObject);
            }


        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var tags = collision.gameObject.GetComponent<MultipleTags>();

            if (tags != null && (tags.HasPlayerTag() || tags.HasMyPlayerTag() || tags.HasBotTag()))
            {
                var player = collision.gameObject.GetComponent<AbstractPlayer>();
                if (player != null)
                {
                    player.Heal(healAmount);
                }

                Destroy(gameObject);
            }
        }


    }

}
