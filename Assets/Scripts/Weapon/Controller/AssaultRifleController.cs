using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class AssaultRifleController : AbstractGunController
    {
        private void Start()
        {
            remains = magazine;
            fireMode = EFireMode.Auto; // アサルトライフルも基本オート
        }

        private void Update()
        {
            base.OnUpdate();
        }

        protected override void CreateBullet(EBulletType type = EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null) return;

            var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            var dir = GetShotDirection();

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

            // The bullet prefab every gun points at carries BulletController, which
            // is the component that actually moves and hits. Asking for
            // AbstractBulletAgent here always came back null, so every rifle this
            // controller was on spawned a bullet and destroyed it in the same
            // frame: five weapons that fired nothing at all when the server was not
            // there to own the damage.
            var owner = GetOwnerPlayer();
            var controller = bullet.GetComponent<BulletController>();
            if (controller != null)
            {
                controller.Init(
                    dir,
                    bulletSpeed,
                    GetEffectiveDamage(owner),
                    GetPlayerID(owner),
                    Name,
                    owner != null ? owner.Team() : ETeam.NoTeam,
                    ResolveBulletDamageType());
            }
            else
            {
                Destroy(bullet);
            }
        }
    }
}
