using System;
using System.Collections.Generic;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// Draws the enemies a mission has.
    /// <para>
    /// The enemies are the server's. It places them, steps them, decides what a
    /// shot did and says when one is gone. This only draws the ones it has been
    /// told about and puts them where it said they are, so a client's map is the
    /// map the mission is being played on rather than one it guessed at.
    /// </para>
    /// <para>
    /// An enemy here is a view, not a thing that can be hurt. Its health is the
    /// server's number and it is replaced on every message, so a client cannot
    /// walk an enemy's health down on its own and get credit for it.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class MissionEnemyViewManager : MonoBehaviour
    {
        /// <summary>
        /// Drawn for an enemy. It is a body, not a prefab, because the project has
        /// no enemy prefab and a mission with nothing on the map is not a mission.
        /// A prefab assigned here replaces it.
        /// </summary>
        [SerializeField] private GameObject enemyPrefab;

        [SerializeField] private int defaultMaxHealth = 60;

        [SerializeField] private float syncInterval = 0.2f;

        [SerializeField] private float moveSmoothingSeconds = 0.08f;

        private readonly Dictionary<string, EnemyView> views =
            new Dictionary<string, EnemyView>(StringComparer.OrdinalIgnoreCase);

        private readonly List<GameObject> spawnEffects = new List<GameObject>();

        private IDisposable enemySubscription;
        private IDisposable snapshotSubscription;
        private IDisposable damageSubscription;

        private float nextSyncAt;
        private bool warnedAboutPrefab;

        private sealed class EnemyView
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Collider2D Collider;
            public int Health;
            public int MaxHealth;
            public Vector2 Target;
            public Vector2 Facing;
        }

        private void OnEnable()
        {
            if (defaultMaxHealth < 1)
            {
                defaultMaxHealth = 60;
            }

            enemySubscription = GameEventBroker.Subscribe<EnemyEvent>(HandleEnemy);
            snapshotSubscription = GameEventBroker.Subscribe<EnemySnapshotEvent>(HandleSnapshot);
            damageSubscription = GameEventBroker.Subscribe<PlayerDamageEvent>(HandlePlayerDamaged);
        }

        private void OnDisable()
        {
            enemySubscription?.Dispose();
            snapshotSubscription?.Dispose();
            damageSubscription?.Dispose();
            enemySubscription = null;
            snapshotSubscription = null;
            damageSubscription = null;
        }

        /// <summary>
        /// Applies one report from the server.
        /// </summary>
        private void HandleEnemy(EnemyEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            switch (evt.Type())
            {
                case EnemyEvent.EEnemyEventType.Spawned:
                case EnemyEvent.EEnemyEventType.Synced:
                    SpawnOrMove(evt);
                    break;

                case EnemyEvent.EEnemyEventType.Damaged:
                    if (TryGet(evt.EnemyId(), out var damaged))
                    {
                        // The health is replaced, never reduced by what the client
                        // thinks the damage was. A client that subtracted its own
                        // number would show an enemy as nearly dead while the
                        // server still had it at full.
                        damaged.Health = evt.Health();
                        damaged.MaxHealth = evt.MaxHealth() > 0 ? evt.MaxHealth() : damaged.MaxHealth;
                        damaged.Target = evt.Position();
                        damaged.Facing = evt.Facing();
                        ApplyTint(damaged, evt.Health(), evt.MaxHealth());
                    }

                    break;

                case EnemyEvent.EEnemyEventType.Killed:
                    Remove(evt.EnemyId());
                    break;
            }
        }

        /// <summary>
        /// Draws every enemy the server has, for a client that arrived late and
        /// saw none of the spawns.
        /// </summary>
        private void HandleSnapshot(EnemySnapshotEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var enemy in evt.Enemies())
            {
                reported.Add(enemy.EnemyId());
                SpawnOrMove(enemy);
            }

            // Anything the snapshot did not mention is not in the mission any
            // more, however much this client thought it had.
            foreach (var id in new List<string>(views.Keys))
            {
                if (!reported.Contains(id))
                {
                    Remove(id);
                }
            }
        }

        /// <summary>
        /// A hit that came from an enemy rather than from another player.
        /// <para>
        /// It arrives under the same health ruling as every other hit, and the
        /// ruling does not say where it came from. Without this it is
        /// indistinguishable from a player, so a mission death looked like a
        /// duel.
        /// </para>
        /// </summary>
        private void HandlePlayerDamaged(PlayerDamageEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (PlayerRegistry.Instance == null ||
                !Guid.TryParse(evt.TargetID(), out var targetId) ||
                !PlayerRegistry.Instance.TryGetPlayer(targetId, out var player) ||
                player == null)
            {
                return;
            }

            // Nothing to do beyond the health the ruling already carried. The
            // handler exists so the case is known rather than silent, and so where
            // a sound for being shot by an enemy is wanted it is written once.
            Debug.Log(
                $"[Mission] {player.DisplayName()} was hit by an enemy for {evt.Damage()}.");
        }

        private void SpawnOrMove(EnemyEvent evt)
        {
            if (!TryGet(evt.EnemyId(), out var view))
            {
                view = Create(evt);
                if (view == null)
                {
                    return;
                }
            }

            view.Target = evt.Position();
            view.Facing = evt.Facing();
            view.Health = evt.Health();
            view.MaxHealth = evt.MaxHealth() > 0 ? evt.MaxHealth() : defaultMaxHealth;
            ApplyTint(view, view.Health, view.MaxHealth);
        }

        private EnemyView Create(EnemyEvent evt)
        {
            var template = enemyPrefab;
            if (template == null)
            {
                if (!warnedAboutPrefab)
                {
                    warnedAboutPrefab = true;
                    Debug.LogWarning(
                        "[MissionEnemyViewManager] No enemy prefab is assigned, so enemies " +
                        "are drawn as plain bodies. Assign one to give them a look.");
                }

                template = BuildFallbackBody();
            }

            if (template == null)
            {
                return null;
            }

            var instance = Instantiate(template, evt.Position(), Quaternion.identity);
            instance.name = $"Enemy_{evt.EnemyKind()}_{evt.EnemyId()}";
            instance.transform.SetParent(transform, worldPositionStays: true);

            var view = new EnemyView
            {
                Root = instance,
                Renderer = instance.GetComponentInChildren<SpriteRenderer>(includeInactive: true),
                Collider = instance.GetComponentInChildren<Collider2D>(includeInactive: true),
                Health = evt.Health(),
                MaxHealth = evt.MaxHealth() > 0 ? evt.MaxHealth() : defaultMaxHealth
            };

            // The body is not a player, so it is not picked up as one by anything
            // that looks for the shared player contract. A body that answered as a
            // player would be shot by friendly fire and be in the registry.
            RemovePlayerContract(instance);

            views[evt.EnemyId()] = view;
            return view;
        }

        private static GameObject BuildFallbackBody()
        {
            var body = new GameObject("EnemyBody");
            body.layer = LayerMask.NameToLayer("Default");

            var collider = body.AddComponent<CircleCollider2D>();
            collider.radius = 0.6f;
            collider.isTrigger = true;

            // No sprite is invented here. A body with no picture is a mistake the
            // inspector will not show, and a plain square reads as a placeholder
            // rather than as an enemy, which is what it is.
            return body;
        }

        private static void RemovePlayerContract(GameObject instance)
        {
            foreach (var contract in instance.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (contract is IBattlePlayer)
                {
                    Destroy(contract);
                }
            }
        }

        private void ApplyTint(EnemyView view, int health, int maxHealth)
        {
            if (view?.Renderer == null || maxHealth <= 0)
            {
                return;
            }

            // A wounded enemy is visibly wounded, from the server's own number. The
            // colour is the only thing that changes, so an enemy that has taken a
            // hit is not a different enemy.
            var ratio = Mathf.Clamp01(health / (float)maxHealth);
            view.Renderer.color = Color.Lerp(new Color(0.75f, 0.2f, 0.2f), Color.white, ratio);
        }

        private bool TryGet(string enemyId, out EnemyView view)
        {
            if (!string.IsNullOrWhiteSpace(enemyId) && views.TryGetValue(enemyId, out view) && view.Root != null)
            {
                return true;
            }

            view = null!;
            return false;
        }

        private void Remove(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId) || !views.TryGetValue(enemyId, out var view))
            {
                return;
            }

            views.Remove(enemyId);
            if (view.Root != null)
            {
                Destroy(view.Root);
            }
        }

        private void Update()
        {
            var now = Time.time;
            if (float.IsNaN(now) || now < 0f)
            {
                return;
            }

            // Positions are nudged towards where the server last said they were,
            // rather than snapped. The server reports on a clock and a client that
            // snapped to each report would put a visible jump on every enemy every
            // time one arrived.
            var step = moveSmoothingSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(Time.deltaTime / moveSmoothingSeconds);

            foreach (var view in views.Values)
            {
                if (view?.Root == null)
                {
                    continue;
                }

                var position = view.Root.transform.position;
                var target = (Vector3)view.Target;
                view.Root.transform.position = Vector3.Lerp(position, target, step);

                if (view.Facing.sqrMagnitude > 0.0001f && view.Renderer != null)
                {
                    view.Renderer.flipX = view.Facing.x < 0f;
                }
            }

            if (syncInterval <= 0f || now < nextSyncAt)
            {
                return;
            }

            nextSyncAt = now + syncInterval;
        }

        /// <summary>
        /// Drops every enemy, for a mission that has ended.
        /// </summary>
        public void Clear()
        {
            foreach (var view in views.Values)
            {
                if (view?.Root != null)
                {
                    Destroy(view.Root);
                }
            }

            views.Clear();
        }

        /// <summary>How many enemies are on this client's map right now.</summary>
        public int VisibleCount => views.Count;
    }
}