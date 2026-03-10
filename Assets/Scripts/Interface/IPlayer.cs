using System;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public interface IPlayer
    {
        void OnDead();
        void OnBurst();

        void OnSpawn();
        void OnReSpawn();

        void ReserveReSpawn(float delay);

        bool IsGround();
        bool IsDead();

        bool IsRolling();

        Guid UniqueID();

        bool HasTeam();

        ETeam Team();

        void SetTeam(ETeam team);

        bool HasEnemyFlag();

        void EnemyFlagCaptured();
        void EnemyFlagReturnedToBase();

        bool CanEquip();
        bool CanWarp();

        void EquipWeapon();
        void EquipWeapon(GameObject weaponPrefab);

        void DropCurrentWeapon();
        void SwapWeapon();

        void CreatePlayerLink(EPlayerType type, string id);

        float GetHP();
        float GetMaxHP();
        float GetArmor();
        float GetMaxArmor();
        float GetBooster();
        float GetMaxBooster();
    }
}
