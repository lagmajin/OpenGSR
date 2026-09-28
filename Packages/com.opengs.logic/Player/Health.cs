using System;

namespace OpenGSCore
{
    /// <summary>
    /// The health the server reported, and whether the local one should move.
    /// <para>
    /// The client keeps its own health and used to reconcile it by hand inside
    /// the player component, which put the arithmetic where no test could reach
    /// it. The server owns health, so the rule belongs next to the other shared
    /// rules: adopt the ceiling the server is using, clamp the reported value
    /// into it, and only report a change when the number actually moved.
    /// </para>
    /// <para>
    /// Pure, so both sides can call it and the test project can check it without
    /// a scene.
    /// </para>
    /// </summary>
    public static class AuthoritativeHealth
    {
        /// <summary>
        /// Bounds accepted for a reported ceiling. A negative or absurd value
        /// would either wipe a player or leave the bar unwinnable, so a
        /// malformed message cannot rewrite it.
        /// </summary>
        public const int MinMaxHealth = 1;
        public const int MaxMaxHealth = 100000;

        /// <summary>
        /// Adopts a ceiling reported by the server.
        /// <para>
        /// A value of zero or less means the message did not carry a ceiling, so
        /// the local one is kept. That is what a death message does, since it
        /// says who is out but not what the ceiling is.
        /// </para>
        /// </summary>
        /// <returns>The ceiling to use, and whether it moved.</returns>
        public static (int Ceiling, bool Changed) AdoptCeiling(
            int localCeiling,
            int reportedCeiling)
        {
            if (reportedCeiling <= 0)
            {
                return (localCeiling, false);
            }

            var clamped = Ceiling(reportedCeiling);
            return (clamped, clamped != localCeiling);
        }

        /// <summary>
        /// Clamps a reported health into the range the ceiling allows.
        /// <para>
        /// The reported value is never trusted above the ceiling and never
        /// allowed below zero, so a message claiming more health than the
        /// player can hold cannot hand out a free full bar.
        /// </para>
        /// </summary>
        public static int ClampRemaining(int reportedRemaining, int ceiling)
        {
            var effective = Ceiling(ceiling);
            if (reportedRemaining < 0)
            {
                return 0;
            }

            if (reportedRemaining > effective)
            {
                return effective;
            }

            return reportedRemaining;
        }

        /// <summary>
        /// The ceiling to use, after bounding an arbitrary value.
        /// </summary>
        public static int Ceiling(int value)
        {
            if (value < MinMaxHealth)
            {
                return MinMaxHealth;
            }

            if (value > MaxMaxHealth)
            {
                return MaxMaxHealth;
            }

            return value;
        }

        /// <summary>
        /// Whether a health change is worth acting on. A message that says what
        /// the local value already is must not raise a death, because a
        /// repeated message would otherwise kill a player twice over.
        /// </summary>
        public static bool IsMeaningfulChange(float localRemaining, int adoptedRemaining)
        {
            if (localRemaining < 0f)
            {
                localRemaining = 0f;
            }

            // Whole health is a server unit, so anything under half a point of
            // difference is the same number arriving twice.
            return MathF.Abs(localRemaining - adoptedRemaining) >= 0.5f;
        }
    }
}
