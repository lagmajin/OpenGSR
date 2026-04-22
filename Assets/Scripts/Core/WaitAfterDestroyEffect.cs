using System.Collections;
using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WaitAfterDestroyEffect : MonoBehaviour
    {
        public float waitTime = 0.0f;

        private void Start()
        {
            StartCoroutine(Functions.WaitAfterAction(DestroyGameObject, waitTime));
               
        }

        private void Update()
        {

        }

        void DestroyGameObject()
        {
            Destroy(this.gameObject);
        }
    }
}
