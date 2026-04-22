using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class UIManagerServiceProvider : MonoBehaviour
    {
        [SerializeField]
        [Required]
        [SceneObjectsOnly]
        private AbstractBattleSceneMediateObject battleSceneMediateObject;
        void Start()
        {

        }


        void Reset()
        {
            battleSceneMediateObject = GetComponent<AbstractBattleSceneMediateObject>();
        }


    }
}
