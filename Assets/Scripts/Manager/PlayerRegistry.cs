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

        // players keyed by GUID
        private readonly Dictionary<Guid, AbstractPlayer> players = new Dictionary<Guid, AbstractPlayer>();
        private readonly Dictionary<Guid, Vector3> remotePositionTargets = new Dictionary<Guid, Vector3>();
        private readonly Dictionary<Guid, Vector3> remotePositionVelocities = new Dictionary<Guid, Vector3>();
        private readonly object locker = new object();

        private const float RemotePositionSmoothTime = 0.06f;

        public event Action<AbstractPlayer> OnPlayerRegistered;
        public event Action<AbstractPlayer> OnPlayerUnregistered;
        public event Action<AbstractPlayer, float> OnPlayerHealthChanged; // (player, newHp)
        public event Action<AbstractPlayer, float> OnPlayerArmorChanged; // (player, newArmor)
        public event Action<AbstractPlayer> OnPlayerDied;
        public event Action<AbstractPlayer> OnPlayerSpawned;
        public event Action<AbstractPlayer> OnPlayerRespawned;
        public event Action<AbstractPlayer, PlayerStatus> OnPlayerStatusChanged;
        public event Action<AbstractPlayer, float> OnPlayerBoosterChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsurePersistentRegistry()
        {
            if (Instance != null || FindFirstObjectByType<PlayerRegistry>() != null)
            {
                return;
            }

            var registryObject = new GameObject(nameof(PlayerRegistry));
            DontDestroyOnLoad(registryObject);
            registryObject.AddComponent<PlayerRegistry>();
        }

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

        private void LateUpdate()
        {
            lock (locker)
            {
                foreach (var entry in remotePositionTargets)
                {
                    if (!players.TryGetValue(entry.Key, out var player) || player == null ||
                        player.PlayerType() == EPlayerType.MyPlayer)
                    {
                        continue;
                    }

                    var velocity = remotePositionVelocities.TryGetValue(entry.Key, out var currentVelocity)
                        ? currentVelocity
                        : Vector3.zero;
                    player.transform.position = Vector3.SmoothDamp(
                        player.transform.position,
                        entry.Value,
                        ref velocity,
                        RemotePositionSmoothTime);
                    remotePositionVelocities[entry.Key] = velocity;
                }
            }
        }

        public bool ApplyRemotePosition(Guid id, Vector3 position)
        {
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                return false;
            }

            lock (locker)
            {
                if (!players.TryGetValue(id, out var player) || player == null ||
                    player.PlayerType() == EPlayerType.MyPlayer)
                {
                    return false;
                }

                remotePositionTargets[id] = position;
                return true;
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public bool RegisterPlayer(AbstractPlayer player)
        {
            if (player == null) return false;
            var id = player.UniqueID();
            AbstractPlayer replacedPlayer = null;
            lock (locker)
            {
                if (players.TryGetValue(id, out replacedPlayer) && replacedPlayer == player)
                {
                    return false;
                }

                players[id] = player;
                remotePositionTargets.Remove(id);
                remotePositionVelocities.Remove(id);
            }

            if (replacedPlayer != null)
            {
                OnPlayerUnregistered?.Invoke(replacedPlayer);
            }
            OnPlayerRegistered?.Invoke(player);
            return true;
        }

        public bool UnregisterPlayer(AbstractPlayer player)
        {
            if (player == null) return false;

            var id = player.UniqueID();
            lock (locker)
            {
                if (!players.TryGetValue(id, out var registeredPlayer) || registeredPlayer != player)
                {
                    return false;
                }

                players.Remove(id);
                remotePositionTargets.Remove(id);
                remotePositionVelocities.Remove(id);
            }

            OnPlayerUnregistered?.Invoke(player);
            return true;
        }

        public bool UnregisterPlayer(Guid id)
        {
            AbstractPlayer removed = null;
            lock (locker)
            {
                if (!players.TryGetValue(id, out removed)) return false;
                players.Remove(id);
                remotePositionTargets.Remove(id);
                remotePositionVelocities.Remove(id);
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
            return ApplyDamage(id, source, damage, type, string.Empty, "Unknown", false);
        }

        public bool ApplyDamage(Guid id, Vector2 source, float damage, eDamageType type, string attackerId, string weaponType, bool headshot)
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
                GameEventBroker.Publish(new PlayerDamageEvent(
                    targetId: p.UniqueID().ToString(),
                    attackerId: attackerId ?? string.Empty,
                    damage: Mathf.Max(0, Mathf.RoundToInt(prevHp - newHp)),
                    remainingHp: Mathf.Max(0, Mathf.RoundToInt(newHp))
                ));

                if (ShouldPlayLocalPlayerSound(p))
                {
                    SoundManager.Instance.PlayPlayerSound(GetDamageSound(p));
                }
            }

            if (!wasDead && p.IsDead())
            {
                if (p.Status != null) p.Status.DeathCount++;
                OnPlayerDied?.Invoke(p);

                var deadReason = string.IsNullOrWhiteSpace(attackerId) ? EDeadReason.Unknown : EDeadReason.KilledBy;
                var deadEvent = new PlayerDeadEvent(deadReason, p.gameObject.name, p.UniqueID().ToString(), p.Team());
                if (!string.IsNullOrWhiteSpace(attackerId))
                {
                    deadEvent.SetKillerID(attackerId);
                    GameEventBroker.Publish(new PlayerKillEvent(attackerId, p.UniqueID().ToString(), weaponType, headshot));
                }

                GameEventBroker.Publish(deadEvent);

                if (ShouldPlayLocalPlayerSound(p))
                {
                    SoundManager.Instance.PlayPlayerSound(GetDeathSound(p));
                }
            }

            return true;
        }

        public bool ApplyAuthoritativeDamage(PlayerDamageEvent evt)
        {
            if (evt == null || !evt.HasAuthoritativeState() ||
                !Guid.TryParse(evt.TargetID(), out var playerId) ||
                !TryGetPlayer(playerId, out var player) || player == null || player.Status == null)
            {
                return false;
            }

            var previousMaxHp = player.Status.MaxHp;
            var previousMaxArmor = player.Status.MaxArmor;
            if (evt.MaxHp() > 0) player.Status.MaxHp = evt.MaxHp();
            if (evt.MaxArmor() >= 0) player.Status.MaxArmor = evt.MaxArmor();

            var previousHp = GetPlayerHpSafe(player);
            var previousArmor = GetPlayerArmorSafe(player);
            var enteredDownState = player.ApplyAuthoritativeVitals(
                evt.RemainingHp(), evt.RemainingArmor(), evt.IsDown());
            var currentHp = GetPlayerHpSafe(player);
            var currentArmor = GetPlayerArmorSafe(player);

            if (!Mathf.Approximately(previousHp, currentHp) ||
                !Mathf.Approximately(previousMaxHp, player.Status.MaxHp))
            {
                OnPlayerHealthChanged?.Invoke(player, currentHp);
            }

            if (!Mathf.Approximately(previousArmor, currentArmor) ||
                !Mathf.Approximately(previousMaxArmor, player.Status.MaxArmor))
            {
                OnPlayerArmorChanged?.Invoke(player, currentArmor);
            }

            if (enteredDownState)
            {
                player.Status.DeathCount++;
                OnPlayerDied?.Invoke(player);
                if (ShouldPlayLocalPlayerSound(player))
                {
                    SoundManager.Instance.PlayPlayerSound(GetDeathSound(player));
                }
            }
            else if (!Mathf.Approximately(previousHp, currentHp) &&
                     ShouldPlayLocalPlayerSound(player))
            {
                SoundManager.Instance.PlayPlayerSound(GetDamageSound(player));
            }

            return true;
        }

        public bool ApplyAuthoritativeRespawn(PlayerRespawnEvent evt)
        {
            if (evt == null || !evt.HasAuthoritativeState() ||
                !Guid.TryParse(evt.PlayerID(), out var playerId) ||
                !TryGetPlayer(playerId, out var player) || player == null || player.Status == null)
            {
                return false;
            }

            var previousHp = GetPlayerHpSafe(player);
            var previousArmor = GetPlayerArmorSafe(player);
            var previousMaxHp = player.Status.MaxHp;
            var previousMaxArmor = player.Status.MaxArmor;
            player.ApplyAuthoritativeRespawn(
                new Vector3(evt.Position().x, evt.Position().y, player.transform.position.z),
                evt.CurrentHealth(), evt.MaxHealth(), evt.CurrentArmor(), evt.MaxArmor());

            var currentHp = GetPlayerHpSafe(player);
            var currentArmor = GetPlayerArmorSafe(player);
            if (!Mathf.Approximately(previousHp, currentHp) ||
                !Mathf.Approximately(previousMaxHp, player.Status.MaxHp))
            {
                OnPlayerHealthChanged?.Invoke(player, currentHp);
            }

            if (!Mathf.Approximately(previousArmor, currentArmor) ||
                !Mathf.Approximately(previousMaxArmor, player.Status.MaxArmor))
            {
                OnPlayerArmorChanged?.Invoke(player, currentArmor);
            }

            OnPlayerRespawned?.Invoke(player);
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
            RegisterPlayer(player);
            OnPlayerSpawned?.Invoke(player);
        }

        public void PublishPlayerRespawned(AbstractPlayer player)
        {
            if (player == null) return;
            OnPlayerRespawned?.Invoke(player);
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
