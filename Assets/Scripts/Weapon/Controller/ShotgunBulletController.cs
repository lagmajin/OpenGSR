using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ShotgunBulletController : MonoBehaviour
    {
        [SerializeField] private int damage = 20;
        [SerializeField] private string ownerPlayerId = "";
        [SerializeField] private string weaponName = "Shotgun";

        public ETeam Team { get; set; } = ETeam.NoTeam;

        private float count;

        private void Awake()
        {
            damage = Mathf.Max(0, damage);
            count = 0f;
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0, damage);
        }

        //[SerializeField][Required]public AudioClip hitSound;

        private void FixedUpdate()
        {
            if (!float.IsFinite(count))
            {
                count = 0f;
            }

            if (count <= 180.0f)
            {
                transform.Rotate(0, 0, -1.1f);
            }

            count += 1.1f;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                Destroy(gameObject);
                return;
            }

            if (ProjectileHitUtility.TryGetTargetPlayer(collision, out var player))
            {
                if (ProjectileHitUtility.ApplyPlayerDamage(
                        player,
                        transform.position,
                        damage,
                        eDamageType.Bullet,
                        ownerPlayerId,
                        weaponName,
                        Team,
                        true))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Destroy(gameObject);
        }

        public void Init(Vector2 direction, float initDamage, string ownerId, string weapon, ETeam team)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.right;
            }

            damage = float.IsFinite(initDamage) ? Mathf.Max(0, Mathf.RoundToInt(initDamage)) : 0;
            ownerPlayerId = ownerId ?? string.Empty;
            weaponName = string.IsNullOrWhiteSpace(weapon) ? "Shotgun" : weapon;
            Team = team;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
