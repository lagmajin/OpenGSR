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

        private new void Start()
        {
            if (autoScriptMachineEnable)
            {
                EnableScriptMachine();
            }
        }

        private void EnableScriptMachine()
        {
            Debug.Log("[AIPlayerController] Script machine enabled");
        }

        private void Update()
        {
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
        }

        private void AimTarget(GameObject target)
        {
            if (target != null)
            {
                transform.localScale = new Vector3(
                    target.transform.position.x >= transform.position.x ? 1f : -1f,
                    transform.localScale.y,
                    transform.localScale.z);
            }
        }

        private void GetTargets()
        {
            targetList.Clear();
            foreach (var player in FindObjectsOfType<AbstractPlayer>())
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
        }

        void Avoid()
        {
            Debug.Log("[AIPlayerController] Avoid");
        }

        void Patrol()
        {
            Debug.Log("[AIPlayerController] Patrol");
        }

        void Wait()
        {
            Debug.Log("[AIPlayerController] Wait");
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
            Debug.Log($"[AIPlayerController] Damage {damage}");
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
        }

        private eWeaponType SelectWeapon()
        {
            return eWeaponType.M60;
        }

        public override void OnSpawn()
        {
            SelectWeapon();
        }

        public override void OnReSpawn()
        {
        }
    }
}
