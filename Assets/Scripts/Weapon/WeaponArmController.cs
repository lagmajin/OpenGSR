using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Controls the weapon arm animations and positioning.
    /// Handles sit and stand up animations for the weapon arm.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponArmController : MonoBehaviour
    {
        /// <summary>
        /// Called when the player sits down - play sit animation for weapon arm
        /// </summary>
        public void Sit()
        {
            // Weapon arm sit animation logic here
        }

        /// <summary>
        /// Called when the player stands up - play stand up animation for weapon arm
        /// </summary>
        public void StandUp()
        {
            // Weapon arm stand up animation logic here
        }
    }
}
