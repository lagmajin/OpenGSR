
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

        }

        public override void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            //throw new NotImplementedException();
        }

        public override void IncreaseAttack(float sec)
        {
            //throw new NotImplementedException();
        }

        public override void IncreaseDefense(float sec)
        {
            //throw new NotImplementedException();
        }

        public override void Invisible(float sec)
        {
            //throw new NotImplementedException();
        }

        public override void SpeedUp(float sec)
        {
            //throw new NotImplementedException();
        }
    }

}