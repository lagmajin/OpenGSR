
using Sirenix.OdinInspector;
using UnityEngine;



namespace OpenGS
{
    [CreateAssetMenu(menuName = "Flag/FlagMasterData")]
    public class FlagMasterData : ScriptableObject
    {
        [Required]public GameObject blueFlag;
        [Required]public GameObject redFlag;


        [Required]public GameObject blueFlagInSlot;
        [Required]public GameObject redFlagInSlot;



    }
}
