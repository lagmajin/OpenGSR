using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Backward-compatible wrapper for selection icons.
    /// New code should use WeaponVisualResolver directly.
    /// </summary>
    public static class WeaponSelectionSpriteResolver
    {
        public static Sprite Resolve(string weaponId)
        {
            return WeaponVisualResolver.GetSelectionSprite(weaponId);
        }
    }
}
