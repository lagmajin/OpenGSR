
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
            remains = magazine;

        }

        protected override void OnUpdate()
        {
            if (Input.GetMouseButton(0)) // 左クリックで1発撃つ
            {
                Shot();
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

            var shotDir = GetShotDirection();
            var angle = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;
            var grenadeObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, angle));

            var owner = GetOwnerPlayer();
            var ownerTeam = owner != null ? owner.Team() : ETeam.NoTeam;
            var playerId = GetPlayerID(owner);
            var effectiveDamage = GetEffectiveDamage(owner);

            var bullet = grenadeObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(shotDir, bulletSpeed, effectiveDamage, playerId, Name, ownerTeam);
                bullet.EnableGravity();
            }

            var grenadeBullet = grenadeObj.GetComponent<GrenadeLauncherBulletController>();
            if (grenadeBullet != null)
            {
                grenadeBullet.Init(shotDir, bulletSpeed, effectiveDamage, playerId, Name, ownerTeam);
            }

            var rb = grenadeObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = shotDir * bulletSpeed;
            }
        }



    }
}
