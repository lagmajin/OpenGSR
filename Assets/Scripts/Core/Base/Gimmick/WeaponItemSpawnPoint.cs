
using System.Collections;
using UnityEngine;
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
                nextItem = eFieldItemType.FlameThrower;
            }

            if(generateType==eWeaponItemGenerateType.RocketLauncherFirst)
            {
                nextItem = eFieldItemType.RocketLauncher;
            }

            if (generateType == eWeaponItemGenerateType.Random)
            {
                nextItem = eFieldItemType.FlameThrower;
            }

            Debug.Log($"[WeaponItemSpawnPoint] Initial item: {FieldItemVisualResolver.GetDisplayName(nextItem ?? eFieldItemType.None)}");
            if (startImmidietry)
            {
                StartWorking();
            }
        }
        [Button("生成テスト")]
        public override void GenerateItem()
        {

           if(nextItem==eFieldItemType.FlameThrower)
            {

                if (gameObject.transform.childCount == 0)
                {
                    Debug.Log("[WeaponItemSpawnPoint] SpawnItem: FlameThrower");

                    var obj = Instantiate(FlameThrowerPrefab, gameObject.transform.position, Quaternion.identity);

                    obj.transform.parent = transform;

                    nextItem = eFieldItemType.RocketLauncher;
                }
            }
            
           if(nextItem==eFieldItemType.RocketLauncher)
            {
                if (gameObject.transform.childCount == 0)
                {
                    Debug.Log("[WeaponItemSpawnPoint] SpawnItem: RocketLauncher");

                    var obj = Instantiate(RocketLauncherPrefab, gameObject.transform.position, Quaternion.identity);

                    obj.transform.parent = transform;


                    nextItem = eFieldItemType.FlameThrower;

                }
            }




        }
    }

}
