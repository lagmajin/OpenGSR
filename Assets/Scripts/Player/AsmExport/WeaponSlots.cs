using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WeaponSlots : MonoBehaviour
    {
        [Required] public GameObject mainWeaponSlot;
        [Required] public GameObject secondaryWeaponSlot;
        [Required] public GameObject specialWeaponSlot;
        public GameObject currentWeapon;

        public EPlayerEquipWeapon EquipWeaponType { get; set; } = EPlayerEquipWeapon.MainWeapon;
        private EPlayerEquipWeapon lastRegularWeapon = EPlayerEquipWeapon.MainWeapon; // 特殊武器の前に持っていた通常武器
        private int specialWeaponAmmo = 0;

        private void Start()
        {
            RefreshWeaponVisibility();
        }

        public void EquipSpecialWeapon(GameObject weaponPrefab, int ammo)
        {
            // すでに特殊武器を持っている場合は古いものを消去
            if (EquipWeaponType == EPlayerEquipWeapon.SpecialWeapon)
            {
                DropCurrentWeapon();
            }
            else
            {
                // 現在の通常武器を記憶
                lastRegularWeapon = EquipWeaponType;
            }

            EquipWeaponType = EPlayerEquipWeapon.SpecialWeapon;
            specialWeaponAmmo = ammo;

            var weapon = Instantiate(weaponPrefab, specialWeaponSlot.transform);
            weapon.transform.localPosition = Vector3.zero;
            currentWeapon = weapon;

            RefreshWeaponVisibility();
        }

        public void OnFireSpecialWeapon()
        {
            if (EquipWeaponType != EPlayerEquipWeapon.SpecialWeapon) return;

            specialWeaponAmmo--;
            if (specialWeaponAmmo <= 0)
            {
                // 弾切れ：特殊武器を捨てて元の武器に戻る
                DropCurrentWeapon();
                EquipWeaponType = lastRegularWeapon;
                RefreshWeaponVisibility();
            }
        }

        public bool CanEquip()
        {
            if (EquipWeaponType == EPlayerEquipWeapon.SpecialWeapon) return false; // 特殊武器装備中は取得不可（あるいは交換）

            if (EquipWeaponType == EPlayerEquipWeapon.MainWeapon)
            {
                return mainWeaponSlot.transform.childCount == 0;
            }
            else if (EquipWeaponType == EPlayerEquipWeapon.SecondaryWeapon)
            {
                return secondaryWeaponSlot.transform.childCount == 0;
            }
            return true;
        }

        public void EquipWeapon(GameObject weaponPrefab)
        {
            // 特殊武器装備中は通常武器の取得を制限（必要なら）
            if (EquipWeaponType == EPlayerEquipWeapon.SpecialWeapon) return;

            var weapon = Instantiate(weaponPrefab);

            if (EquipWeaponType == EPlayerEquipWeapon.MainWeapon)
            {
                weapon.transform.SetParent(mainWeaponSlot.transform);
                weapon.transform.position = mainWeaponSlot.transform.position;
                currentWeapon = weapon;
            }
            else
            {
                weapon.transform.SetParent(secondaryWeaponSlot.transform);
                weapon.transform.position = secondaryWeaponSlot.transform.position;
                currentWeapon = weapon;
            }
        }

        [Button("ドロップ")]
        public void DropCurrentWeapon()
        {
            if (currentWeapon != null)
            {
                Destroy(currentWeapon);
                currentWeapon = null;
            }
        }

        private void RefreshWeaponVisibility()
        {
            if (mainWeaponSlot != null) 
                mainWeaponSlot.SetActive(EquipWeaponType == EPlayerEquipWeapon.MainWeapon);
            
            if (secondaryWeaponSlot != null) 
                secondaryWeaponSlot.SetActive(EquipWeaponType == EPlayerEquipWeapon.SecondaryWeapon);

            if (specialWeaponSlot != null)
                specialWeaponSlot.SetActive(EquipWeaponType == EPlayerEquipWeapon.SpecialWeapon);

            // activeSlot を選択
            GameObject activeSlot = null;
            switch (EquipWeaponType)
            {
                case EPlayerEquipWeapon.MainWeapon: activeSlot = mainWeaponSlot; break;
                case EPlayerEquipWeapon.SecondaryWeapon: activeSlot = secondaryWeaponSlot; break;
                case EPlayerEquipWeapon.SpecialWeapon: activeSlot = specialWeaponSlot; break;
            }

            if (activeSlot != null && activeSlot.transform.childCount > 0)
            {
                currentWeapon = activeSlot.transform.GetChild(0).gameObject;
            }
            else
            {
                currentWeapon = null;
            }
        }

        [Button("武器切り替え")]
        public void FlipWeapon()
        {
            // 特殊武器装備中は切り替え不可
            if (EquipWeaponType == EPlayerEquipWeapon.SpecialWeapon)
            {
                Debug.Log("Cannot switch weapon while using Special Weapon!");
                return;
            }

            if (EquipWeaponType == EPlayerEquipWeapon.MainWeapon)
            {
                EquipWeaponType = EPlayerEquipWeapon.SecondaryWeapon;
            }
            else
            {
                EquipWeaponType = EPlayerEquipWeapon.MainWeapon;
            }
            RefreshWeaponVisibility();
        }

        public void FlipWeaponFirstWeapon(GameObject obj) { }
        public void FlipSecondaryWeapon(GameObject obj) { }
        public void FlipSpecialWeapon(GameObject obj) { }
    }
}
