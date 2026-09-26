
using UnityEngine;

namespace OpenGS
{
    public class FlameThrower : WorldItem
    {
        [SerializeField] private GameObject weaponPrefab; // 装備される武器のプレハブ
        [SerializeField] private int initialAmmo = 100;
        private bool consumed;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (consumed || collision == null || weaponPrefab == null)
            {
                return;
            }

            var weaponSlots = collision.GetComponentInParent<WeaponSlots>();
            if (weaponSlots == null)
            {
                return;
            }

            consumed = true;
            weaponSlots.EquipSpecialWeapon(weaponPrefab, Mathf.Max(0, initialAmmo));
            Destroy(gameObject);
        }
    }



}
