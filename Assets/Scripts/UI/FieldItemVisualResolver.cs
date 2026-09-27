using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Display names and wire name handling for field items.
    /// <para>
    /// This used to hold two parallel implementations, one per enum: a method
    /// taking OpenGSCore.EFieldItemType and another taking the client local
    /// eFieldItemType, each with its own switch. The client enum is now an
    /// alias of the shared one, so one switch is enough and the two can no
    /// longer disagree about what an item is called.
    /// </para>
    /// <para>
    /// Parsing and the wire name go through FieldItemTypeNames in the shared
    /// package, which is the same helper the server uses, so a name the server
    /// writes is a name the client reads.
    /// </para>
    /// </summary>
    public static class WorldItemVisualResolver
    {
        public static string GetDisplayName(OpenGSCore.EFieldItemType type)
        {
            return type switch
            {
                OpenGSCore.EFieldItemType.GranadeLauncher => "Grenade Launcher",
                OpenGSCore.EFieldItemType.FlameThrower => "Flame Thrower",
                OpenGSCore.EFieldItemType.PowerUpItem => "Power Up",
                OpenGSCore.EFieldItemType.DefenceUpItem => "Defence Up",
                OpenGSCore.EFieldItemType.SpeedUpItem => "Speed Up",
                OpenGSCore.EFieldItemType.StealthItem => "Stealth",
                OpenGSCore.EFieldItemType.GrenadePack => "Grenade Pack",
                OpenGSCore.EFieldItemType.HealItem => "Heal",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// Parses a wire name, including the legacy spellings the client and the
        /// server used to disagree on.
        /// </summary>
        public static bool TryParse(string value, out OpenGSCore.EFieldItemType type)
        {
            return OpenGSCore.FieldItemTypeNames.TryParse(value, out type);
        }

        /// <summary>
        /// The name written to the wire. Matches what the server writes.
        /// </summary>
        public static string ToWireName(OpenGSCore.EFieldItemType type)
        {
            return OpenGSCore.FieldItemTypeNames.ToWireName(type);
        }
    }

    /// <summary>
    /// Compatibility shim for the older name. Every member forwards to
    /// WorldItemVisualResolver, which is the single implementation.
    /// </summary>
    [System.Obsolete("Use WorldItemVisualResolver and OpenGSCore.EFieldItemType.")]
    public static class FieldItemVisualResolver
    {
        public static string GetDisplayName(OpenGSCore.EFieldItemType type) => WorldItemVisualResolver.GetDisplayName(type);

        public static string GetDisplayName(eFieldItemType type) => WorldItemVisualResolver.GetDisplayName(ToCoreType(type));

        public static bool TryParse(string value, out OpenGSCore.EFieldItemType type) => WorldItemVisualResolver.TryParse(value, out type);

        public static string ToWireName(OpenGSCore.EFieldItemType type) => WorldItemVisualResolver.ToWireName(type);

        /// <summary>
        /// Maps a legacy client value onto the shared type. Every legacy member
        /// is an alias of a shared value, so this is a widening cast, and the
        /// members that had no counterpart, None and Random, stand for a power
        /// up exactly as the old switch said they did.
        /// </summary>
        public static OpenGSCore.EFieldItemType ToCoreType(eFieldItemType type) => (OpenGSCore.EFieldItemType)(int)type;

        public static eFieldItemType ToLegacyType(OpenGSCore.EFieldItemType type) => (eFieldItemType)(int)type;

        public static bool TryParseLegacy(string value, out eFieldItemType type)
        {
            type = eFieldItemType.None;
            return OpenGSCore.FieldItemTypeNames.TryParse(value, out var core)
                && (System.Enum.IsDefined(typeof(eFieldItemType), (int)core));
        }
    }
}