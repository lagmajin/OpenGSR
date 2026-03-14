using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ショットガン (SG) の挙動を制御するクラス。
    /// 一度の射撃で複数の弾を拡散させて発射する。
    /// </summary>
    public class ShotgunController : AbstractGunController
    {
        [Header("Shotgun Settings")]
        [SerializeField] private int pelletCount = 8;
        [SerializeField] private float spreadWidth = 15f;

        protected override void CreateBullet(OpenGSCore.EBulletType type = OpenGSCore.EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null) return;

            // 基準となるエイム方向
            Vector2 baseDir = GetShotDirection();
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            for (int i = 0; i < pelletCount; i++)
            {
                // 各ペレットごとの追加拡散
                float angleOffset = Random.Range(-spreadWidth, spreadWidth);
                float finalAngle = baseAngle + angleOffset;
                Vector2 shotDir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));

                GameObject pellet = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, finalAngle));
                
                var bullet = pellet.GetComponent<BulletController>();
                if (bullet != null)
                {
                    // 各弾の速度とダメージを微調整してバラけさせる
                    bullet.Init(shotDir, bulletSpeed * Random.Range(0.95f, 1.05f), damage);
                }
            }
        }
    }
}
