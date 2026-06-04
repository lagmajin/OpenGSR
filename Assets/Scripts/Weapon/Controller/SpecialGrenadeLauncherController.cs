
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    interface ISpecialGrenadeRifleController
    {

    }

    //使い捨てグレネードランチャー
    [DisallowMultipleComponent]
    public class SpecialGrenadeRifleController : AbstractGunController
    {
        private void Start()
        {
            fireMode = EFireMode.Semi;
            remains = magazine;
        }

        protected override void OnUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shot();
            }

            if (autoDelete && remains <= 0)
            {
                RemoveThis();
                return;
            }
        }

        private void Update()
        {
            base.OnUpdate();
            OnUpdate();
        }

        protected override void CreateBullet(EBulletType type = EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null)
            {
                return;
            }

            Vector2 shotDir = GetShotDirection();
            float angle = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;
            GameObject grenadeObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, angle));

            var owner = GetOwnerPlayer();
            var ownerTeam = owner != null ? owner.Team() : ETeam.NoTeam;
            var playerId = GetPlayerID(owner);
            var effectiveDamage = GetEffectiveDamage(owner);

            var projectile = grenadeObj.GetComponent<GrenadeProjectileController>();
            if (projectile != null)
            {
                projectile.SetDamage(effectiveDamage);
                projectile.SetGrenadeType(EGrenadeType.Normal);
                projectile.Launch(shotDir, bulletSpeed, playerId, Name, ownerTeam, owner != null ? owner.transform : null);
                return;
            }

            var bullet = grenadeObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(shotDir, bulletSpeed, effectiveDamage, playerId, Name, ownerTeam);
                bullet.EnableGravity();
                return;
            }

            var grenadeBullet = grenadeObj.GetComponent<GrenadeLauncherBulletController>();
            if (grenadeBullet != null)
            {
                grenadeBullet.Init(shotDir, bulletSpeed, effectiveDamage, playerId, Name, ownerTeam);
                grenadeBullet.EnableGravity();
            }
        }
    }
}
