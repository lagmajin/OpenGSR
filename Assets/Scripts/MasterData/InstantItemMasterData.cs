
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using OpenGSCore;



namespace OpenGS
{

    [CreateAssetMenu(menuName = "Item/InstantItem")]
    public class InstantItemMasterData : ScriptableObject
    {
        [SerializeField] private EInstantItemType type;

        [SerializeField] private Image img;

        void Start()
        {

        }

        [Button("自動セット")]
        void AutoSet()
        {

        }

    }
}
