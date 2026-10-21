



using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SniperRifleController : AbstractGunController
    {

        private void Start()
        {
            bulletGravity = true;
            remains = magazine;
            isShottable = true;
        }

        protected override void OnUpdate()
        {
            if (inputService != null && inputService.IsFirePressed())
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
                Debug.LogWarning("[SniperRifleController] Bullet prefab or muzzle is not assigned.");
                return;
            }

            // 他の武器と同じ入力サービスの照準座標を使う。
            Vector3 mouseWorldPos = inputService != null
                ? inputService.GetAimWorldPosition()
                : muzzle.transform.position + transform.right;
            var direction = mouseWorldPos - muzzle.transform.position;
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) ||
                !float.IsFinite(direction.z) || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = muzzle.transform.right;
            }
            Vector2 dir = ((Vector2)direction).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0, 0, angle));

            // The prefab carries BulletController, which is the component that
            // moves the round and reports the hit. Asking for AbstractBulletAgent
            // here came back null, so a sniper rifle spawned a bullet it never
            // launched: no velocity, no damage, a round that sat at the muzzle
            // until its lifetime ran out. It also ignored the authored bullet
            // speed entirely and launched at a hard coded 200.
            var owner = GetOwnerPlayer();
            var controller = bullet.GetComponent<BulletController>();
            if (controller != null)
            {
                controller.Init(
                    dir,
                    bulletSpeed,
                    GetEffectiveDamage(owner),
                    GetPlayerID(owner),
                    Name,
                    owner != null ? owner.Team() : ETeam.NoTeam,
                    ResolveBulletDamageType());
            }
            else
            {
                Destroy(bullet);
            }

            CreateMuzzulleFlash();

            PlayShotSound();
        }


    }
}
