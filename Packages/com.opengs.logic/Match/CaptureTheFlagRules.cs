using System;
using System.Collections.Generic;

namespace OpenGSCore
{
    /// <summary>
    /// Why a flag went back to its own stand.
    /// <para>
    /// The client carried this as an enum nested in its flag component. The reason
    /// matters because a return is not one thing: a flag that timed out on the
    /// ground and a flag a team deliberately carried home are both home, but only
    /// one of them involved a player, and only the second is a team spending
    /// effort. The nested spelling meant the server could not have told them apart
    /// either.
    /// </para>
    /// </summary>
    public enum EFlagReturnReason
    {
        /// <summary>It sat on the ground for long enough to go home on its own.</summary>
        AutoReturn = 0,

        /// <summary>A player of the owning team carried it back.</summary>
        FriendlyRecovered = 1,

        /// <summary>It was delivered to the other team's stand and the match scored.</summary>
        CapturedAtBase = 2
    }

    /// <summary>
    /// Why a delivery did not score.
    /// </summary>
    public enum EFlagRefusal
    {
        /// <summary>Nothing is wrong with it.</summary>
        None = 0,

        /// <summary>The player's own flag is not on its stand, which is the rule the mode turns on.</summary>
        OwnFlagNotAtBase = 1,

        /// <summary>Nobody is carrying the enemy flag, so there is nothing to deliver.</summary>
        NoEnemyFlagCarried = 2,

        /// <summary>The player is on no team, so there is no flag of theirs to defend.</summary>
        NotATeam = 3
    }

    /// <summary>
    /// Where one team's flag is, and who has it.
    /// <para>
    /// The server held nothing but a record of who was carrying something, keyed
    /// by the carrier. That cannot answer where a flag is, so the capture rule
    /// could not be applied: a team whose own flag was in the other side's hands
    /// scored exactly as well as one whose flag was safe. A flag belongs to a team
    /// and that is the identity the state has to be held under.
    /// </para>
    /// </summary>
    public sealed class TeamFlag
    {
        public TeamFlag(ETeam team)
        {
            Team = team;
            State = EFlagState.FlagOnStand;
        }

        /// <summary>The team this flag belongs to. A red player carries the blue flag.</summary>
        public ETeam Team { get; }

        public EFlagState State { get; private set; }

        /// <summary>
        /// The player carrying the flag, or null when it is not being carried.
        /// <para>
        /// A capture is judged against this rather than against a message, because
        /// a message naming a carrier is that carrier's own claim about itself.
        /// </para>
        /// </summary>
        public string? CarrierId { get; private set; }

        /// <summary>
        /// Whether a flag is on the ground waiting to be picked up or to go home.
        /// <para>
        /// This is what the auto return asks, rather than a flag on the drop time,
        /// because a flag dropped at the start of a clock is a flag on the ground
        /// like any other. Treating a timestamp of zero as "not dropped" would
        /// quietly exempt the first drop of a match from ever coming back.
        /// </para>
        /// </summary>
        private bool waitingOnGround;

        public bool IsAtBase => State.IsStable();

        public bool IsCarried => State.IsCarried();

        public bool IsDropped => State.IsDropped();

        /// <summary>
        /// How long a dropped flag waits before it goes home on its own.
        /// <para>
        /// The client used to carry its own number for this, and the server had
        /// another, so the two sides could disagree about when a flag was due
        /// back. That is not a cosmetic difference: a client that waits longer
        /// than the server destroys the flag later than the server has already
        /// put it home, and a client that waits less brings a flag back the server
        /// still believes is lying on the ground. It is one rule, so it is one
        /// number, and the client draws it from here rather than keeping a copy.
        /// </para>
        /// </summary>
        public const float DefaultAutoReturnSeconds = CaptureTheFlagRules.DefaultAutoReturnSeconds;

        private float autoReturnSeconds = DefaultAutoReturnSeconds;

        /// <summary>
        /// When the flag was dropped, on a clock the client cannot set. Zero
        /// when it is not lying on the ground.
        /// </summary>
        public double DroppedAtSeconds { get; private set; }

        /// <summary>
        /// How long this flag waits on the ground before it returns by itself.
        /// </summary>
        public float AutoReturnSeconds
        {
            get => autoReturnSeconds;
            set => autoReturnSeconds = CaptureTheFlagRules.SanitizeAutoReturnSeconds(value);
        }

        /// <summary>
        /// Puts the flag in a player's hands.
        /// <para>
        /// A flag that is already carried is not taken again: it is one object, so
        /// the second claim names a flag that is not on the ground.
        /// </para>
        /// </summary>
        public bool PickUp(string carrierId)
        {
            if (string.IsNullOrWhiteSpace(carrierId) || IsCarried)
            {
                return false;
            }

            State = EFlagState.FlagCapturedPlayer;
            CarrierId = carrierId;
            waitingOnGround = false;
            DroppedAtSeconds = 0.0d;
            return true;
        }

        /// <summary>
        /// Takes the flag out of the carrier's hands and leaves it on the ground.
        /// <para>
        /// The clock starts here, because a flag on the ground is on a countdown
        /// whether or not anybody is around to see it go home.
        /// </para>
        /// </summary>
        public bool Drop(double nowSeconds)
        {
            if (!IsCarried)
            {
                return false;
            }

            State = EFlagState.FlagOnGround;
            CarrierId = null;
            waitingOnGround = true;
            DroppedAtSeconds = nowSeconds;
            return true;
        }

        /// <summary>
        /// Puts the flag back on its own stand.
        /// <para>
        /// A flag that is already home is not put back again. It is not a change,
        /// and reporting one would make a reset look like something happening.
        /// </para>
        /// </summary>
        public bool Return(EFlagReturnReason reason)
        {
            if (IsAtBase)
            {
                return false;
            }

            State = EFlagState.FlagOnStand;
            CarrierId = null;
            waitingOnGround = false;
            DroppedAtSeconds = 0.0d;
            return true;
        }

        /// <summary>
        /// Whether a flag lying on the ground has been there long enough to go
        /// home by itself.
        /// <para>
        /// This is asked rather than done, so the caller decides what a return by
        /// itself announces. A flag that nobody claims has to come back on its own
        /// or a team that lost a carrier can never pick their flag up again and is
        /// out of the match by a rule nobody chose.
        /// </para>
        /// </summary>
        public bool HasTimedOutOnGround(double nowSeconds)
        {
            return waitingOnGround && (nowSeconds - DroppedAtSeconds) >= autoReturnSeconds;
        }
    }

    /// <summary>
    /// The rules a capture has to satisfy, shared so the client can predict the
    /// outcome and the server can decide it.
    /// <para>
    /// The core loop says a team scores when it brings the enemy flag to its own
    /// stand while its own flag is still at base. The second half was missing
    /// entirely, and it is the half the mode turns on.
    /// </para>
    /// </summary>
    public static class CaptureTheFlagRules
    {
        /// <summary>
        /// How long a dropped flag waits before it goes home on its own.
        /// <para>
        /// The client used to carry its own number for this and the server had
        /// another, so the two sides could disagree about when a flag was due
        /// back. That is not a cosmetic difference: a client that waits longer
        /// than the server destroys the flag after the server has already put it
        /// home, and a client that waits less brings one back that the server
        /// still believes is lying on the ground. It is one rule, so it is one
        /// number.
        /// </para>
        /// </summary>
        public const float DefaultAutoReturnSeconds = 30f;

        /// <summary>
        /// Clamps a wait into something a flag can actually wait.
        /// <para>
        /// A wait of zero is a flag that comes home the instant it is dropped,
        /// and a wait that is not a number is a flag that never comes home. Both
        /// are worse than the default, so both fall back to it rather than being
        /// taken as given.
        /// </para>
        /// </summary>
        public static float SanitizeAutoReturnSeconds(float seconds)
        {
            return float.IsFinite(seconds) ? MathF.Max(0.1f, seconds) : DefaultAutoReturnSeconds;
        }

        /// <summary>
        /// Whether a delivery scores.
        /// </summary>
        public static bool CanScore(ETeam scoringTeam, IReadOnlyDictionary<ETeam, TeamFlag> flags)
        {
            return RefusalFor(scoringTeam, flags) == EFlagRefusal.None;
        }

        /// <summary>
        /// Why a delivery would not score, or None when it would.
        /// <para>
        /// A caller that only wants a yes or no can use CanScore, but a caller
        /// that is going to tell a player why nothing happened wants the reason.
        /// That is the difference between a rule and a mystery.
        /// </para>
        /// </summary>
        public static EFlagRefusal RefusalFor(ETeam scoringTeam, IReadOnlyDictionary<ETeam, TeamFlag> flags)
        {
            if (scoringTeam != ETeam.Red && scoringTeam != ETeam.Blue)
            {
                return EFlagRefusal.NotATeam;
            }

            // A team whose own flag the server has never heard of is not a team
            // whose flag is safe. Treating an unknown flag as one on its stand is
            // the exact hole this rule is here to close.
            if (flags == null || !flags.TryGetValue(scoringTeam, out var ownFlag) || ownFlag == null)
            {
                return EFlagRefusal.OwnFlagNotAtBase;
            }

            if (!ownFlag.IsAtBase)
            {
                return EFlagRefusal.OwnFlagNotAtBase;
            }

            var enemyFlag = OpposingFlag(scoringTeam, flags);
            if (enemyFlag == null || !enemyFlag.IsCarried)
            {
                // A team cannot score by delivering a flag nobody is carrying.
                return EFlagRefusal.NoEnemyFlagCarried;
            }

            return EFlagRefusal.None;
        }

        /// <summary>
        /// The other team's flag, or null when the match has no other team.
        /// </summary>
        public static TeamFlag? OpposingFlag(ETeam team, IReadOnlyDictionary<ETeam, TeamFlag> flags)
        {
            if (flags == null)
            {
                return null;
            }

            var enemy = OpposingTeam(team);
            return enemy != ETeam.NoTeam && flags.TryGetValue(enemy, out var flag) ? flag : null;
        }

        public static ETeam OpposingTeam(ETeam team)
        {
            return team switch
            {
                ETeam.Red => ETeam.Blue,
                ETeam.Blue => ETeam.Red,
                _ => ETeam.NoTeam
            };
        }

        /// <summary>
        /// Puts both flags back on their stands after a score.
        /// <para>
        /// The rule resets the flag state when a team scores, so this is the whole
        /// of that reset.
        /// </para>
        /// </summary>
        public static void ResetAll(IReadOnlyDictionary<ETeam, TeamFlag> flags)
        {
            if (flags == null)
            {
                return;
            }

            foreach (var flag in flags.Values)
            {
                flag?.Return(EFlagReturnReason.AutoReturn);
            }
        }

        /// <summary>
        /// Puts a flag that has been lying on the ground long enough back on its
        /// stand, and says which team it was.
        /// <para>
        /// The timer belongs to the server because the server holds the flag. When
        /// only the client had one, a dropped flag came back if and only if some
        /// client lived long enough to say so, so a single message that did not
        /// arrive left a flag on the ground for the rest of the match and a team
        /// that could never pick it up.
        /// </para>
        /// </summary>
        /// <returns>The team whose flag went home, or NoTeam when nothing did.</returns>
        public static ETeam ReturnTimedOutFlags(
            IReadOnlyDictionary<ETeam, TeamFlag> flags,
            double nowSeconds)
        {
            if (flags == null)
            {
                return ETeam.NoTeam;
            }

            foreach (var flag in flags.Values)
            {
                if (flag != null &&
                    flag.HasTimedOutOnGround(nowSeconds) &&
                    flag.Return(EFlagReturnReason.AutoReturn))
                {
                    return flag.Team;
                }
            }

            return ETeam.NoTeam;
        }
    }
}
