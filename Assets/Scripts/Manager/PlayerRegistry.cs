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
        private readonly Dictionary<Guid, AbstractPlayer> players = new Dictionary<Guid, AbstractPlayer>();
        private readonly object locker = new object();

        public event Action<AbstractPlayer> OnPlayerRegistered;
        public event Action<AbstractPlayer> OnPlayerUnregistered;
        public event Action<AbstractPlayer, float> OnPlayerHealthChanged; // (player, newHp)
        public event Action<AbstractPlayer, float> OnPlayerArmorChanged; // (player, newArmor)
        public event Action<AbstractPlayer> OnPlayerDied;
        public event Action<AbstractPlayer> OnPlayerSpawned;
        public event Action<AbstractPlayer> OnPlayerRespawned;
        public event Action<AbstractPlayer, PlayerStatus> OnPlayerStatusChanged;
        public event Action<AbstractPlayer, float> OnPlayerBoosterChanged;

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

            foreach (var player in FindObjectsByType<AbstractPlayer>(FindObjectsSortMode.None))
            {
                RegisterPlayer(player);
            }
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

        public bool RegisterPlayer(AbstractPlayer player)
        {
            if (player == null) return false;
            var id = player.UniqueID();
            lock (locker)
            {
                if (players.TryGetValue(id, out var existing))
                {
                    if (existing != null)
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

        public bool UnregisterPlayer(AbstractPlayer player)
        {
            if (player == null) return false;
            return UnregisterPlayer(player.UniqueID());
        }

        public bool UnregisterPlayer(Guid id)
        {
            AbstractPlayer removed = null;
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

        public bool TryGetPlayer(Guid id, out AbstractPlayer player)
        {
            lock (locker)
            {
                return players.TryGetValue(id, out player);
            }
        }

        public IReadOnlyCollection<AbstractPlayer> GetAllPlayers()
        {
            lock (locker)
            {
                return players.Values.Where(player => player != null).ToList().AsReadOnly();
            }
        }

        public IReadOnlyCollection<AbstractPlayer> GetPlayersByTeam(ETeam team)
        {
            lock (locker)
            {
                return players.Values.Where(p => p != null && p.Team() == team).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Publishes a health update for changes made outside ApplyDamage.
        /// ApplyDamage already raises this event and must not call this method.
        /// </summary>
        public void NotifyPlayerHealthChanged(AbstractPlayer player, float newHp)
        {
            if (player == null || float.IsNaN(newHp) || float.IsInfinity(newHp))
            {
                return;
            }

            InvokeSafely(OnPlayerHealthChanged, player, newHp, nameof(OnPlayerHealthChanged));
        }

        public void NotifyPlayerDied(AbstractPlayer player)
        {
            if (player != null)
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

            float prevHp = GetPlayerHpSafe(p);
            float prevArmor = GetPlayerArmorSafe(p);
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

            float newHp = GetPlayerHpSafe(p);
            float newArmor = GetPlayerArmorSafe(p);
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
                var deadEvent = new PlayerDeadEvent(deadReason, p.gameObject.name, p.UniqueID().ToString(), p.Team());
                if (!string.IsNullOrWhiteSpace(attackerId))
                {
                    deadEvent.SetKillerID(attackerId);
                    GameEventBroker.Publish(new PlayerKillEvent(attackerId, p.UniqueID().ToString(), weaponType, headshot));
                }

                GameEventBroker.Publish(deadEvent);

                if (ShouldPlayLocalPlayerSound(p) && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPlayerSound(GetDeathSound(p));
                }
            }

            return true;
        }

        private void ApplyKnockback(AbstractPlayer player, Vector2 source, float damage)
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

        private float GetPlayerHpSafe(AbstractPlayer p)
        {
            try
            {
                if (p is IPlayer player)
                {
                    return player.GetHP();
                }
            }
            catch { }

            return -1f;
        }

        private float GetPlayerArmorSafe(AbstractPlayer p)
        {
            try
            {
                if (p is IPlayer player)
                {
                    return player.GetArmor();
                }
            }
            catch { }

            return -1f;
        }

        /// <summary>
        /// Convenience: find player by Unity instance id
        /// </summary>
        public bool TryGetPlayerByInstanceId(int instanceId, out AbstractPlayer player)
        {
            lock (locker)
            {
                foreach (var p in players.Values)
                {
                    if (p == null) continue;
                    if (UnityObjectIdCompat.GetObjectId(p.gameObject) == instanceId)
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
            List<AbstractPlayer> removedPlayers;
            lock (locker)
            {
                removedPlayers = players.Values.Where(player => player != null).ToList();
                players.Clear();
            }

            foreach (var removedPlayer in removedPlayers)
            {
                InvokeSafely(OnPlayerUnregistered, removedPlayer, nameof(OnPlayerUnregistered));
            }
        }

        public void PublishPlayerStatus(AbstractPlayer player)
        {
            if (player == null) return;
            InvokeSafely(OnPlayerStatusChanged, player, player.Status, nameof(OnPlayerStatusChanged));
        }

        public void PublishPlayerBooster(AbstractPlayer player, float newBooster)
        {
            if (player == null) return;

            if (player.Status != null)
            {
                player.Status.Booster = newBooster;
                newBooster = player.Status.Booster;
            }

            InvokeSafely(OnPlayerBoosterChanged, player, newBooster, nameof(OnPlayerBoosterChanged));
        }

        public void PublishPlayerSpawned(AbstractPlayer player)
        {
            if (player == null) return;
            InvokeSafely(OnPlayerSpawned, player, nameof(OnPlayerSpawned));
        }

        public void PublishPlayerRespawned(AbstractPlayer player)
        {
            if (player == null) return;
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

        private static void InvokeSafely<T>(Action<AbstractPlayer, T> handlers, AbstractPlayer player, T value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<AbstractPlayer, T> handler in handlers.GetInvocationList())
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

        private static bool ShouldPlayLocalPlayerSound(AbstractPlayer player)
        {
            return player != null && player.PlayerType() == EPlayerType.MyPlayer;
        }

        private static bool IsFemaleCharacter(AbstractPlayer player)
        {
            if (player == null)
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

        private static EPlayerSound GetDamageSound(AbstractPlayer player)
        {
            return IsFemaleCharacter(player)
                ? EPlayerSound.DamageFemale1
                : EPlayerSound.DamageMale1;
        }

        private static EPlayerSound GetDeathSound(AbstractPlayer player)
        {
            var female = IsFemaleCharacter(player);
            var offset = UnityEngine.Random.Range(0, 4);
                return female
                ? (EPlayerSound)((int)EPlayerSound.DeathFemale1 + offset)
                : (EPlayerSound)((int)EPlayerSound.DeathMale1 + offset);
        }
    }
}
