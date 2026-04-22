
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SMGController : AbstractGunController
    {
        private void Start()
        {
            bulletGravity = true;
            remains = magazine;
            fireMode = EFireMode.Auto; // SMGはフルオート
        }

        private void Update()
        {
            base.OnUpdate();
        }

        protected override void CreateBullet(EBulletType type = EBulletType.Normal)
        {
            if (bulletPrefab == null) return;

            var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            var dir = GetShotDirection();
            
            // 弾丸の向きをセット
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
