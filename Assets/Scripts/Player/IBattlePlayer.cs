using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// What the game needs to know about a player in the match, and what it needs
    /// to be able to do to one.
    /// <para>
    /// There are two player component trees: AbstractPlayer, which carries a
    /// PlayerStatus and registers itself with PlayerRegistry, and PlayerAgent,
    /// which is what every player prefab in the project actually carries. They
    /// share no base class, so everything that looked players up went through one
    /// of the two by name and found nothing on a real match: the registry was
    /// empty, a bullet's hit test returned null, and the scoreboard, the kill feed
    /// and the health bar had no player to read.
    /// </para>
    /// <para>
    /// The member names are deliberately the ones AbstractPlayer already used, so
    /// that a caller reads the same either way rather than having to know which
    /// tree it got.
    /// </para>
    /// </summary>
    public interface IBattlePlayer
    {
        /// <summary>Stable identity for this player for as long as it is alive.</summary>
        System.Guid UniqueID();

        /// <summary>Moves this player onto an identity another body already held.</summary>
        void SetUniqueID(System.Guid id);

        /// <summary>What to put on a nameplate, a kill feed row or a scoreboard.</summary>
        string DisplayName();

        ETeam Team();
        void SetTeam(ETeam team);
        bool HasTeam();

        EPlayerType PlayerType();
        void SetPlayerType(EPlayerType type);

        /// <summary>
        /// Called once the player exists in the world and its equipment is in
        /// place. A mode that only sets a type is not the same as a player that
        /// has been set up.
        /// </summary>
        void OnSpawn();

        /// <summary>The damage this player is currently doing, buffs included.</summary>
        float AttackMultiplier();

        EPlayerCharacter Character();

        /// <summary>True once the player is out, whichever way that happened.</summary>
        bool IsDead();

        float GetHP();
        float GetMaxHP();
        float GetArmor();
        float GetMaxArmor();
        float GetBooster();
        float GetMaxBooster();

        /// <summary>
        /// The health, armour and tally for this player. It is the only copy:
        /// a second one is a second answer to "how is this player doing".
        /// </summary>
        PlayerStatus Status { get; }

        /// <summary>The transform to follow, to aim at, or to stand on.</summary>
        Transform Transform { get; }

        /// <summary>The object carrying this component, for lookups above it.</summary>
        GameObject Root { get; }

        void AddDamage(Vector2 source, float damage, eDamageType type);
        void AddDamageAndForce(float damage, Vector3 direction, float force = 1f);

        /// <summary>
        /// Adopts the health the server says this player has. Returns false when
        /// nothing changed.
        /// </summary>
        bool ApplyServerHealth(int remainingHealth, int maxHealth);

        void Heal(float heal = 0f);

        /// <summary>Takes back every timed effect, for a respawn or a clear.</summary>
        void CancelTimedBuffs();
    }
}
