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

        //[SerializeField][Required]public AudioClip hitSound;

        private void FixedUpdate()
        {
            if (count <= 180.0f)
            {
                transform.Rotate(0, 0, -1.1f);
            }

            count += 1.1f;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var player = collision.GetComponentInParent<AbstractPlayer>();
            if (player != null)
            {
                var playerId = player.UniqueID().ToString();
                var playerTeam = player.Team();

                if (!string.IsNullOrWhiteSpace(ownerPlayerId) && playerId == ownerPlayerId)
                {
                    Destroy(gameObject);
                    return;
                }

                if (Team != ETeam.NoTeam && playerTeam != ETeam.NoTeam && playerTeam == Team)
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

        public void Init(Vector2 direction, float initDamage, string ownerId, string weapon, ETeam team)
        {
            damage = Mathf.RoundToInt(initDamage);
            ownerPlayerId = ownerId ?? string.Empty;
            weaponName = string.IsNullOrWhiteSpace(weapon) ? "Shotgun" : weapon;
            Team = team;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
