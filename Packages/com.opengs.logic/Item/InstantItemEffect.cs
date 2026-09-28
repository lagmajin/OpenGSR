using OpenGSCore;

namespace OpenGSCore.Item
{
    public abstract class InstantItemEffect : AbstractItemEffect
    {
        public string EffectName { get; protected set; } = string.Empty;

        public sealed override void ApplyItemEffect(PlayerStatus status)
        {
            Apply(status);
        }

        public sealed override void UnApplyItemEffect(PlayerStatus status)
        {
            UnApply(status);
        }

        protected abstract void Apply(PlayerStatus status);
        protected abstract void UnApply(PlayerStatus status);
    }

    public abstract class NormalGranadeInstantItemEffect : InstantItemEffect
    {
        protected override void Apply(PlayerStatus status)
        {
            status?.RefillGrenade(EGrenadeType.Normal);
        }

        protected override void UnApply(PlayerStatus status)
        {
        }
    }

    public abstract class PowerUpGranadeInstantItemEffect : InstantItemEffect
    {
        protected override void Apply(PlayerStatus status)
        {
            if (status == null) return;
            status.AddAttackPower(3);
            status.AddDefensePower(3);
        }

        protected override void UnApply(PlayerStatus status)
        {
            if (status == null) return;
            status.AddAttackPower(-3);
            status.AddDefensePower(-3);
        }
    }
}
