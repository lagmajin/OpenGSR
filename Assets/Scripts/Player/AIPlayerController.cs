using OpenGSCore;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0414

namespace OpenGS
{
    public class AIPlayerController : AbstractPlayer, IDamageable, IPlayer
    {
        [SerializeField]
        private eAIStrength strength_ = eAIStrength.Normal;
        [SerializeField]
        private eAIPlayerType playerType_ = eAIPlayerType.Attcker;

        private eAIBattleMode mode = eAIBattleMode.Patrol;
        private readonly List<GameObject> targetList = new();
        [SerializeField]
        private bool autoScriptMachineEnable = true;
        private float lastAttackTime = -10f;
        [SerializeField]
        private float attackCooldown = 0.5f;
        [SerializeField]
        private float targetRefreshInterval = 0.25f;
        private float nextTargetRefreshTime;

        private void OnValidate()
        {
            if (!float.IsFinite(attackCooldown)) attackCooldown = 0.5f;
            if (!float.IsFinite(targetRefreshInterval)) targetRefreshInterval = 0.25f;
            attackCooldown = Mathf.Max(0f, attackCooldown);
            targetRefreshInterval = Mathf.Max(0.05f, targetRefreshInterval);
        }

        private void Start()
        {
            SetPlayerType(EPlayerType.AIPlayer);
            if (autoScriptMachineEnable)
            {
                EnableScriptMachine();
            }

            Analyze();
        }

        private void EnableScriptMachine()
        {
        }

        private void Update()
        {
            if (CheckFallDeath())
            {
                return;
            }

            switch (mode)
            {
                case eAIBattleMode.Patrol:
                    Patrol();
                    break;
                case eAIBattleMode.Wait:
                    Wait();
                    break;
                case eAIBattleMode.Attack:
                    Attack();
                    break;
                case eAIBattleMode.Avoid:
                    Avoid();
                    break;
            }

            if (mode == eAIBattleMode.Attack && targetList.Count == 0)
            {
                mode = eAIBattleMode.Patrol;
            }
        }

        private void AimTarget(GameObject target)
        {
            if (target != null)
            {
                var gun = weaponSlots != null ? weaponSlots.GetCurrentGun() : null;
                if (gun != null)
                {
                    gun.SetGunDirection(target.transform.position.x >= transform.position.x);
                }

                transform.localScale = new Vector3(
                    target.transform.position.x >= transform.position.x ? 1f : -1f,
                    transform.localScale.y,
                    transform.localScale.z);
            }
        }

        private void GetTargets()
        {
            var now = Time.time;
            if (!float.IsFinite(now) || now < 0f || now < nextTargetRefreshTime)
            {
                return;
            }

            nextTargetRefreshTime = now + targetRefreshInterval;
            targetList.Clear();
            foreach (var player in FindObjectsByType<AbstractPlayer>(FindObjectsSortMode.None))
            {
                if (player != null && player.gameObject != gameObject)
                {
                    targetList.Add(player.gameObject);
                }
            }
        }

        private void GetTargetPos()
        {
            if (targetList.Count == 0)
            {
                return;
            }

            AimTarget(targetList[0]);
        }

        void Attack()
        {
            GetTargets();
            GetTargetPos();

            var now = Time.time;
            if (!float.IsFinite(now) || now < 0f)
            {
                return;
            }

            if (targetList.Count > 0)
            {
                if (now >= lastAttackTime + attackCooldown)
                {
                    var gun = weaponSlots != null ? weaponSlots.GetCurrentGun() : null;
                    if (gun != null && gun.CanShot())
                    {
                        gun.Shot();
                        lastAttackTime = now;
                    }
                }
            }
        }

        void Avoid()
        {
            mode = eAIBattleMode.Wait;
        }

        void Patrol()
        {
            Analyze();
            if (targetList.Count > 0)
            {
                mode = eAIBattleMode.Attack;
            }
        }

        void Wait()
        {
            if (targetList.Count > 0)
            {
                mode = eAIBattleMode.Attack;
            }
        }

        public void Analyze()
        {
            GetTargets();
        }

        public void SetAIStrength(eAIStrength strength = eAIStrength.Normal)
        {
            strength_ = strength;
        }

        public void SetAIPlayerType(eAIPlayerType type = eAIPlayerType.Attcker)
        {
            playerType_ = type;
        }

        public void AddToTargetList(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            if (!targetList.Contains(player))
            {
                targetList.Add(player);
            }
        }

        public void RemoveToTargetList(GameObject player)
        {
            targetList.Remove(player);
        }

        public override void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            base.AddDamage(source, damage, type);
        }

        public override void IncreaseAttack(float sec)
        {
            base.IncreaseAttack(sec);
        }

        public override void IncreaseDefense(float sec)
        {
            base.IncreaseDefense(sec);
        }

        public override void Invisible(float sec)
        {
            base.Invisible(sec);
        }

        public override void SpeedUp(float sec)
        {
            base.SpeedUp(sec);
        }

        public eAIBattleMode AIBattleMode()
        {
            return mode;
        }

        public void SetAIMode(eAIBattleMode mode)
        {
            this.mode = mode;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            var player = other.GetComponentInParent<AbstractPlayer>();
            if (player != null && player.gameObject != gameObject)
            {
                AddToTargetList(player.gameObject);
                mode = eAIBattleMode.Attack;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            var player = other.GetComponentInParent<AbstractPlayer>();
            if (player != null)
            {
                RemoveToTargetList(player.gameObject);
            }
        }

        private eWeaponType SelectWeapon()
        {
            return eWeaponType.M60;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            SetPlayerType(EPlayerType.AIPlayer);
            mode = eAIBattleMode.Patrol;
            lastAttackTime = -10f;
            nextTargetRefreshTime = 0f;
            SelectWeapon();
        }

        public override void OnReSpawn()
        {
            base.OnReSpawn();
            mode = eAIBattleMode.Patrol;
            nextTargetRefreshTime = 0f;
            Analyze();
        }
    }
}
