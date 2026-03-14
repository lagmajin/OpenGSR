using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// マシンガン (MG) の挙動を制御するクラス。
    /// 圧倒的な連射速度を持つが、撃ち続けると精度が低下する。
    /// </summary>
    public class MachineGunController : AbstractGunController
    {
        [Header("Machine Gun Settings")]
        [SerializeField] private float heatAccuracyPenalty = 5.0f; // 熱による精度の低下倍率

        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            // マシンガン独自の更新処理があればここに追加
        }

        protected override void CreateBullet(OpenGSCore.EBulletType type = OpenGSCore.EBulletType.Normal)
        {
            if (bulletPrefab == null || muzzle == null) return;

            // 基本のブレに加えて、現在の熱量に応じたペナルティを加える
            float currentSpread = baseSpread + (heat * heatAccuracyPenalty);
            
            // エイム方向を取得し、ブレを加える
            Vector2 baseDir = inputService.GetAimDirection(muzzle.position);
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float finalAngle = baseAngle + Random.Range(-currentSpread, currentSpread);
            Vector2 shotDir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));

            GameObject bulletObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, finalAngle));
            
            var bullet = bulletObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(shotDir, bulletSpeed, damage);
            }
        }
    }
}
