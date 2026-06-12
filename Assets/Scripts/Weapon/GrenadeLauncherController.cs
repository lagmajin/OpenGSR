using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// グレネードランチャー (GR) の挙動を制御するクラス。
    /// 放物線を描く弾を発射し、着弾時に爆発（範囲ダメージ）を発生させる。
    /// </summary>
    public class GrenadeLauncherController : AbstractGunController
    {
        protected override void CreateBullet(OpenGSCore.EBulletType type = OpenGSCore.EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null) return;

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

            var grenadeBullet = grenadeObj.GetComponent<GrenadeLauncherBulletController>();
            if (grenadeBullet != null)
            {
                grenadeBullet.Init(shotDir, bulletSpeed, effectiveDamage, playerId, Name, ownerTeam, owner != null ? owner.transform : null);
                grenadeBullet.EnableGravity();
                return;
            }

            var bullet = grenadeObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(shotDir, bulletSpeed, effectiveDamage, playerId, Name, ownerTeam);
                bullet.EnableGravity();

                var impactBullet = grenadeObj.AddComponent<GrenadeLauncherBulletController>();
                impactBullet.Team = ownerTeam;
                impactBullet.Init(shotDir, bulletSpeed, effectiveDamage, playerId, Name, ownerTeam, owner != null ? owner.transform : null);
                impactBullet.EnableGravity();
                bullet.enabled = false;
            }
        }
    }
}
