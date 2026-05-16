using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class AssaultRifleController : AbstractGunController
    {
        private void Start()
        {
            remains = magazine;
            fireMode = EFireMode.Auto; // アサルトライフルも基本オート
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
