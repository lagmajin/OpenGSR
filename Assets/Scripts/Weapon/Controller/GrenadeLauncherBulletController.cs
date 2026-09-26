
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
        [SerializeField] private float maxLifetime = 10f;


        [SerializeField] private GameObject explosion;
        private IEffectService effectService;
        private Transform ownerTransform;
        private bool exploded;
        private float lifetime;
        private readonly ProjectileBallistics2D ballistics = new ProjectileBallistics2D();

        private void Awake()
        {
            damage = Mathf.Max(0, damage);
            speed = float.IsFinite(speed) ? Mathf.Max(0f, speed) : 15f;
            gravityStrength = float.IsFinite(gravityStrength) ? Mathf.Max(0f, gravityStrength) : 18f;
            spriteAngleOffset = float.IsFinite(spriteAngleOffset) ? spriteAngleOffset : 0f;
            maxLifetime = float.IsFinite(maxLifetime) ? Mathf.Max(0.1f, maxLifetime) : 10f;
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0, damage);
            if (!float.IsFinite(speed)) speed = 15f;
            if (!float.IsFinite(gravityStrength)) gravityStrength = 18f;
            if (!float.IsFinite(spriteAngleOffset)) spriteAngleOffset = 0f;
            if (!float.IsFinite(maxLifetime)) maxLifetime = 10f;
            speed = Mathf.Max(0f, speed);
            gravityStrength = Mathf.Max(0f, gravityStrength);
            maxLifetime = Mathf.Max(0.1f, maxLifetime);
        }

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerPlayerId, string weaponName, ETeam team)
        {
            Init(direction, initSpeed, initDamage, ownerPlayerId, weaponName, team, null);
        }

        public void Init(Vector2 direction, float initSpeed, float initDamage, string ownerPlayerId, string weaponName, ETeam team, Transform owner)
        {
            speed = float.IsFinite(initSpeed) ? Mathf.Max(0f, initSpeed) : 0f;
            damage = float.IsFinite(initDamage)
                ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp(initDamage, 0f, int.MaxValue)), 0, int.MaxValue)
                : 0;
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            WeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Unknown" : weaponName;
            Team = team;
            ownerTransform = owner;
            exploded = false;
            lifetime = 0f;
            ballistics.Configure(direction, speed, enableGravity, gravityStrength, alignToVelocity, spriteAngleOffset);
            transform.rotation = ballistics.GetRotation();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f || !IsFinite(transform.position))
            {
                Destroy(gameObject);
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            var step = ballistics.Step(deltaTime);
            lifetime += deltaTime;
            if (!float.IsFinite(lifetime) || lifetime >= maxLifetime)
            {
                Explosion();
                return;
            }
            transform.position += (Vector3)step;
            transform.rotation = ballistics.GetRotation();
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        private void Explosion()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            if (explosion)
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(explosion, gameObject.transform.position, Quaternion.identity);
                }
                else
                {
                    var spawnedEffect = Instantiate(explosion);
                    spawnedEffect.transform.position = gameObject.transform.position;
                    Destroy(spawnedEffect, 5f);
                }
            }

            ApplyExplosionDamage();
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryExplode(collision != null ? collision.gameObject : null);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryExplode(collision != null ? collision.gameObject : null);
        }

        private void TryExplode(GameObject target)
        {
            if (target == null || ShouldIgnoreTarget(target))
            {
                return;
            }

            var tags = target.GetComponentInParent<IMultipleTags>();
            if (tags != null)
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

        private bool ShouldIgnoreTarget(GameObject target)
        {
            if (target == null || ownerTransform == null)
            {
                return false;
            }

            return target.transform == ownerTransform || target.transform.IsChildOf(ownerTransform);
        }

        public void EnableGravity()
        {
            enableGravity = true;
            ballistics.SetGravityEnabled(true);
        }






    }
}
