using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum EPlayerType
    {
        Unknown,
        MyPlayer,
        OtherPlayer,
        AI
    }


    public sealed class PlayerStatus
    {
        private const int DefaultMaxGrenade = 3;
        private readonly object lockObject = new();
        private readonly EGrenadeType[] grenadeSlots = new EGrenadeType[DefaultMaxGrenade]
        {
            EGrenadeType.Empty,
            EGrenadeType.Empty,
            EGrenadeType.Empty
        };

        private ETeam Team { get; set; }=ETeam.NoTeam;

     
        public float MaxHp { get; set; } = 500;
        public float Hp { get; set; } = 500;
        public float MaxBooster { get;set; } = 100;
        public float Booster { get; set; } = 100;

        public float BoosterPowerGround { get; set; } = 3.0f;
        public float BoosterPower { get; set; } = 1.0f;
        public int AttackPower { get; set; } = 10;
        public int DefensePower { get; set; } = 5;
        public int GrenadeCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < grenadeSlots.Length; index++)
                {
                    if (grenadeSlots[index] != EGrenadeType.Empty)
                    {
                        count++;
                    }
                }

                return count;
            }
            set => SetGrenadeCount(value);
        }

        public int MaxGrenadeCount { get; set; } = DefaultMaxGrenade;

        public PlayerStatus()
        {




        }

        public PlayerStatus(int maxHP=500, float maxBooster=100.0f, float boosterPower=1.0f)
        {
            MaxHp = maxHP;
            MaxBooster = maxBooster;
            BoosterPower = boosterPower;

        }

        public PlayerStatus(ETeam team = ETeam.NoTeam,EPlayerType type=EPlayerType.Unknown, int maxHP = 550,float maxBooster=100.0f)
        {
            Team = team;


        }

        public void AddHp(float amount)
        {
            if (amount <= 0) return;
            Hp = Math.Min(MaxHp, Hp + amount);
        }

        public void Damage(float amount)
        {
            if (amount <= 0) return;
            Hp = Math.Max(0, Hp - amount);
        }

        public void AddBooster(float amount)
        {
            if (amount <= 0) return;
            Booster = Math.Min(MaxBooster, Booster + amount);
        }

        public void UseBooster(float amount)
        {
            if (amount <= 0) return;
            Booster = Math.Max(0, Booster - amount);
        }

        public void RefillBooster()
        {
            Booster = MaxBooster;
        }

        public void AddAttackPower(int amount)
        {
            AttackPower = Math.Max(0, AttackPower + amount);
        }

        public void AddDefensePower(int amount)
        {
            DefensePower = Math.Max(0, DefensePower + amount);
        }

        public IReadOnlyList<EGrenadeType> GrenadeSlots => grenadeSlots;

        public void RefillGrenade()
        {
            RefillGrenade(EGrenadeType.Normal, MaxGrenadeCount);
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

            return filled;
        }

        public bool UseGrenade()
        {
            return UseGrenade(out _);
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

        public int FillGrenade(EGrenadeType type = EGrenadeType.Normal)
        {
            return RefillGrenade(type, MaxGrenadeCount);
        }

        public void FillNormalGrenade()
        {
            RefillGrenade(EGrenadeType.Normal, MaxGrenadeCount);
        }

        private void SetGrenadeCount(int value)
        {
            value = Math.Max(0, Math.Min(value, MaxGrenadeCount));
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
        }

    }


}
