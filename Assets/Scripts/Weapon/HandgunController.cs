using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ハンドガン (HG) の挙動を制御するクラス。
    /// 基本的にセミオートで動作し、移動中の射撃精度が高い。
    /// </summary>
    public class HandgunController : AbstractGunController
    {
        private void Start()
        {
            // ハンドガンは常にセミオート（MasterDataの設定に関わらず）
            fireMode = EFireMode.Semi;
        }

        protected override void CreateBullet(OpenGSCore.EBulletType type = OpenGSCore.EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null) return;

            Vector2 shotDir = GetShotDirection();
            float angle = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;

            GameObject bulletObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, angle));
            
            var bullet = bulletObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(shotDir, bulletSpeed, damage);
            }
        }
    }
}
