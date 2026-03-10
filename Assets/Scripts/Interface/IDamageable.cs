using UnityEngine;

namespace OpenGS
{
    public interface IDamageable
    {
        void AddDamage(Vector2 source, float damage, eDamageType type);
        void AddDamageAndForce(float damage, Vector3 vec, float force = 1.0f);
        void AddDamageAndForce2(float damage, Vector2 point);

        void Heal(float heal = 0);

        void TakeLavaDamage();
        void AddSlipDamage(float v, string id);
    }
}
