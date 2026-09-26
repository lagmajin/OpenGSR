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
            base.AddDamage(source, damage, type);
            onDamage = true;
            StartBlink();
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

            var now = Time.time;
            if (!float.IsFinite(now) || now < 0f)
            {
                return;
            }

            slipDamage[now] = id ?? string.Empty;
            onDamage = true;
            StartBlink();
        }


    }
}
