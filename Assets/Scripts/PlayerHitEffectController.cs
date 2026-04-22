using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


namespace OpenGS
{
    public interface IPlayerEffect
    {
        void CreateHitEffect();
    }







    [DisallowMultipleComponent]
    public class PlayerHitEffectController : MonoBehaviour, IPlayerEffect
    {
        [SerializeField] [Required] public AbstractPlayer player;
        [SerializeField] [Required] [SceneObjectsOnly] public Transform transforom;
        [SerializeField] [Required] private GameObject hitEffect;





        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        [Button("エフェクト生成テスト")]
        public void CreateHitEffect()
        {
            if (hitEffect)
            {
                var effect = Instantiate(hitEffect);

                effect.transform.position = player.transform.position;


            }


        }



    }


}