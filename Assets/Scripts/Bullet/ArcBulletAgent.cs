using UnityEngine;


namespace OpenGS
{


    public class ArcBulletAgent : AbstractBulletAgent
    {
        private Vector2 velocity;
        private float gravity = -9.8f;
        private float damage;
        public override void Launch(Vector2 direction, float speed, float damage=0)
        {

            this.velocity = direction.normalized * speed;
            this.damage = damage;

        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame

        void Update()
        {


            // èdóÕìKóp
            velocity.y += gravity * Time.deltaTime;

            // à⁄ìÆ
            transform.position += (Vector3)(velocity * Time.deltaTime);

            if (velocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        void OnColision()
        {


        }

    }


}