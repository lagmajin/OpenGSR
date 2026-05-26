
using OpenGSCore;
using UnityEngine;

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


        [SerializeField] private GameObject explosion;

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

        private void Update()
        {
            transform.position += transform.rotation * Vector3.right * (speed * Time.deltaTime);
        }

        private void Explosion()
        {
            if (explosion)
            {
                Instantiate(explosion).transform.position = gameObject.transform.position;
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






    }
}
