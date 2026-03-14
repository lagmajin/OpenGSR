using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// アサルトライフル (AR) の挙動を制御するクラス。
    /// 標準的な連射性能と安定した精度を持つ。
    /// </summary>
    public class AssaultRifleController : AbstractGunController
    {
        protected override void CreateBullet(OpenGSCore.EBulletType type = OpenGSCore.EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null) return;

            // 弾の飛ぶ方向を計算（反動とブレを含む）
            Vector2 shotDir = GetShotDirection();
            float angle = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;

            // 弾を生成
            GameObject bulletObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, angle));
            
            // 弾の初期設定（速度、ダメージなど）
            var bullet = bulletObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(shotDir, bulletSpeed, damage);
            }
        }
    }
}
