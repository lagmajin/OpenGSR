using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class RocketLauncherItem : AbstractFieldItem
    {
        [SerializeField] private GameObject weaponPrefab; // 装備される武器のプレハブ
        [SerializeField] private int initialAmmo = 10;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var weaponSlots = collision.GetComponentInChildren<WeaponSlots>();
            if (weaponSlots != null)
            {
                // 特殊武器として装備
                weaponSlots.EquipSpecialWeapon(weaponPrefab, initialAmmo);
                
                // アイテム自体は削除
                Destroy(gameObject);
                
                Debug.Log($"[Item] RocketLauncher equipped. Ammo: {initialAmmo}");
            }
        }
    }

}
