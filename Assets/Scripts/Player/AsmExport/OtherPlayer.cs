
using System;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OtherPlayer : AbstractPlayer
    {
        public float attack = 1.0f;
        public float defence = 1.0f;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (CheckFallDeath())
            {
                return;
            }
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
    }

}
