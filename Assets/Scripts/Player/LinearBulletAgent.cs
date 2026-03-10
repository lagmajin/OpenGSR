using System.Collections;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class LinearBulletAgent : AbstractBulletAgent
    {
        public float Speed;
        private Vector2 velocity;
        [SerializeField] LayerMask layerMask;

        public override void Launch(Vector2 direction, float speed, float damage=0)
        {
            Speed = speed;

            velocity = direction.normalized * Speed;

            Damage = damage;
        }
        void Start()
        {
            Destroy(gameObject, 5f);
            //Launch(transform.up);
        }
        void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);

            OnColision();
        }

        void OnColision()
        {
            Vector2 direction = transform.up;
            var hit = Physics2D.Raycast(transform.position, direction,Speed * Time.deltaTime,layerMask);

            if (hit.collider != null)
            {
                // 衝突対象を取得


                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Platforms"))
                {
                  PlaySound(ESoundEffect.HitStageObject);

                    Debug.Log("衝突した: " + hit.collider.gameObject.name);

                    Instantiate(hitEffect, hit.point, Quaternion.identity);

                }

                // 衝突した場合、銃弾を削除する
                Destroy(gameObject);
            }
        }
        // Use this for initialization


        // Update is called once per frame

    }
}