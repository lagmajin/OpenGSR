using Sirenix.OdinInspector;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ShotgunBulletController : MonoBehaviour, IBulletController
    {
        public Rigidbody2D body;

        public float speed = 10.0f;
        [SerializeField] private int damage = 20;
        [SerializeField] private string ownerPlayerId = "";
        [SerializeField] private string weaponName = "Shotgun";

        public ETeam Team { get; set; } = ETeam.NoTeam;

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
            var player = collision.gameObject.GetComponentInParent<AbstractPlayer>();
            if (player != null)
            {
                if (!string.IsNullOrWhiteSpace(ownerPlayerId) && player.UniqueID().ToString() == ownerPlayerId)
                {
                    Destroy(gameObject);
                    return;
                }

                if (Team != ETeam.NoTeam && player.Team() != ETeam.NoTeam && player.Team() == Team)
                {
                    Destroy(gameObject);
                    return;
                }

                PlayerRegistry.Instance?.ApplyDamage(
                    player.UniqueID(),
                    player.transform.position - transform.position,
                    damage,
                    eDamageType.Bullet,
                    ownerPlayerId,
                    weaponName,
                    false
                );
            }

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

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerId, string weapon, ETeam team)
        {
            speed = initSpeed;
            damage = Mathf.RoundToInt(initDamage);
            ownerPlayerId = ownerId ?? string.Empty;
            weaponName = string.IsNullOrWhiteSpace(weapon) ? "Shotgun" : weapon;
            Team = team;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
