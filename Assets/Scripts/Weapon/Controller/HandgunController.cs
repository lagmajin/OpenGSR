using UnityEngine;
using KanKikuchi.AudioManager;
using OpenGSCore;

namespace OpenGS
{
    class HandgunController : AbstractGunController
    {
        private void Start()
        {
            remains = magazine;
            fireMode = EFireMode.Semi; // ハンドガンはセミオート
        }

        private void Update()
        {
            base.OnUpdate();
        }

        protected override void CreateBullet(OpenGSCore.EBulletType type = OpenGSCore.EBulletType.Normal)
        {
            if (bulletPrefab == null) return;

            var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            var dir = GetShotDirection();
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

            var bulletAgent = bullet.GetComponent<AbstractBulletAgent>();
            if (bulletAgent != null)
            {
                bulletAgent.Launch(dir, bulletSpeed);
            }
        }
    }
}
