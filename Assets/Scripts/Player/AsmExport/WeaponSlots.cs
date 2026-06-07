using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using OpenGSCore;
using Zenject;

namespace OpenGS
{
    /// <summary>
    /// プレイヤーの武器スロットを管理するコンポーネント。
    /// Main, Secondary, Special の3つのスロットを持ち、表示・非表示を切り替える。
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponSlots : MonoBehaviour
    {
        [Header("Slot Containers")]
        [Required] public GameObject mainWeaponSlot;
        [Required] public GameObject secondaryWeaponSlot;
        [Required] public GameObject specialWeaponSlot;

        [Header("Current State")]
        [ReadOnly] public GameObject currentWeaponObject;
        [ReadOnly] public EPlayerEquipWeapon currentEquipType = EPlayerEquipWeapon.MainWeapon;

        // 互換性のためのエイリアス
        public GameObject currentWeapon => currentWeaponObject;

        private EPlayerEquipWeapon lastRegularWeapon = EPlayerEquipWeapon.MainWeapon;
        private int specialWeaponAmmo = 0;

        private void Start()
        {
            RefreshWeaponVisibility();
        }

        /// <summary>
        /// 特殊武器（火炎放射器やランチャーなど）を一時的に装備する
        /// </summary>
        public void EquipSpecialWeapon(GameObject weaponPrefab, int ammo)
        {
            if (weaponPrefab == null)
            {
                return;
            }

            if (currentEquipType == EPlayerEquipWeapon.SpecialWeapon)
            {
                RemoveWeaponFromSlot(specialWeaponSlot);
            }
            else
            {
                lastRegularWeapon = currentEquipType;
            }

            currentEquipType = EPlayerEquipWeapon.SpecialWeapon;
            specialWeaponAmmo = ammo;

            var weapon = Instantiate(weaponPrefab, specialWeaponSlot.transform);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            RefreshWeaponVisibility();
        }

        public void OnFireSpecialWeapon()
        {
            if (currentEquipType != EPlayerEquipWeapon.SpecialWeapon) return;

            specialWeaponAmmo--;
            if (specialWeaponAmmo <= 0)
            {
                RemoveWeaponFromSlot(specialWeaponSlot);
                currentEquipType = lastRegularWeapon;
                RefreshWeaponVisibility();
            }
        }

        public void ClearSpecialWeapon()
        {
            if (specialWeaponSlot == null)
            {
                return;
            }

            RemoveWeaponFromSlot(specialWeaponSlot);
            specialWeaponAmmo = 0;

            if (currentEquipType == EPlayerEquipWeapon.SpecialWeapon)
            {
                currentEquipType = lastRegularWeapon;
            }

            RefreshWeaponVisibility();
        }

        public bool CanEquip()
        {
            // 特殊武器装備中は通常武器の拾得不可
            if (currentEquipType == EPlayerEquipWeapon.SpecialWeapon) return false;

            GameObject targetSlot = (currentEquipType == EPlayerEquipWeapon.MainWeapon) ? mainWeaponSlot : secondaryWeaponSlot;
            if (targetSlot == null)
            {
                return false;
            }

            return targetSlot.transform.childCount == 0;
        }

        public bool HasAnyRegularWeapon()
        {
            return (mainWeaponSlot != null && mainWeaponSlot.transform.childCount > 0)
                || (secondaryWeaponSlot != null && secondaryWeaponSlot.transform.childCount > 0);
        }

        public void EquipWeapon(GameObject weaponPrefab)
        {
            if (weaponPrefab == null)
            {
                return;
            }

            if (currentEquipType == EPlayerEquipWeapon.SpecialWeapon) return;

            GameObject targetSlot = (currentEquipType == EPlayerEquipWeapon.MainWeapon) ? mainWeaponSlot : secondaryWeaponSlot;
            if (targetSlot == null)
            {
                return;
            }
            
            // 既存の武器があれば削除
            RemoveWeaponFromSlot(targetSlot);

            var weapon = Instantiate(weaponPrefab, targetSlot.transform);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            RefreshWeaponVisibility();
        }

        [Button("ドロップ")]
        public void DropCurrentWeapon()
        {
            if (currentWeaponObject != null)
            {
                var gun = currentWeaponObject.GetComponent<AbstractGunController>();
                var dropPrefab = gun != null ? gun.FieldPrefab() : null;
                if (gun != null && dropPrefab != null)
                {
                    var dropped = Instantiate(dropPrefab, currentWeaponObject.transform.position, currentWeaponObject.transform.rotation);
                    var fieldController = dropped.GetComponent<FieldWeaponController>();
                    if (fieldController != null)
                    {
                        fieldController.SetStoredMagazine(gun.CurrentMagazineCount());
                    }
                }

                Destroy(currentWeaponObject);
                currentWeaponObject = null;
                RefreshWeaponVisibility();
            }
        }

        private void RemoveWeaponFromSlot(GameObject slot)
        {
            foreach (Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void FlipWeapon()
        {
            if (currentEquipType == EPlayerEquipWeapon.SpecialWeapon) return;

            currentEquipType = (currentEquipType == EPlayerEquipWeapon.MainWeapon) 
                ? EPlayerEquipWeapon.SecondaryWeapon 
                : EPlayerEquipWeapon.MainWeapon;

            RefreshWeaponVisibility();

            var gun = GetCurrentGun();
            gun?.OnSwappedIn();
        }

        private void RefreshWeaponVisibility()
        {
            if (mainWeaponSlot) mainWeaponSlot.SetActive(currentEquipType == EPlayerEquipWeapon.MainWeapon);
            if (secondaryWeaponSlot) secondaryWeaponSlot.SetActive(currentEquipType == EPlayerEquipWeapon.SecondaryWeapon);
            if (specialWeaponSlot) specialWeaponSlot.SetActive(currentEquipType == EPlayerEquipWeapon.SpecialWeapon);

            GameObject activeSlot = null;
            switch (currentEquipType)
            {
                case EPlayerEquipWeapon.MainWeapon: activeSlot = mainWeaponSlot; break;
                case EPlayerEquipWeapon.SecondaryWeapon: activeSlot = secondaryWeaponSlot; break;
                case EPlayerEquipWeapon.SpecialWeapon: activeSlot = specialWeaponSlot; break;
            }

            if (activeSlot && activeSlot.transform.childCount > 0)
            {
                currentWeaponObject = activeSlot.transform.GetChild(0).gameObject;
            }
            else
            {
                currentWeaponObject = null;
            }
        }

        public AbstractGunController GetCurrentGun()
        {
            if (currentWeaponObject == null)
            {
                Debug.LogWarning($"[WeaponSlots] Current weapon object is null. equipType={currentEquipType}");
                return null;
            }

            var gun = currentWeaponObject.GetComponent<AbstractGunController>();
            if (gun == null)
            {
                Debug.LogWarning($"[WeaponSlots] Current weapon object has no AbstractGunController. weapon={currentWeaponObject.name}");
            }

            return gun;
        }
    }
}
