using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool RegisterPlayer(AbstractPlayer player)
        {
            if (player == null) return false;
            var id = player.UniqueID();
            lock (locker)
            {
                if (players.ContainsKey(id)) return false;
                players[id] = player;
            }

            OnPlayerRegistered?.Invoke(player);
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
                OnPlayerUnregistered?.Invoke(removed);
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
                return players.Values.ToList().AsReadOnly();
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
        /// Apply damage to a player by id. This will call player's AddDamage and then raise events.
        /// Returns true if the player existed and damage was applied.
        /// </summary>
        public bool ApplyDamage(Guid id, Vector2 source, float damage, eDamageType type)
        {
            if (!TryGetPlayer(id, out var p) || p == null) return false;

            float prevHp = GetPlayerHpSafe(p);
            float prevArmor = GetPlayerArmorSafe(p);
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

            if (!Mathf.Approximately(prevArmor, newArmor))
            {
                OnPlayerArmorChanged?.Invoke(p, newArmor);
            }

            if (!Mathf.Approximately(prevHp, newHp))
            {
                OnPlayerHealthChanged?.Invoke(p, newHp);
            }

            if (!wasDead && p.IsDead())
            {
                if (p.Status != null) p.Status.DeathCount++;
                OnPlayerDied?.Invoke(p);
            }

            return true;
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
                    if (p.gameObject.GetInstanceID() == instanceId)
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
            lock (locker)
            {
                players.Clear();
            }
        }

        public void PublishPlayerStatus(AbstractPlayer player)
        {
            if (player == null) return;
            OnPlayerStatusChanged?.Invoke(player, player.Status);
        }

        public void PublishPlayerBooster(AbstractPlayer player, float newBooster)
        {
            if (player == null) return;
            OnPlayerBoosterChanged?.Invoke(player, newBooster);
        }

        public void PublishPlayerSpawned(AbstractPlayer player)
        {
            if (player == null) return;
            OnPlayerSpawned?.Invoke(player);
        }

        public void PublishPlayerRespawned(AbstractPlayer player)
        {
            if (player == null) return;
            OnPlayerRespawned?.Invoke(player);
        }
    }
}
