

using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class GrenadeExplosionController : MonoBehaviour
    {
        public float time = 1.0f;
        public float damage = 120.0f;
        public float force = 3.0f;
        public AudioClip expSound;
        [SerializeField]
        private Rigidbody2D body;

        [SerializeField]Vector2 size = new Vector2(1.0f, 1.0f);

        [SerializeField] LayerMask targetMask;
        [SerializeField] AudioSource audioSource;

        private void Start()
        {
            Debug.Log("in start func");
            //SoundManager.Instance.PlaySoundEffect(ESoundEffect.Explosion, 0.6f);





            Explosion();



            Destroy(this.gameObject, time);
        }

        void Explosion()
        {
            if(audioSource)
            {
                if(expSound)
                {
                    audioSource.PlayOneShot(expSound);
                }
            }

            //Debug.Log("In Explosion Func");

            //Vector2 size = new Vector2(1.0f, 1.0f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, size, 0f);

            // ヒットした対象に何かする例
            foreach (var hit in hits)
            {
                //hit.gameObject

                //var playerAgent = hit.gameObject.GetComponent<PlayerAgent>();

                if (hit.gameObject.TryGetComponent<PlayerAgent>(out PlayerAgent playerAgent))
                {
                    // ダメージの値と攻撃方向（例えば、爆風や攻撃方向）を渡す
                    //float damage = 20f;  // 例として固定値で設定
                    //Vector3 attackDirection = (hit.transform.position - transform.position).normalized;  // 攻撃方向を計算
                    //bool isExplosion = true;  // 爆風ダメージの場合

                    // ダメージを与える
                    playerAgent.TakeDamage();
                }

                Debug.Log($"ヒット: {hit.name}");
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector2 size = new Vector2(1.0f, 1.0f);
            Gizmos.DrawWireCube(transform.position,size);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.TryGetComponent<IMultipleTags>(out var tags))
            {

                if (tags.HasPlayerTag())
                {
                    var iDamage = collision.gameObject.GetComponent<IDamageable>();

                    if (iDamage != null)
                    {
                        var vec2 = new Vector2();

                        iDamage.AddDamageAndForce(damage, vec2);

                        //SoundManager.Instance.;



                    }


                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent<IMultipleTags>(out var tags))
            {

                if (tags.HasPlayerTag())
                {
                    var iDamage = collision.gameObject.GetComponent<IDamageable>();

                    if (iDamage != null)
                    {
                        var vec2 = new Vector2();

                        iDamage.AddDamageAndForce(damage, vec2);

                        //SoundManager.Instance.;

                    }


                }
            }


            //var tags = collision.gameObject.GetComponent<MultipleTags>();

            //var parent = collision.gameObject.transform.parent;








        }


    }

}
