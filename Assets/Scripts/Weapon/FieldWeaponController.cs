
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;


#pragma warning disable 0414


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class FieldWeaponController : OpenGSBaseClass, IFieldWeaponController
    {
        private static readonly List<FieldWeaponController> ActiveWeapons = new();
        private int bulletCount = 0;


        private float time = 30.0f;

        private float picupableDelay = 1.0f;

        public bool pickupableOnTime = true;

        public bool pickupable = false;

        private Collider2D pickupCollider;
        private string reservedPlayerId = string.Empty;
        private bool suppressReservationSync = false;
        private bool lifecycleInitialized;

        [SerializeField] [Required] public GameObject weaponPrefab;
        [SerializeField] private int storedMagazine = -1;
        [SerializeField] private bool isSpecialWeapon = false;
        [SerializeField] private int specialAmmo = 0;

        private void OnValidate()
        {
            if (!float.IsFinite(time) || time < 0f) time = 30f;
            if (!float.IsFinite(picupableDelay) || picupableDelay < 0f) picupableDelay = 1f;
            storedMagazine = Mathf.Max(-1, storedMagazine);
            specialAmmo = Mathf.Max(0, specialAmmo);
        }

        private void Awake()
        {
            time = float.IsFinite(time) ? Mathf.Max(0f, time) : 30f;
            picupableDelay = float.IsFinite(picupableDelay) ? Mathf.Max(0f, picupableDelay) : 1f;
            storedMagazine = Mathf.Max(-1, storedMagazine);
            specialAmmo = Mathf.Max(0, specialAmmo);
        }

        //public Sound



        [SerializeField] [Required] public Rigidbody2D body;

        private void Start()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (body != null)
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            pickupCollider = GetComponent<Collider2D>();
            if (pickupCollider)
            {
                pickupCollider.enabled = false;
            }

            pickupable = false;

            lifecycleInitialized = true;
            ScheduleLifecycleInvokes();
        }

        private void OnEnable()
        {
            if (!ActiveWeapons.Contains(this))
            {
                ActiveWeapons.Add(this);
            }

            if (lifecycleInitialized)
            {
                ScheduleLifecycleInvokes();
            }
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(EnablePickUp));
            CancelInvoke(nameof(DestroySelf));
            ActiveWeapons.Remove(this);
        }

        private void ScheduleLifecycleInvokes()
        {
            CancelInvoke(nameof(EnablePickUp));
            CancelInvoke(nameof(DestroySelf));

            if (!pickupable)
            {
                if (pickupableOnTime)
                {
                    Invoke(nameof(EnablePickUp), picupableDelay);
                }
                else
                {
                    EnablePickUp();
                }
            }

            if (time > 0f)
            {
                Invoke(nameof(DestroySelf), time);
            }
        }

        public void EnablePickUp()
        {
            pickupable = true;
            if (pickupCollider)
            {
                pickupCollider.enabled = true;
            }
        }

        public void DisablePickUp()
        {
            pickupable = false;
            if (pickupCollider)
            {
                pickupCollider.enabled = false;
            }
        }

        private void ClearReservation()
        {
            reservedPlayerId = string.Empty;
        }

        private string ResolveWeaponType()
        {
            if (weaponPrefab != null)
            {
                return weaponPrefab.name;
            }

            return gameObject.name;
        }

        private Vector2 ResolveSyncPosition()
        {
            var position = transform.position;
            return new Vector2(position.x, position.y);
        }

        private string BuildSyncKey()
        {
            var position = ResolveSyncPosition();
            return $"{ResolveWeaponType()}|{position.x:F2}|{position.y:F2}";
        }

        public static bool TryFindMatchingWeapon(string weaponType, Vector2 position, out FieldWeaponController controller)
        {
            controller = null;

            if (!IsFinite(position) || string.IsNullOrWhiteSpace(weaponType))
            {
                return false;
            }

            var bestDistance = float.MaxValue;
            foreach (var weapon in ActiveWeapons)
            {
                if (weapon == null || !weapon.isActiveAndEnabled)
                {
                    continue;
                }

                if (!string.Equals(weapon.ResolveWeaponType(), weaponType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var weaponPosition = weapon.ResolveSyncPosition();
                if (!IsFinite(weaponPosition))
                {
                    continue;
                }
                var distance = Vector2.Distance(weaponPosition, position);
                if (distance > 0.5f)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    controller = weapon;
                }
            }

            return controller != null;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private void SendWeaponReservationSync(bool isReserved, string playerId)
        {
            if (suppressReservationSync)
            {
                return;
            }

            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                if (networkManager == null || !networkManager.IsConnected())
                {
                    return;
                }

                var weaponType = ResolveWeaponType();
                var position = ResolveSyncPosition();
                var weaponId = BuildSyncKey();
                JObject json = isReserved
                    ? RUDPMessageBuilder.CreateWeaponReserve(playerId, weaponId, weaponType, position)
                    : RUDPMessageBuilder.CreateWeaponRelease(playerId, weaponId, weaponType, position);
                networkManager.SendToServer(json);
            }
            catch
            {
                // Keep local pickup behavior working even if network sync is unavailable.
            }
        }

        private void SendWeaponPickupSync(string playerId)
        {
            if (suppressReservationSync)
            {
                return;
            }

            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                if (networkManager == null || !networkManager.IsConnected())
                {
                    return;
                }

                var weaponType = ResolveWeaponType();
                var position = ResolveSyncPosition();
                var weaponId = BuildSyncKey();
                networkManager.SendToServer(RUDPMessageBuilder.CreateWeaponPickup(playerId, weaponId, weaponType, position, false, playerId));
            }
            catch
            {
                // Local pickup should still succeed if the network layer is unavailable.
            }
        }

        private void DestroySelf()
        {
            CancelInvoke(nameof(EnablePickUp));
            CancelInvoke(nameof(DestroySelf));
            Destroy(gameObject);
        }

/// <summary>
        /// Puts the weapon into the player's hands by whichever equip API they
        /// carry.
        /// <para>
        /// There are two equip APIs in this project: the slot system the player
        /// prefabs actually use, and the older one on the other player tree. The
        /// pickup asked the older one and so found no player at all on a real
        /// match, while the tree that is used had a separate overload that skipped
        /// the reservation, the pickup report and the sound. One path now resolves
        /// whichever API the player has, so a weapon lying on the ground is
        /// picked up under the same rules by everybody.
        /// </para>
        /// </summary>
        /// <returns>False when the player had no room for it.</returns>
        private static bool TryEquip(
            IBattlePlayer player,
            GameObject prefab,
            bool special,
            int specialAmmoCount,
            int magazine)
        {
            if (player is PlayerAgent agent)
            {
                if (special)
                {
                    agent.EquipSpecialWeapon(prefab, specialAmmoCount);
                    return true;
                }

                if (!agent.CanEquipWeapon())
                {
                    return false;
                }

                agent.EquipWeapon(prefab);
                if (magazine >= 0)
                {
                    agent.SetCurrentWeaponMagazine(magazine);
                }

                return true;
            }

            if (player is not IPlayer legacy)
            {
                return false;
            }

            if (special)
            {
                legacy.EquipSpecialWeapon(prefab, specialAmmoCount);
                return true;
            }

            if (!legacy.CanEquip())
            {
                return false;
            }

            legacy.EquipWeapon(prefab);
            if (magazine >= 0 && legacy is AbstractPlayer abstractPlayer)
            {
                abstractPlayer.SetCurrentWeaponMagazine(magazine);
            }

            return true;
        }

        private static void TryPlayTakeItemSound(IBattlePlayer player)
        {
            if (player is PlayerAgent agent)
            {
                agent.TryPlayGeneralSound(EPlayerGeneralSound.TakeItem, 1f, 1f);
                return;
            }

            if (player is AbstractPlayer abstractPlayer)
            {
                abstractPlayer.TryPlayGeneralSound(EPlayerGeneralSound.TakeItem, 1f, 1f);
            }
        }

        public void SetStoredMagazine(int magazine)
        {
            storedMagazine = magazine;
        }

        public void SetSpecialWeaponAmmo(int ammo)
        {
            specialAmmo = ammo;
        }

        private void TryAutoPickup(IBattlePlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (!weaponPrefab)
            {
                Debug.LogWarning(
                    $"[{nameof(FieldWeaponController)}] Pickup ignored: weapon prefab is not assigned on {name}.");
                return;
            }

            if (string.IsNullOrWhiteSpace(reservedPlayerId))
            {
                reservedPlayerId = player.UniqueID().ToString();
            }

            if (!string.Equals(reservedPlayerId, player.UniqueID().ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // A special weapon replaces what is held rather than joining it, so
            // the only question is whether the player is standing on a weapon at
            // all; a regular one needs the player to have a free slot.
            if (TryEquip(player, weaponPrefab, isSpecialWeapon,
                    specialAmmo > 0 ? specialAmmo : storedMagazine, storedMagazine))
            {
                TryPlayTakeItemSound(player);
                SendWeaponPickupSync(player.UniqueID().ToString());
                Destroy(gameObject);
                return;
            }

            ClearReservation();
        }

        public void ApplyNetworkReservation(string playerId)
        {
            suppressReservationSync = true;
            reservedPlayerId = playerId ?? string.Empty;
            suppressReservationSync = false;
        }

        public void ApplyNetworkRelease(string playerId)
        {
            suppressReservationSync = true;
            if (string.IsNullOrWhiteSpace(playerId) || string.Equals(reservedPlayerId, playerId, StringComparison.OrdinalIgnoreCase))
            {
                ClearReservation();
            }
            suppressReservationSync = false;
        }

        public void ApplyNetworkPickup(string playerId, string weaponType, Vector2 position)
        {
            if (!string.Equals(ResolveWeaponType(), weaponType, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Vector2.Distance(ResolveSyncPosition(), position) > 0.5f)
            {
                return;
            }

            suppressReservationSync = true;
            Destroy(gameObject);
            suppressReservationSync = false;
        }

private bool TryResolvePlayer(Collider2D collision, out IBattlePlayer player)
        {
            player = null;

            if (collision == null)
            {
                return false;
            }

            var tags = collision.GetComponentInParent<IMultipleTags>();
            if (tags == null)
            {
                return false;
            }

            if (!tags.HasPlayerTag() && !tags.HasMyPlayerTag())
            {
                return false;
            }

            // Whichever tree carries the player. This asked for one type, which no
            // player prefab has, so nothing on the ground was ever picked up by
            // this path.
            player = collision.GetComponentInParent<IBattlePlayer>();
            return player != null;
        }

        private void HandlePickupCollision(Collider2D collision)
        {
            if (!pickupable)
            {
                return;
            }

            if (!TryResolvePlayer(collision, out var player))
            {
                return;
            }

            TryAutoPickup(player);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // One path for every player. This used to have a second, earlier
            // branch that bypassed the reservation, the pickup report and the
            // sound, so a weapon somebody else had laid claim to could simply be
            // walked into.
            HandlePickupCollision(collision);
        }
    }


}
