using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class GrenadeBulletAgent:AbstractBulletAgent
    {
        private Vector2 velocity;
        private float gravity = -9.8f;
        private float damage=0;
        [SerializeField]private GameObject explosionPrefab;
        [SerializeField] private LayerMask layerMask;

        float Speed = 0;
        public override void Launch(Vector2 direction, float speed, float damage = 0)
        {
            Speed = speed;
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
            velocity.y += gravity * Time.deltaTime;

            // à⁄ìÆ
            transform.position += (Vector3)(velocity * Time.deltaTime);

            if (velocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            OnColision();
        }


        void OnColision()
        {
            Vector2 direction = transform.up;
            var hit = Physics2D.Raycast(transform.position, direction, Speed * Time.deltaTime, layerMask);

            if (hit.collider != null)
            {
                // è’ìÀëŒè€ÇéÊìæ


                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Platforms"))
                {
                    PlaySound(ESoundEffect.HitStageObject);

                    Debug.Log("è’ìÀÇµÇΩ: " + hit.collider.gameObject.name);

                    //Instantiate(hitEffect, hit.point, Quaternion.identity);

                    Explosion();
                }

                // è’ìÀÇµÇΩèÍçáÅAèeíeÇçÌèúÇ∑ÇÈ
                Destroy(gameObject);
            }
        }

        void Explosion()
        {
            if(explosionPrefab!=null)
            {
                Instantiate(explosionPrefab);
            }

        }

    }


}