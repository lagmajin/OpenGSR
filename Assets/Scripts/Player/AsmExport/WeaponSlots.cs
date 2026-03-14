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

        public bool CanEquip()
        {
            // 特殊武器装備中は通常武器の拾得不可
            if (currentEquipType == EPlayerEquipWeapon.SpecialWeapon) return false;

            GameObject targetSlot = (currentEquipType == EPlayerEquipWeapon.MainWeapon) ? mainWeaponSlot : secondaryWeaponSlot;
            return targetSlot.transform.childCount == 0;
        }

        public void EquipWeapon(GameObject weaponPrefab)
        {
            if (currentEquipType == EPlayerEquipWeapon.SpecialWeapon) return;

            GameObject targetSlot = (currentEquipType == EPlayerEquipWeapon.MainWeapon) ? mainWeaponSlot : secondaryWeaponSlot;
            
            // 既存の武器があれば削除
            RemoveWeaponFromSlot(targetSlot);

            var weapon = Instantiate(weaponPrefab, targetSlot.transform);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            RefreshWeaponVisibility();
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
            if (currentWeaponObject == null) return null;
            return currentWeaponObject.GetComponent<AbstractGunController>();
        }
    }
}
