using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class RocketLauncherItem : WorldItem
    {
        [SerializeField] private GameObject weaponPrefab;
        [SerializeField] private int initialAmmo = 10;
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

            Debug.Log($"[Item] RocketLauncher equipped. Ammo: {initialAmmo}");
        }
    }
}
