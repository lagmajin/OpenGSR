using System.Collections;
using System.Collections.Generic;
using OpenGSCore;
using Sirenix.OdinInspector;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class GrenadeDropper : MonoBehaviour
    {
        [SerializeField] private EGrenadeType type;

        [SerializeField] private AllGrenadeListMasterData data;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        [Button("グレネード投下")]
        public void DropGrenade()
        {
            var dat = data.dataList.Find(entry => entry != null && entry.Name == type.ToString());
            if (dat == null || dat.GrenadePrefab == null)
            {
                Debug.LogWarning($"[GrenadeDropper] Grenade prefab not found for {type}");
                return;
            }

            var obj=Instantiate(dat.GrenadePrefab);

            obj.transform.position = gameObject.transform.position;


        }
    }


}