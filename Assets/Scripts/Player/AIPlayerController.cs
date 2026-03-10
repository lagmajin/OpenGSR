using OpenGSCore;
using System.Collections.Generic;
using UnityEngine;
//using Unity.VisualScripting;



#pragma warning disable 0414

namespace OpenGS
{

    public class AIPlayerController : AbstractPlayer,IDamageable,IPlayer
    {
        [SerializeField]
        private eAIStrength strength_ = eAIStrength.Normal;
        [SerializeField]
        private eAIPlayerType playerType_ = eAIPlayerType.Attcker;

        private eAIBattleMode mode = eAIBattleMode.Patrol;

        private List<GameObject> targetList;
        //[SerializeField]
        //private ScriptMachine machine;
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
            //machine.enabled = true;

            Debug.Log("Test");
        }

        private void Update()
        {
            if (mode == eAIBattleMode.Patrol)
            {
                Patrol();
            }

            if (mode == eAIBattleMode.Wait)
            {
                Wait();
            }

            if (mode == eAIBattleMode.Attack)
            {
                Attack();
            }

            if (mode == eAIBattleMode.Avoid)
            {
                Avoid();
            }


        }

        private void AimTarget(GameObject target)
        {

        }

        private void GetTargets()
        {

        }

        private void GetTargetPos()
        {

        }

        void Attack()
        {

        }

        void Avoid()
        {

        }

        void Patrol()
        {

        }


        void Wait()
        {

        }

        public void Analyze()
        {

        }

        public void SetAIStrength(eAIStrength strength = eAIStrength.Normal)
        {
            Debug.Log("AI TypeChanged");

            strength_ = strength;

        }

        public void SetAIPlayerType(eAIPlayerType type = eAIPlayerType.Attcker)
        {



        }

        public void AddToTargetList(GameObject player)
        {
            Debug.Log("TargetName:" + player.gameObject.name);

            targetList.Add(player);



        }

        public void RemoveToTargetList(GameObject player)
        {
            targetList.Remove(player);
        }

        public override void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            //throw new System.NotImplementedException();
        }

        public override void IncreaseAttack(float sec)
        {
            //throw new System.NotImplementedException();
        }

        public override void IncreaseDefense(float sec)
        {
            //throw new System.NotImplementedException();
        }

        public override void Invisible(float sec)
        {
            //throw new System.NotImplementedException();
        }

        public override void SpeedUp(float sec)
        {
            //throw new System.NotImplementedException();
        }

        public eAIBattleMode AIBattleMode()
        {
            return eAIBattleMode.Attack;
        }

        public void SetAIMode(eAIBattleMode mode)
        {

        }


        void OnTriggerEnter2D(Collider2D other)
        {
        }

        private eWeaponType SelectWeapon()
        {
            eWeaponType type;

            type = eWeaponType.M60;

            return type;
        }

        public override void OnSpawn()
        {
            var type = SelectWeapon();

            //EquipWeapon(type);

        }

        public override void OnReSpawn()
        {


        }
    }




}
