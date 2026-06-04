
using OpenGSCore;
using UnityEngine;
using Zenject;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class GrenadeLauncherBulletController : MonoBehaviour
    {
        public ETeam Team { get; set; } = ETeam.NoTeam;
        public string OwnerPlayerId { get; private set; } = string.Empty;
        public string WeaponName { get; private set; } = "Unknown";

        [SerializeField] public int damage = 120;
        [SerializeField] public float speed = 15.0f;
        [SerializeField] public bool enableGravity = false;
        [SerializeField] private float gravityStrength = 18f;
        [SerializeField] private bool alignToVelocity = true;
        [SerializeField] private float spriteAngleOffset = 0f;


        [SerializeField] private GameObject explosion;
        private IEffectService effectService;
        private readonly ProjectileBallistics2D ballistics = new ProjectileBallistics2D();

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerPlayerId, string weaponName, ETeam team)
        {
            speed = initSpeed;
            damage = Mathf.RoundToInt(initDamage);
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            WeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Unknown" : weaponName;
            Team = team;
            ballistics.Configure(direction, speed, enableGravity, gravityStrength, alignToVelocity, spriteAngleOffset);
            transform.rotation = ballistics.GetRotation();
        }

        private void Update()
        {
            var step = ballistics.Step(Time.deltaTime);
            transform.position += (Vector3)step;
            transform.rotation = ballistics.GetRotation();
        }

        private void Explosion()
        {
            if (explosion)
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(explosion, gameObject.transform.position, Quaternion.identity);
                }
                else
                {
                    Instantiate(explosion).transform.position = gameObject.transform.position;
                }
            }

            ApplyExplosionDamage();
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.TryGetComponent<IMultipleTags>(out var tags))
            {
                if (tags.HasPlayerTag() || tags.HasStageObjectTag())
                {
                    Explosion();
                }

            }
        }

        private void ApplyExplosionDamage()
        {
            GrenadeExplosionDamageUtility.ApplyCircularDamage((Vector2)transform.position, OwnerPlayerId, WeaponName, Team, damage / 100f);
        }

        public void EnableGravity()
        {
            enableGravity = true;
            ballistics.SetGravityEnabled(true);
        }






    }
}
