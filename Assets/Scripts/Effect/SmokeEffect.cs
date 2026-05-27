using System.Collections;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SmokeEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2.5f;

        private void Start()
        {
            Destroy(gameObject, Mathf.Max(0.1f, lifetime));
        }
    }
}
