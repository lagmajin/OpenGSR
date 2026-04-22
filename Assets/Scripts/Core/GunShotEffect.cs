using UnityEngine;
using DG.Tweening;

namespace OpenGS
{
    interface IGunShotEffect
    {

    }

    [DisallowMultipleComponent]
    public class GunShotEffect:MonoBehaviour,IGunShotEffect
    {
        public float time = 0.5f;
        public GameObject fireEffect;

        private void Start()
        {
            Destroy(gameObject, time);
        }
        

        private void Update()
        {
            
        }


    }
}
