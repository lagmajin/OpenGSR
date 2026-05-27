using System.Collections.Generic;
using UnityEngine;
using UniRx;
using OpenGSCore;

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
        readonly EGrenadeType[] grenadeSlots = new EGrenadeType[DefaultMaxGrenade]
        {
            EGrenadeType.Empty,
            EGrenadeType.Empty,
            EGrenadeType.Empty
        };

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
            set => SetGrenadeCount(value);
        }

        public IReadOnlyList<EGrenadeType> GrenadeSlots => grenadeSlots;

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

        public void AddDeath()
        {
            DeathCount++;
        }

        public void ResetKills()
        {
            KillCount = 0;
        }

        public void ResetDeaths()
        {
            DeathCount = 0;
        }

        public void ResetCombatStats()
        {
            ResetKills();
            ResetDeaths();
        }

        public void ConsumeGrenade()
        {
            UseGrenade();
        }

        public bool ConsumeGrenade(int amount)
        {
            return ConsumeGrenade(EGrenadeType.Empty, amount);
        }

        public bool UseGrenade()
        {
            return UseGrenade(out _);
        }

        public void RefillGrenade()
        {
            RefillGrenade(EGrenadeType.Normal, DefaultMaxGrenade);
        }

        public int RefillGrenade(EGrenadeType type, int amount = DefaultMaxGrenade)
        {
            if (amount <= 0)
            {
                return 0;
            }

            if (type == EGrenadeType.Empty)
            {
                type = EGrenadeType.Normal;
            }

            var filled = 0;
            for (var index = 0; index < grenadeSlots.Length && filled < amount; index++)
            {
                if (grenadeSlots[index] != EGrenadeType.Empty)
                {
                    continue;
                }

                grenadeSlots[index] = type;
                filled++;
            }

            SyncGrenadeCount();
            return filled;
        }

        public bool UseGrenade(out EGrenadeType usedType)
        {
            for (var index = 0; index < grenadeSlots.Length; index++)
            {
                if (grenadeSlots[index] == EGrenadeType.Empty)
                {
                    continue;
                }

                usedType = grenadeSlots[index];
                grenadeSlots[index] = EGrenadeType.Empty;
                SyncGrenadeCount();
                return true;
            }

            usedType = EGrenadeType.Empty;
            return false;
        }

        public bool UseGrenade(EGrenadeType type)
        {
            return UseGrenade(type, out _);
        }

        public bool UseGrenade(EGrenadeType type, out int slotIndex)
        {
            slotIndex = -1;

            if (type == EGrenadeType.Empty)
            {
                return UseGrenade(out _);
            }

            for (var index = 0; index < grenadeSlots.Length; index++)
            {
                if (grenadeSlots[index] != type)
                {
                    continue;
                }

                grenadeSlots[index] = EGrenadeType.Empty;
                slotIndex = index;
                SyncGrenadeCount();
                return true;
            }

            return false;
        }

        public EGrenadeType GetGrenadeSlot(int index)
        {
            if (index < 0 || index >= grenadeSlots.Length)
            {
                return EGrenadeType.Empty;
            }

            return grenadeSlots[index];
        }

        public bool ConsumeGrenade(EGrenadeType type, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            var consumed = 0;
            for (var index = 0; index < grenadeSlots.Length && consumed < amount; index++)
            {
                if (grenadeSlots[index] == EGrenadeType.Empty)
                {
                    continue;
                }

                if (type != EGrenadeType.Empty && grenadeSlots[index] != type)
                {
                    continue;
                }

                grenadeSlots[index] = EGrenadeType.Empty;
                consumed++;
            }

            if (consumed > 0)
            {
                SyncGrenadeCount();
            }

            return consumed > 0;
        }

        private void SetGrenadeCount(int value)
        {
            value = Mathf.Clamp(value, 0, DefaultMaxGrenade);
            var current = GrenadeCount;

            if (value == current)
            {
                return;
            }

            if (value > current)
            {
                RefillGrenade(EGrenadeType.Normal, value - current);
                return;
            }

            var toRemove = current - value;
            for (var index = grenadeSlots.Length - 1; index >= 0 && toRemove > 0; index--)
            {
                if (grenadeSlots[index] == EGrenadeType.Empty)
                {
                    continue;
                }

                grenadeSlots[index] = EGrenadeType.Empty;
                toRemove--;
            }

            SyncGrenadeCount();
        }

        private void SyncGrenadeCount()
        {
            var count = 0;
            for (var index = 0; index < grenadeSlots.Length; index++)
            {
                if (grenadeSlots[index] != EGrenadeType.Empty)
                {
                    count++;
                }
            }

            grenadeCount.Value = Mathf.Clamp(count, 0, DefaultMaxGrenade);
        }

        public void FullCombatRecovery()
        {
            ResetCombatStats();
            RefillGrenade();
        }
    }
}
