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

        /// <summary>
        /// Adopts the health the server says this player has.
        /// <para>
        /// The server owns health, so the value it broadcasts is the truth and the
        /// local one is only a prediction made when the player expected to be hit.
        /// Replacing rather than applying it means a hit the client already
        /// guessed at is not counted twice.
        /// </para>
        /// <para>
        /// Declared here because AbstractPlayer and PlayerAgent are separate
        /// hierarchies and the message has to reach whichever one is in play.
        /// </para>
        /// </summary>
        /// <returns>True when the local value actually moved.</returns>
        bool ApplyServerHealth(int remainingHealth, int maxHealth);
    }
}
