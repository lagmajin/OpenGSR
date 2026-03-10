using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]

    public class FlagReturnParticle : MonoBehaviour
    {
        public ParticleSystem system;

        [SerializeField] private float time = 1.0f;
        // Start is called before the first frame update
        void Start()
        {

            Destroy(gameObject,time);

        }

        // Update is called once per frame
        void Update()
        {

        }
    }


}