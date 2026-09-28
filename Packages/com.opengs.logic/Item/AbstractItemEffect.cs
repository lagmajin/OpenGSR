using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public abstract class AbstractItemEffect
    {
        protected AbstractItemEffect()
        {
        }

        protected static bool IsValid(PlayerStatus status)
        {
            return status != null;
        }

        protected static void ApplyHp(PlayerStatus status, float amount)
        {
            if (!IsValid(status) || amount == 0f)
            {
                return;
            }

            if (amount > 0f)
            {
                status.AddHp(amount);
                return;
            }

            status.Damage(-amount);
        }

        protected static void ApplyBooster(PlayerStatus status, float amount)
        {
            if (!IsValid(status) || amount == 0f)
            {
                return;
            }

            if (amount > 0f)
            {
                status.AddBooster(amount);
                return;
            }

            status.UseBooster(-amount);
        }

        protected static void ApplyAttackPower(PlayerStatus status, int amount)
        {
            if (!IsValid(status) || amount == 0)
            {
                return;
            }

            status.AddAttackPower(amount);
        }

        protected static void ApplyDefensePower(PlayerStatus status, int amount)
        {
            if (!IsValid(status) || amount == 0)
            {
                return;
            }

            status.AddDefensePower(amount);
        }

        public abstract void ApplyItemEffect(PlayerStatus status);
        public abstract void UnApplyItemEffect(PlayerStatus status);
    }
}
