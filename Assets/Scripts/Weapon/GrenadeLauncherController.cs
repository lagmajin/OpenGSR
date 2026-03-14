using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// グレネードランチャー (GR) の挙動を制御するクラス。
    /// 放物線を描く弾を発射し、着弾時に爆発（範囲ダメージ）を発生させる。
    /// </summary>
    public class GrenadeLauncherController : AbstractGunController
    {
        [Header("Grenade Settings")]
        [SerializeField] private float launchForce = 15f;
        [SerializeField] private bool usePhysics = true;

        protected override void CreateBullet(OpenGSCore.EBulletType type = OpenGSCore.EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null) return;

            Vector2 shotDir = GetShotDirection();
            float angle = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;

            GameObject grenadeObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, angle));
            
            var bullet = grenadeObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(shotDir, bulletSpeed, damage);
                bullet.EnableGravity(); // 重力を有効化
            }

            // Rigidbody2D があれば初速を与える（物理ベースの場合）
            var rb = grenadeObj.GetComponent<Rigidbody2D>();
            if (rb != null && usePhysics)
            {
                rb.linearVelocity = shotDir * launchForce;
            }
        }
    }
}
