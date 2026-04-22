
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class ShotgunBulletController : MonoBehaviour, IBulletController
    {
        public Rigidbody2D body;

        public float speed = 10.0f;

        private Vector2 rotation;

        private float count = 0;

        //[SerializeField][Required]public AudioClip hitSound;

        [SerializeField]
        [Required]
        public GameObject collisionEffectPrefab;

        void Start()
        {
            var vec = new Vector2(10, 10);

            var rotation = transform.rotation;


        }

        private void Update()
        {

            //gameObject.transform.Rotate(0, 0, 1.1f*Time.deltaTime);
        }

        private void FixedUpdate()
        {
            var velocity = body.linearVelocity;

            var rotation = transform.rotation;


            var rotationVolume = 1;





            if (count <= 180.0f)
            {
                gameObject.transform.Rotate(0, 0, -1.1f);

            }


            count += 1.1f;


        }


        private void OnTriggerEnter2D(Collider2D collision)
        {




            Destroy(gameObject);


        }

        public void EnableGravity()
        {

        }

        public void Speed(float f)
        {
            speed = f;


            body.AddForce(transform.right * speed, ForceMode2D.Impulse);
        }
    }
}
