
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{

    [CreateAssetMenu(menuName = "Effect/EffectPrefabMasterData")]
    public class EffectPrefabMasterData : ScriptableObject
    {
        [SerializeField][Required] public GameObject flagReturnEffect;
        

    }
}
