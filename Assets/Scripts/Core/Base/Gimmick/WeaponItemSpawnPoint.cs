
using System.Collections;
using UnityEngine;
using OpenGSCore;
using Sirenix.OdinInspector;


namespace OpenGS
{
    public enum eWeaponItemGenerateType
    {
        RocketLauncherFirst,
        FlameThrowerFirst,
        Random
    }

    interface IWeaponItemSpawnPoint
    {

    }
    [DisallowMultipleComponent]
    class WeaponItemSpawnPoint:AbstractItemSpawnPoint
    {
        public eWeaponItemGenerateType generateType = eWeaponItemGenerateType.RocketLauncherFirst;

       


        public GameObject FlameThrowerPrefab;
        public GameObject RocketLauncherPrefab;

        void Start()
        {
            if(generateType==eWeaponItemGenerateType.FlameThrowerFirst)
            {
                nextItem = EFieldItemType.FlameThrower;
            }

            if(generateType==eWeaponItemGenerateType.RocketLauncherFirst)
            {
                nextItem = EFieldItemType.GranadeLauncher;
            }

            if (generateType == eWeaponItemGenerateType.Random)
            {
                nextItem = EFieldItemType.FlameThrower;
            }

            Debug.Log($"[WeaponItemSpawnPoint] Initial item: {WorldItemVisualResolver.GetDisplayName(nextItem ?? EFieldItemType.PowerUpItem)}");
            if (startImmidietry)
            {
                StartWorking();
            }
        }
        [Button("生成テスト")]
        public override void GenerateItem()
        {

           if(nextItem==EFieldItemType.FlameThrower)
            {

                if (gameObject.transform.childCount == 0)
                {
                    Debug.Log("[WeaponItemSpawnPoint] SpawnItem: FlameThrower");

                    if (FlameThrowerPrefab == null)
                    {
                        Debug.LogWarning($"[WeaponItemSpawnPoint] FlameThrowerPrefab is not assigned on {name}. Skipping spawn.", this);
                        return;
                    }

                    var obj = Instantiate(FlameThrowerPrefab, gameObject.transform.position, Quaternion.identity);

                    obj.transform.parent = transform;

                    nextItem = EFieldItemType.GranadeLauncher;
                }
            }
            
           if(nextItem==EFieldItemType.GranadeLauncher)
            {
                if (gameObject.transform.childCount == 0)
                {
                    Debug.Log("[WeaponItemSpawnPoint] SpawnItem: RocketLauncher");

                    if (RocketLauncherPrefab == null)
                    {
                        Debug.LogWarning($"[WeaponItemSpawnPoint] RocketLauncherPrefab is not assigned on {name}. Skipping spawn.", this);
                        return;
                    }

                    var obj = Instantiate(RocketLauncherPrefab, gameObject.transform.position, Quaternion.identity);

                    obj.transform.parent = transform;


                    nextItem = EFieldItemType.FlameThrower;

                }
            }




        }
    }

}
