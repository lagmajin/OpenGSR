using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class AutoDelete : MonoBehaviour
    {
        [SerializeField] private float deleteTime = 1.0f;

        private void Awake()
        {
            deleteTime = Mathf.Max(0f, float.IsFinite(deleteTime) ? deleteTime : 1f);
        }

        // Start is called before the first frame update
        void Start()
        {
            Destroy(gameObject,deleteTime);
        }

    }


}
