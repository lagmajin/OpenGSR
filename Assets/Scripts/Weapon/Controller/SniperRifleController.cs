



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

            float spreadAngle = Random.Range(0,0);

            // 弾を生成
            var bullet = Instantiate(bulletPrefab);
            bullet.transform.position = muzzle.transform.position;

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

            // スプレッド角を回転に加える
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += spreadAngle;

            Quaternion spreadRotation = Quaternion.Euler(0, 0, angle);
            bullet.transform.rotation = spreadRotation;

            // 回転に基づく方向を再計算（LaunchにはVector2渡す）
            Vector2 finalDir = spreadRotation * Vector2.right;

            var script = bullet.GetComponent<AbstractBulletAgent>();
            var owner = GetOwnerPlayer();
            if (script != null)
            {
                script.SetOwnerInfo(GetPlayerID(owner), Name, owner != null ? owner.Team() : ETeam.NoTeam);
            }
            script?.Launch(finalDir,200);
            if (script == null)
            {
                Destroy(bullet);
            }

            CreateMuzzulleFlash();

            PlayShotSound();

            remains--;




        }


    }
}
