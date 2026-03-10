using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenGS
{
    partial class CharaController
    {
        private Dictionary<float, string> slipDamage;


        public override void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            Debug.Log("Damage:" + damage);
            base.AddDamage(source, damage, type);

            if (type == eDamageType.Explosion)
            {

            }

            if (type == eDamageType.Fire)
            {


            }

        }
        public override void AddSlipDamage(float v, string id)
        {

        }


    }
}
