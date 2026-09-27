namespace OpenGS
{
    /// <summary>
    /// Kept only so older call sites still compile. The client used to carry
    /// its own item type enum with the same values under different names from
    /// the server, which meant two sources of truth for the same concept and no
    /// way to catch a mismatch at compile time.
    /// <para>
    /// Every member here is an alias for OpenGSCore.EFieldItemType, so the two
    /// enums are now the same set of values rather than two parallel sets. The
    /// client local values that have no counterpart, None and Random, map onto
    /// the item they stood in for: Random resolved to PowerUpItem whenever it
    /// was converted, and HealItem had no member at all and so also resolved to
    /// PowerUpItem.
    /// </para>
    /// <para>
    /// New code should use OpenGSCore.EFieldItemType directly and
    /// WorldItemVisualResolver for display names and parsing.
    /// </para>
    /// </summary>
    [System.Obsolete("Use OpenGSCore.EFieldItemType. This alias exists only to keep older call sites compiling.")]
    public enum eFieldItemType
    {
        None = (int)OpenGSCore.EFieldItemType.PowerUpItem,
        PowerUp = (int)OpenGSCore.EFieldItemType.PowerUpItem,
        PowerUpItem = (int)OpenGSCore.EFieldItemType.PowerUpItem,
        DefenceUp = (int)OpenGSCore.EFieldItemType.DefenceUpItem,
        DefenceUpItem = (int)OpenGSCore.EFieldItemType.DefenceUpItem,
        Stealth = (int)OpenGSCore.EFieldItemType.StealthItem,
        StealthItem = (int)OpenGSCore.EFieldItemType.StealthItem,
        SpeedUp = (int)OpenGSCore.EFieldItemType.SpeedUpItem,
        SpeedUpItem = (int)OpenGSCore.EFieldItemType.SpeedUpItem,
        NormalGrenadePack = (int)OpenGSCore.EFieldItemType.GrenadePack,
        GrenadePack = (int)OpenGSCore.EFieldItemType.GrenadePack,
        Random = (int)OpenGSCore.EFieldItemType.PowerUpItem,
        RocketLauncher = (int)OpenGSCore.EFieldItemType.GranadeLauncher,
        FlameThrower = (int)OpenGSCore.EFieldItemType.FlameThrower,
        Heal = (int)OpenGSCore.EFieldItemType.HealItem,
        HealItem = (int)OpenGSCore.EFieldItemType.HealItem
    }
}