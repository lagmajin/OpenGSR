



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
            if (Input.GetMouseButton(0)) // 左クリックで1発撃つ
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

            float spreadAngle = Random.Range(0,0);

            // 弾を生成
            var bullet = Instantiate(bulletPrefab);
            bullet.transform.position = muzzle.transform.position;

            // === マウス方向を計算 ===
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mouseWorldPos - muzzle.transform.position).normalized;

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

            CreateMuzzulleFlash();

            PlayShotSound();

            remains--;




        }


    }
}
