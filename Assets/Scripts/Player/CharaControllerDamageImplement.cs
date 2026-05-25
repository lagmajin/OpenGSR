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
            onDamage = true;
            StartBlink();

            if (type == eDamageType.Explosion)
            {
                Debug.Log("[CharaController] Explosion damage received.");
            }

            if (type == eDamageType.Fire)
            {
                Debug.Log("[CharaController] Fire damage received.");
            }

        }
        public override void AddSlipDamage(float v, string id)
        {
            if (v <= 0f)
            {
                return;
            }

            if (slipDamage == null)
            {
                slipDamage = new Dictionary<float, string>();
            }

            slipDamage[Time.time] = id ?? string.Empty;
            onDamage = true;
            StartBlink();
        }


    }
}
