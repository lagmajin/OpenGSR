using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum EInstantItemType
    {
        None = 0,
        FireBullet,
        PoisonBullet,
        HealthKit,

        PowerGrenadePack,
        ClusterGrenadePack,
        MagnetGrenadePack,
        MineGrenadePack,
    }

    /// <summary>
    /// What using an instant item does, decided on the server.
    /// <para>
    /// The client used to name the effect in the use message, as a free form
    /// string, and apply it to itself: "heal", "fire_bullet", "poison_bullet"
    /// and so on. That made the strength of an item a claim by the client, and a
    /// client claiming a bigger one got a bigger one. The effect is now something
    /// the server looks up, so a message names an item and the server says what
    /// that item does.
    /// </para>
    /// </summary>
    public enum EInstantItemEffect
    {
        /// <summary>Restores health, never past the maximum.</summary>
        Heal = 0,

        /// <summary>Changes the rounds a shot carries.</summary>
        Ammo = 1,
    }

    /// <summary>
    /// The rules for using an instant item, shared so both sides agree on what an
    /// item does without the client having to say.
    /// </summary>
    public static class InstantItemRules
    {
        /// <summary>How much a health kit restores.</summary>
        public const int HealAmount = 30;

        /// <summary>
        /// What using the item does, or null when the item does nothing.
        /// </summary>
        public static EInstantItemEffect? EffectOf(EInstantItemType type)
        {
            return type switch
            {
                // A health kit is the only item that changes a stat outright.
                // The bullet and grenade packs change what a shot carries, which
                // is a property of the weapon rather than of the player, so they
                // share one effect here and the weapon side reads it.
                EInstantItemType.HealthKit => EInstantItemEffect.Heal,
                EInstantItemType.FireBullet
                    or EInstantItemType.PoisonBullet
                    or EInstantItemType.PowerGrenadePack
                    or EInstantItemType.ClusterGrenadePack
                    or EInstantItemType.MagnetGrenadePack
                    or EInstantItemType.MineGrenadePack => EInstantItemEffect.Ammo,
                _ => null
            };
        }

        /// <summary>
        /// Whether the item is one a player can carry and spend.
        /// </summary>
        public static bool IsUsable(EInstantItemType type)
        {
            return type != EInstantItemType.None && EffectOf(type).HasValue;
        }
    }
}
