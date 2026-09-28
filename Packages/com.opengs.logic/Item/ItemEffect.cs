using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class PowerUpItemEffect:AbstractItemEffect
    {

        public PowerUpItemEffect()
        {
        }

        public override void ApplyItemEffect(PlayerStatus status)
        {
            if (status == null)
            {
                return;
            }

            status.AddHp(100f);
            status.AddBooster(25f);
            status.AddAttackPower(5);
        }

        public override void UnApplyItemEffect(PlayerStatus status)
        {
            if (status == null)
            {
                return;
            }

            status.Damage(100f);
            status.UseBooster(25f);
            status.AddAttackPower(-5);
        }

    }

    public class DefenceUpItemEffect:AbstractItemEffect
    {


        public override void ApplyItemEffect(PlayerStatus status)
        {
            ApplyHp(status, 50f);
            ApplyBooster(status, 50f);
            ApplyDefensePower(status, 5);
        }

        public override void UnApplyItemEffect(PlayerStatus status)
        {
            ApplyHp(status, -50f);
            ApplyBooster(status, -50f);
            ApplyDefensePower(status, -5);
        }
    }

    public class NormalGranadePackItemEffect : AbstractItemEffect
    {
        public override void ApplyItemEffect(PlayerStatus status)
        {
            status?.RefillGrenade(EGrenadeType.Normal);
        }

        public override void UnApplyItemEffect(PlayerStatus status)
        {
            // 消費型なので解除処理は不要
        }
    }

    public class SpeedUpItemEffect : AbstractItemEffect
    {
        public override void ApplyItemEffect(PlayerStatus status)
        {
            ApplyBooster(status, 20f);
        }

        public override void UnApplyItemEffect(PlayerStatus status)
        {
            ApplyBooster(status, -20f);
        }
    }

    public class StealthItemEffect : AbstractItemEffect
    {
        public override void ApplyItemEffect(PlayerStatus status)
        {
            ApplyDefensePower(status, 1);
        }

        public override void UnApplyItemEffect(PlayerStatus status)
        {
            ApplyDefensePower(status, -1);
        }
    }

    public class HealItemEffect : AbstractItemEffect
    {
        public override void ApplyItemEffect(PlayerStatus status)
        {
            status?.AddHp(150f);
        }

        public override void UnApplyItemEffect(PlayerStatus status)
        {
            ApplyHp(status, -150f);
        }
    }
}
