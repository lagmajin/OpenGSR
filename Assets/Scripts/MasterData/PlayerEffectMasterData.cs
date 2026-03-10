using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace OpenGS
{
    // eEffectType enum moved to Interface/eEffectType.cs

    [CreateAssetMenu(menuName = "Effect/PlayerEffectMasterData")]
    public class PlayerEffectMasterData : ScriptableObject
    {
        [BoxGroup("ItemEffect")] [SerializeField] public GameObject TakePowerUpItemEffect;
        [BoxGroup("ItemEffect")] [SerializeField] public GameObject TakeDefenseUpItemEffect;
        [BoxGroup("ItemEffect")] [SerializeField] public GameObject TakeSpeedUpItemEffect;
        [BoxGroup("ItemEffect")] [SerializeField] public GameObject RedBoosterEffectPrefab;
        [BoxGroup("ItemEffect")] [SerializeField] public GameObject GreenBoosterEffectPrefab;
        [BoxGroup("ItemEffect")] [SerializeField] public GameObject BlueBoosterEffectPrefab;

        [BoxGroup("ItemEffect")] [SerializeField] public GameObject BoosterSparkEffectPrefab;

        [BoxGroup("BattleEffect")] [SerializeField] public GameObject HitEffect;


#if UNITY_EDITOR
        private void Set(in string name, ref GameObject obj)
        {
            var sequence = "t:Prefab " + name;

            var guids = AssetDatabase.FindAssets(sequence);

            foreach (var id in guids)
            {
                var asset = AssetDatabase.GUIDToAssetPath(id);




            }

        }

        [Button("")]
        public void AutoSet()
        {
            Set("TakePowerUpItemEffect", ref TakePowerUpItemEffect);
            Set("TakeDefenseUpItemEffect", ref TakeDefenseUpItemEffect);
        }

#endif
    }
}
