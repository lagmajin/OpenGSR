
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    interface ISpecialGrenadeRifleController
    {

    }

    //使い捨てグレネードランチャー
    [DisallowMultipleComponent]
    public class SpecialGrenadeRifleController : AbstractGunController
    {


        private void Start()
        {
            remains = magazine;

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
            //throw new NotImplementedException();

            //PlaySound()
        }



    }
}
