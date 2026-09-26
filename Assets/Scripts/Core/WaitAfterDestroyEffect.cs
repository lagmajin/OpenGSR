using System.Collections;
using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class WaitAfterDestroyEffect : MonoBehaviour
    {
        public float waitTime = 0.0f;

        private void OnValidate()
        {
            if (!float.IsFinite(waitTime) || waitTime < 0f) waitTime = 0f;
        }

        private void Start()
        {
            StartCoroutine(Functions.WaitAfterAction(DestroyGameObject, waitTime));
               
        }

        private void Awake()
        {
            waitTime = float.IsFinite(waitTime) ? Mathf.Max(0f, waitTime) : 0f;
        }

        void DestroyGameObject()
        {
            Destroy(this.gameObject);
        }
    }
}
