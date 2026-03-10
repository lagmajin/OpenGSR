//using RuntimeScriptField;
using UnityEngine;

using OpenGSCore;


namespace OpenGS
{
    [CreateAssetMenu(menuName = "Master/ItemMasterData")]
    public class ItemMasterData : ScriptableObject
    {
        public new string name="";

        public EFieldItemType type;

        public float activeTime=30.0f;


        
        //public ComponentReference pickableItemScript;
        //public ComponentReference 


    }
}
