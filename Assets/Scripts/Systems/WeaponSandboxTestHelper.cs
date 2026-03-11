using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WeaponSandboxTestHelper : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private WeaponSlots weaponSlots;
        [SerializeField] private Transform spawnOrigin;

        [Header("Weapon Test")]
        [SerializeField] private bool infiniteAmmo = true;
        [SerializeField] private bool instantReloadOnKey = true;
        [SerializeField] private KeyCode reloadHotKey = KeyCode.R;
        [SerializeField] private int refillThreshold = 1;

        [Header("Dummy Spawn")]
        [SerializeField] private SandboxDummyEnemy sandboxDummyPrefab;
        [SerializeField] private KeyCode spawnDummyKey = KeyCode.F7;
        [SerializeField] private KeyCode clearDummyKey = KeyCode.F8;
        [SerializeField] private float spawnDistance = 7f;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 0f);

        private readonly List<SandboxDummyEnemy> spawnedDummies = new List<SandboxDummyEnemy>();

        private void Awake()
        {
            if (weaponSlots == null)
            {
                weaponSlots = GetComponentInChildren<WeaponSlots>();
            }

            if (spawnOrigin == null)
            {
                spawnOrigin = transform;
            }
        }

        private void Update()
        {
            ProcessWeaponAssist();
            ProcessDummySpawnInput();
        }

        private void ProcessWeaponAssist()
        {
            AbstractGunController gun = GetCurrentGun();
            if (gun == null)
            {
                return;
            }

            if (infiniteAmmo && gun.MagazineCount() <= Mathf.Max(0, refillThreshold))
            {
                gun.ReloadComplete();
            }

            if (instantReloadOnKey && Input.GetKeyDown(reloadHotKey))
            {
                gun.ReloadComplete();
            }
        }

        private void ProcessDummySpawnInput()
        {
            if (Input.GetKeyDown(spawnDummyKey))
            {
                SpawnDummy();
            }

            if (Input.GetKeyDown(clearDummyKey))
            {
                ClearSpawnedDummies();
            }
        }

        private void SpawnDummy()
        {
            if (sandboxDummyPrefab == null || spawnOrigin == null)
            {
                return;
            }

            Vector3 direction = GetAimDirection();
            Vector3 spawnPosition = spawnOrigin.position + (direction * Mathf.Max(0.1f, spawnDistance)) + spawnOffset;
            var dummy = Instantiate(sandboxDummyPrefab, spawnPosition, Quaternion.identity);
            spawnedDummies.Add(dummy);
        }

        private void ClearSpawnedDummies()
        {
            for (int i = 0; i < spawnedDummies.Count; i++)
            {
                if (spawnedDummies[i] != null)
                {
                    Destroy(spawnedDummies[i].gameObject);
                }
            }

            spawnedDummies.Clear();
        }

        private AbstractGunController GetCurrentGun()
        {
            if (weaponSlots == null || weaponSlots.currentWeapon == null)
            {
                return null;
            }

            return weaponSlots.currentWeapon.GetComponentInChildren<AbstractGunController>();
        }

        private Vector3 GetAimDirection()
        {
            if (Camera.main != null)
            {
                Vector3 mouse = Input.mousePosition;
                Vector3 world = Camera.main.ScreenToWorldPoint(mouse);
                world.z = spawnOrigin.position.z;

                Vector3 dir = (world - spawnOrigin.position);
                if (dir.sqrMagnitude > 0.0001f)
                {
                    return dir.normalized;
                }
            }

            return spawnOrigin.right.normalized;
        }
    }
}
