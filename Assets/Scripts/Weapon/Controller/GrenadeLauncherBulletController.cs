
using OpenGSCore;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class GrenadeLauncherBulletController : MonoBehaviour
    {
        //public Rigidbody2D body;
        public ETeam Team { get; set; } = ETeam.NoTeam;

        [SerializeField] public int damage = 120;
        [SerializeField] public float speed = 15.0f;


        [SerializeField] [Required] private Rigidbody2D _rigidbody;

        [SerializeField] private GameObject explosion;

        [SerializeField] private MultipleTags myTags;


        //[SerializeField] private float speed = 5.2f;

        void Start()
        {
            var position = transform.position;

            var rotate = transform.rotation;

            myTags = gameObject.GetComponent<MultipleTags>();

            //_rigidbody.velocity(rotate*10);


        }
        private void Update()
        {
            //float speed = 4.5f;
            Vector3 velocity = gameObject.transform.rotation * new Vector3(speed, 0, 0);
            gameObject.transform.position += velocity * Time.deltaTime;

        }

        public void DamageScaling(float x = 1.0f)
        {



        }

        private void Explosion()
        {
            if (explosion)
            {
                var exp = Instantiate(explosion);
                exp.transform.position = gameObject.transform.position;


            }

            Destroy(gameObject);


        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.TryGetComponent<IMultipleTags>(out var tags))
            {
                //Debug.LogError(tags.ToString());

                if (tags.HasPlayerTag())
                {



                    Explosion();
                }

                if (tags.HasStageObjectTag())
                {
                    Explosion();
                }

            }






            //Destroy(gameObject);


        }






    }
}
