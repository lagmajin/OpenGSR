using UnityEngine;
using UniRx;

namespace OpenGS
{
    /// <summary>
    /// Player status data class containing HP, Booster, and related reactive properties
    /// </summary>
    public class PlayerStatus
    {
        const float DefaultMaxHp = 512f; // ベースルールに合わせた512固定
        const float DefaultMaxBooster = 100f;
        const int DefaultMaxGrenade = 3;
        const float DefaultMaxArmor = 100f; // デフォルトの最大アーマー値

        readonly ReactiveProperty<float> hp = new(DefaultMaxHp);
        readonly ReactiveProperty<float> maxHp = new(DefaultMaxHp);
        readonly ReactiveProperty<float> armor = new(0f); // 初期状態は0
        readonly ReactiveProperty<float> maxArmor = new(DefaultMaxArmor);
        readonly ReactiveProperty<float> booster = new(DefaultMaxBooster);
        readonly ReactiveProperty<float> maxBooster = new(DefaultMaxBooster);
        readonly ReactiveProperty<float> boosterPower = new(3.0f);
        readonly ReactiveProperty<int> grenadeCount = new(DefaultMaxGrenade);

        // Kill/Death count properties for gameplay tracking
        readonly ReactiveProperty<int> killCount = new(0);
        readonly ReactiveProperty<int> deathCount = new(0);

        public PlayerStatus()
        {
        }

        public float Hp
        {
            get => hp.Value;
            set => hp.Value = Mathf.Clamp(value, 0f, MaxHp);
        }

        public float MaxHp
        {
            get => maxHp.Value;
            set => maxHp.Value = Mathf.Max(1f, value);
        }

        public float Armor
        {
            get => armor.Value;
            set => armor.Value = Mathf.Clamp(value, 0f, MaxArmor);
        }

        public float MaxArmor
        {
            get => maxArmor.Value;
            set => maxArmor.Value = Mathf.Max(0f, value);
        }

        public float Booster
        {
            get => booster.Value;
            set => booster.Value = Mathf.Clamp(value, 0f, MaxBooster);
        }

        public float MaxBooster
        {
            get => maxBooster.Value;
            set => maxBooster.Value = Mathf.Max(1f, value);
        }

        public float BoosterPower
        {
            get => boosterPower.Value;
            set => boosterPower.Value = Mathf.Max(0.1f, value);
        }

        public int KillCount
        {
            get => killCount.Value;
            set => killCount.Value = Mathf.Max(0, value);
        }

        public int DeathCount
        {
            get => deathCount.Value;
            set => deathCount.Value = Mathf.Max(0, value);
        }

        public int GrenadeCount
        {
            get => grenadeCount.Value;
            set => grenadeCount.Value = Mathf.Clamp(value, 0, DefaultMaxGrenade);
        }

        public IReadOnlyReactiveProperty<float> HpStream => hp;
        public IReadOnlyReactiveProperty<float> ArmorStream => armor;
        public IReadOnlyReactiveProperty<float> BoosterStream => booster;
        public IReadOnlyReactiveProperty<float> MaxBoosterStream => maxBooster;
        public IReadOnlyReactiveProperty<int> KillCountStream => killCount;
        public IReadOnlyReactiveProperty<int> DeathCountStream => deathCount;

        public void AddHp(float amount)
        {
            Hp = Hp + amount;
        }

        public void ReduceHp(float amount)
        {
            Hp = Hp - amount;
        }

        public void AddArmor(float amount)
        {
            Armor = Armor + amount;
        }

        public void ReduceArmor(float amount)
        {
            Armor = Armor - amount;
        }

        public void FullRecovery()
        {
            Hp = MaxHp;
            Armor = MaxArmor;
            Booster = MaxBooster;
            RefillGrenade();
        }

        public void RefillBooster(float amount)
        {
            Booster = Booster + amount;
        }

        public void ConsumeBooster(float amount)
        {
            Booster = Booster - amount;
        }

        public void AddKill()
        {
            KillCount++;
        }

        public void ResetKills()
        {
            KillCount = 0;
        }

        public void ConsumeGrenade()
        {
            GrenadeCount--;
        }

        public void RefillGrenade()
        {
            GrenadeCount = DefaultMaxGrenade;
        }
    }
}
