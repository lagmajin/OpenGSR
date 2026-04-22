//using System;

//using OepnGSCore;
using OpenGSCore;
using UnityEngine;
//using Random = System.Random;


namespace OpenGS
{
    public class ShotgunController:AbstractGunController
    {
        [SerializeField, Range(1f, 20f)]
        public int bulletCount = 1;


        private void Start()
        {
            bulletGravity = true;
            remains = magazine;
            isShottable = true;
        }
        
   


        protected override  void CreateBullet(EBulletType type=EBulletType.Normal)
        {
            int pelletCount = 6; // 一度に撃つ弾の数
            float spreadRange = 15f; // 全体の拡散角（左右合計）

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 baseDir = (mouseWorldPos - muzzle.transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            for (int i = 0; i < pelletCount; i++)
            {
                float spreadAngle = Random.Range(-spreadRange / 2f, spreadRange / 2f);
                float finalAngle = baseAngle + spreadAngle;
                Quaternion rotation = Quaternion.Euler(0, 0, finalAngle);
                Vector2 finalDir = rotation * Vector2.right;

                var bullet = Instantiate(bulletPrefab);
                bullet.transform.position = muzzle.transform.position;
                bullet.transform.rotation = rotation;

                var script = bullet.GetComponent<AbstractBulletAgent>();
                script.Launch(finalDir,30);
            }

            CreateMuzzulleFlash();
            PlayShotSound();
            remains--;
        }

  
    }
}
