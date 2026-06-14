
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

        [SerializeField] [Required] public GameObject weaponPrefab;
        [SerializeField] private int storedMagazine = -1;
        [SerializeField] private bool isSpecialWeapon = false;
        [SerializeField] private int specialAmmo = 0;

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

            if (pickupableOnTime)
            {
                Invoke(nameof(EnablePickUp), Mathf.Max(0f, picupableDelay));
            }
            else
            {
                EnablePickUp();
            }

            if (time > 0f)
            {
                Invoke(nameof(DestroySelf), time);
            }
        }

        private void OnEnable()
        {
            if (!ActiveWeapons.Contains(this))
            {
                ActiveWeapons.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveWeapons.Remove(this);
        }

        private void Update()
        {

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

        private void EquipPlayer(IPlayer p)
        {
            if (!weaponPrefab)
            {
                Debug.Log("Error weapon");

                return;
            }

            if (isSpecialWeapon)
            {
                p.EquipSpecialWeapon(weaponPrefab, specialAmmo > 0 ? specialAmmo : storedMagazine);
            }
            else if (p.CanEquip())
            {
                p.EquipWeapon(weaponPrefab);

                if (storedMagazine >= 0 && p is AbstractPlayer matchedPlayer)
                {
                    matchedPlayer.SetCurrentWeaponMagazine(storedMagazine);
                }
            }
            else
            {
                return;
            }

            if (p is AbstractPlayer pickupPlayer)
            {
                pickupPlayer.TryPlayGeneralSound(EPlayerGeneralSound.TakeItem, 1f, 1f);
            }

            if (p is AbstractPlayer pickupActor)
            {
                SendWeaponPickupSync(pickupActor.UniqueID().ToString());
            }
            Destroy(gameObject);
        }

        public void SetStoredMagazine(int magazine)
        {
            storedMagazine = magazine;
        }

        public void SetSpecialWeaponAmmo(int ammo)
        {
            specialAmmo = ammo;
        }

        private void TryAutoPickup(AbstractPlayer player)
        {
            if (player == null)
            {
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

            if (isSpecialWeapon || player.CanEquip())
            {
                EquipPlayer(player);
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

        private bool TryResolvePlayer(Collider2D collision, out AbstractPlayer player)
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

            player = collision.GetComponentInParent<AbstractPlayer>();
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
            HandlePickupCollision(collision);
        }
    }


}
