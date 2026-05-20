
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
        public string OwnerPlayerId { get; private set; } = string.Empty;
        public string WeaponName { get; private set; } = "Unknown";

        [SerializeField] public int damage = 120;
        [SerializeField] public float speed = 15.0f;
        [SerializeField] private GrenadeExplosionMasterData explosionMasterData;


        [SerializeField] [Required] private Rigidbody2D _rigidbody;

        [SerializeField] private GameObject explosion;

        [SerializeField] private MultipleTags myTags;


        //[SerializeField] private float speed = 5.2f;

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerPlayerId, string weaponName, ETeam team)
        {
            speed = initSpeed;
            damage = Mathf.RoundToInt(initDamage);
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            WeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Unknown" : weaponName;
            Team = team;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

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

            ApplyExplosionDamage();
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
        }

        private void ApplyExplosionDamage()
        {
            GrenadeExplosionDamageUtility.ApplyCircularDamage(
                (Vector2)transform.position,
                explosionMasterData,
                OwnerPlayerId,
                WeaponName,
                Team,
                damage / Mathf.Max(1f, explosionMasterData != null ? explosionMasterData.BaseDamage() : 100f)
            );
        }






    }
}
