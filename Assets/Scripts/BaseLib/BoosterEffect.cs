

using UnityEngine;
using DG.Tweening;


namespace OpenGS
{
    interface IBoosterEffect
    {
        
    }
    
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class BoosterEffect:MonoBehaviour
    {
        public AudioClip startBoosterSound;
        public AudioClip booster;
        public AudioClip endBoosterSound;

        //public GameObject redBoosterEffectPrefab;
        //public GameObject blueBoosterEffectPrefab;
        

        public string boosterColorName = "";

        public float time = 0.0f;

        private void Start()
        {
             DOVirtual.DelayedCall(time, () =>
            {
                deleteThis();
            });

            if(""==boosterColorName)
            {
                boosterColorName = "Red";

                var resource = Resources.Load("");
            }

            if("Spark"==boosterColorName)
            {

            }

            if("Blue"==boosterColorName)
            {

            }

            if ("Green" == boosterColorName)
            {

            }

        }

        private void Update()
        {
            
        }

        private void deleteThis()
        {
            Destroy(gameObject);
        }

    }


}
