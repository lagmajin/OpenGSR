using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// Central registry for all player instances in the game. Lightweight, single-source of truth for player lookups
    /// and basic operations (damage dispatch, registration). Designed for minimal side-effects: does not change player state
    /// except when explicitly asked (e.g., ApplyDamage).
    /// <para>
    /// It tracks IBattlePlayer rather than one component tree. It used to be
    /// typed on AbstractPlayer, which is not on any player prefab in the project:
    /// the prefabs carry PlayerAgent. So the registry registered nothing, and
    /// everything that asked it for a player, or for the local player, or for the
    /// players on a team, was asking an empty dictionary on a live match.
    /// </para>
    /// </summary>
    public sealed class PlayerRegistry : MonoBehaviour
    {
        public static PlayerRegistry Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance != null || FindFirstObjectByType<PlayerRegistry>() != null)
            {
                return;
            }

            var registryObject = new GameObject(nameof(PlayerRegistry));
            registryObject.AddComponent<PlayerRegistry>();
            DontDestroyOnLoad(registryObject);
        }

        // players keyed by GUID
        private readonly Dictionary<Guid, IBattlePlayer> players = new Dictionary<Guid, IBattlePlayer>();
        private readonly object locker = new object();

        public event Action<IBattlePlayer> OnPlayerRegistered;
        public event Action<IBattlePlayer> OnPlayerUnregistered;
        public event Action<IBattlePlayer, float> OnPlayerHealthChanged; // (player, newHp)
        public event Action<IBattlePlayer, float> OnPlayerArmorChanged; // (player, newArmor)
        public event Action<IBattlePlayer> OnPlayerDied;
        public event Action<IBattlePlayer> OnPlayerSpawned;
        public event Action<IBattlePlayer> OnPlayerRespawned;
        public event Action<IBattlePlayer, PlayerStatus> OnPlayerStatusChanged;
        public event Action<IBattlePlayer, float> OnPlayerBoosterChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple PlayerRegistry instances found - destroying duplicate.");
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var player in FindAllBattlePlayers())
            {
                RegisterPlayer(player);
            }
        }

        /// <summary>
        /// Every live player in the scene, whichever component tree carries it.
        /// <para>
        /// The scan used to be for one type, so it found nothing. Asking for the
        /// interface is what makes both trees visible without the registry having
        /// to know a second type exists.
        /// </para>
        /// </summary>
        private static IEnumerable<IBattlePlayer> FindAllBattlePlayers()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IBattlePlayer>()
                .Where(IsLive);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                lock (locker)
                {
                    players.Clear();
                }

                Instance = null;
            }
        }

        /// <summary>
        /// Whether this is a player that still exists.
        /// <para>
        /// A destroyed Unity component keeps comparing non-null as an interface,
        /// because the overloaded equality on Object is not used for interfaces.
        /// Without this a destroyed player stays in the dictionary and the scoreboard
        /// shows somebody who left.
        /// </para>
        /// </summary>
        private static bool IsLive(IBattlePlayer player)
        {
            // Unity's overloaded equality is only on Object, so an interface has
            // to go through the object to find out whether it has been destroyed.
            return player != null && player is UnityEngine.Object live && live != null;
        }

        public bool RegisterPlayer(IBattlePlayer player)
        {
            if (!IsLive(player)) return false;
            var id = player.UniqueID();
            lock (locker)
            {
                if (players.TryGetValue(id, out var existing))
                {
                    if (IsLive(existing))
                    {
                        return false;
                    }

                    // A destroyed Unity object can remain as a dictionary value
                    // until its OnDisable callback runs. Replace that stale entry
                    // so a newly spawned player with the same ID is not invisible.
                    players.Remove(id);
                }

                players[id] = player;
            }

            InvokeSafely(OnPlayerRegistered, player, nameof(OnPlayerRegistered));
            return true;
        }

        public bool UnregisterPlayer(IBattlePlayer player)
        {
            if (player == null) return false;
            return UnregisterPlayer(player.UniqueID());
        }

        public bool UnregisterPlayer(Guid id)
        {
            IBattlePlayer removed = null;
            lock (locker)
            {
                if (!players.TryGetValue(id, out removed)) return false;
                players.Remove(id);
            }

            if (removed != null)
            {
                InvokeSafely(OnPlayerUnregistered, removed, nameof(OnPlayerUnregistered));
            }

            return true;
        }

        public bool TryGetPlayer(Guid id, out IBattlePlayer player)
        {
            lock (locker)
            {
                if (players.TryGetValue(id, out var found) && IsLive(found))
                {
                    player = found;
                    return true;
                }
            }

            player = null;
            return false;
        }

        public IReadOnlyCollection<IBattlePlayer> GetAllPlayers()
        {
            lock (locker)
            {
                return players.Values.Where(IsLive).ToList().AsReadOnly();
            }
        }

        public IReadOnlyCollection<IBattlePlayer> GetPlayersByTeam(ETeam team)
        {
            lock (locker)
            {
                return players.Values
                    .Where(p => IsLive(p) && p.Team() == team)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Publishes a health update for changes made outside ApplyDamage.
        /// ApplyDamage already raises this event and must not call this method.
        /// </summary>
        public void NotifyPlayerHealthChanged(IBattlePlayer player, float newHp)
        {
            if (!IsLive(player) || float.IsNaN(newHp) || float.IsInfinity(newHp))
            {
                return;
            }

            InvokeSafely(OnPlayerHealthChanged, player, newHp, nameof(OnPlayerHealthChanged));
        }

        public void NotifyPlayerDied(IBattlePlayer player)
        {
            if (IsLive(player))
            {
                InvokeSafely(OnPlayerDied, player, nameof(OnPlayerDied));
            }
        }

        /// <summary>
        /// Apply damage to a player by id. This will call player's AddDamage and then raise events.
        /// Returns true if the player existed and damage was applied.
        /// </summary>
        public bool ApplyDamage(Guid id, Vector2 source, float damage, eDamageType type)
        {
            return ApplyDamage(id, source, damage, type, string.Empty, "Unknown", false, false);
        }

        public bool ApplyDamage(Guid id, Vector2 source, float damage, eDamageType type, string attackerId, string weaponType, bool headshot, bool knockback = false)
        {
            if (!TryGetPlayer(id, out var p) || p == null) return false;
            if (!float.IsFinite(source.x) || !float.IsFinite(source.y)
                || !float.IsFinite(damage) || damage <= 0f || p.IsDead()) return false;

            float prevHp = p.GetHP();
            float prevArmor = p.GetArmor();
            if (!float.IsFinite(prevHp) || !float.IsFinite(prevArmor))
            {
                return false;
            }
            bool wasDead = p.IsDead();

            try
            {
                p.AddDamage(source, damage, type);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying damage to player {id}: {ex.Message}");
                return false;
            }

            float newHp = p.GetHP();
            float newArmor = p.GetArmor();
            if (!float.IsFinite(newHp) || !float.IsFinite(newArmor))
            {
                Debug.LogWarning($"[PlayerRegistry] Ignoring non-finite damage result for player {id}.");
                return false;
            }

            if (!Mathf.Approximately(prevArmor, newArmor))
            {
                InvokeSafely(OnPlayerArmorChanged, p, newArmor, nameof(OnPlayerArmorChanged));
            }

            if (!Mathf.Approximately(prevHp, newHp))
            {
                InvokeSafely(OnPlayerHealthChanged, p, newHp, nameof(OnPlayerHealthChanged));
                GameEventBroker.Publish(new PlayerDamageEvent(
                    targetId: p.UniqueID().ToString(),
                    attackerId: attackerId ?? string.Empty,
                    damage: Mathf.Max(0, Mathf.RoundToInt(prevHp - newHp)),
                    remainingHp: Mathf.Max(0, Mathf.RoundToInt(newHp))
                ));

                if (ShouldPlayLocalPlayerSound(p) && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPlayerSound(GetDamageSound(p));
                }
            }

            if (knockback && (!Mathf.Approximately(prevHp, newHp) || !Mathf.Approximately(prevArmor, newArmor)))
            {
                ApplyKnockback(p, source, damage);
            }

            if (!wasDead && p.IsDead())
            {
                if (p.Status != null) p.Status.DeathCount++;
                InvokeSafely(OnPlayerDied, p, nameof(OnPlayerDied));

                var deadReason = string.IsNullOrWhiteSpace(attackerId) ? EDeadReason.Unknown : EDeadReason.KilledBy;
                var deadEvent = new PlayerDeadEvent(deadReason, p.DisplayName(), p.UniqueID().ToString(), p.Team());
                if (!string.IsNullOrWhiteSpace(attackerId))
                {
                    deadEvent.SetKillerID(attackerId);
                    GameEventBroker.Publish(new PlayerKillEvent(attackerId, p.UniqueID().ToString(), weaponType, headshot));

                    // The killer's own tally. The death count above was credited but
                    // the kill count never was: AddKill had no callers at all, so
                    // every scoreboard and every result row in the game sorted and
                    // displayed zero kills while the deaths beside them counted up.
                    CreditKill(attackerId);
                }

                GameEventBroker.Publish(deadEvent);

                if (ShouldPlayLocalPlayerSound(p) && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPlayerSound(GetDeathSound(p));
                }
            }

            return true;
        }

        /// <summary>
        /// Credits a kill to whoever is named as the attacker.
        /// <para>
        /// The attacker arrives as a string while the registry is keyed by Guid. A
        /// name that does not resolve is skipped rather than treated as an error:
        /// the server settles a kill on its own, and a local hit on a player this
        /// registry never saw is not a tally it can keep.
        /// </para>
        /// </summary>
        private void CreditKill(string attackerId)
        {
            if (string.IsNullOrWhiteSpace(attackerId)) return;

            if (!Guid.TryParse(attackerId, out var attackerGuid)) return;

            TryGetPlayer(attackerGuid, out var attacker);
            if (attacker?.Status != null)
            {
                attacker.Status.AddKill();
            }
        }

        private void ApplyKnockback(IBattlePlayer player, Vector2 source, float damage)
        {
            if (player == null)
            {
                return;
            }

            var impactDirection = source;
            if (impactDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var force = Mathf.Clamp(damage * 0.01f, 0.2f, 0.6f);
            // ApplyDamage has already reduced HP. The impact pass must only
            // add knockback, otherwise AddDamageAndForce would reduce HP a
            // second time.
            player.AddDamageAndForce(0f, impactDirection, force);
        }

        /// <summary>
        /// Convenience: find player by Unity instance id
        /// </summary>
        public bool TryGetPlayerByInstanceId(int instanceId, out IBattlePlayer player)
        {
            lock (locker)
            {
                foreach (var p in players.Values)
                {
                    if (!IsLive(p)) continue;
                    if (UnityObjectIdCompat.GetObjectId(p.Root) == instanceId)
                    {
                        player = p;
                        return true;
                    }
                }
            }

            player = null;
            return false;
        }

        /// <summary>
        /// Clear all registrations (editor or scene reset)
        /// </summary>
        public void ClearAll()
        {
            List<IBattlePlayer> removedPlayers;
            lock (locker)
            {
                removedPlayers = players.Values.Where(IsLive).ToList();
                players.Clear();
            }

            foreach (var removedPlayer in removedPlayers)
            {
                InvokeSafely(OnPlayerUnregistered, removedPlayer, nameof(OnPlayerUnregistered));
            }
        }

        public void PublishPlayerStatus(IBattlePlayer player)
        {
            if (!IsLive(player)) return;
            InvokeSafely(OnPlayerStatusChanged, player, player.Status, nameof(OnPlayerStatusChanged));
        }

        public void PublishPlayerBooster(IBattlePlayer player, float newBooster)
        {
            if (!IsLive(player)) return;

            if (player.Status != null)
            {
                player.Status.Booster = newBooster;
                newBooster = player.Status.Booster;
            }

            InvokeSafely(OnPlayerBoosterChanged, player, newBooster, nameof(OnPlayerBoosterChanged));
        }

        public void PublishPlayerSpawned(IBattlePlayer player)
        {
            if (!IsLive(player)) return;
            InvokeSafely(OnPlayerSpawned, player, nameof(OnPlayerSpawned));
        }

        public void PublishPlayerRespawned(IBattlePlayer player)
        {
            if (!IsLive(player)) return;
            InvokeSafely(OnPlayerRespawned, player, nameof(OnPlayerRespawned));
        }

        private static void InvokeSafely<T>(Action<T> handlers, T value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PlayerRegistry] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely<T>(Action<IBattlePlayer, T> handlers, IBattlePlayer player, T value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<IBattlePlayer, T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(player, value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PlayerRegistry] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static bool ShouldPlayLocalPlayerSound(IBattlePlayer player)
        {
            return IsLive(player) && player.PlayerType() == EPlayerType.MyPlayer;
        }

        private static bool IsFemaleCharacter(IBattlePlayer player)
        {
            if (!IsLive(player))
            {
                return false;
            }

            return player.Character() switch
            {
                EPlayerCharacter.Ami => true,
                EPlayerCharacter.Yumi => true,
                EPlayerCharacter.Misty => true,
                EPlayerCharacter.Mary => true,
                EPlayerCharacter.Jackle => true,
                _ => false
            };
        }

        private static EPlayerSound GetDamageSound(IBattlePlayer player)
        {
            return IsFemaleCharacter(player)
                ? EPlayerSound.DamageFemale1
                : EPlayerSound.DamageMale1;
        }

        private static EPlayerSound GetDeathSound(IBattlePlayer player)
        {
            var female = IsFemaleCharacter(player);
            var offset = UnityEngine.Random.Range(0, 4);
                return female
                ? (EPlayerSound)((int)EPlayerSound.DeathFemale1 + offset)
                : (EPlayerSound)((int)EPlayerSound.DeathMale1 + offset);
        }
    }
}
