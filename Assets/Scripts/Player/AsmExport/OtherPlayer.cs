
using System;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OtherPlayer : AbstractPlayer
    {
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

    }

}
