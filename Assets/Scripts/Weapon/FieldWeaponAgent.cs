using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class FieldWeaponAgent:MonoBehaviour
    {
        [SerializeField] private float lifetime = 60f;
        [SerializeField] private float rotateSpeed = 120f;
        private float spawnTime;

        void Start()
        {
            spawnTime = Time.time;
        }


        private void Update()
        {
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
            if (Time.time - spawnTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }


    }
}
