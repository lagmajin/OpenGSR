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

        private void Awake()
        {
            lifetime = float.IsFinite(lifetime) ? Mathf.Max(0f, lifetime) : 60f;
            rotateSpeed = float.IsFinite(rotateSpeed) ? rotateSpeed : 120f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(lifetime) || lifetime < 0f) lifetime = 60f;
            if (!float.IsFinite(rotateSpeed)) rotateSpeed = 120f;
        }

        void Start()
        {
            var now = Time.time;
            spawnTime = float.IsFinite(now) && now >= 0f ? now : 0f;
        }


        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var now = Time.time;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f || !float.IsFinite(now))
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            transform.Rotate(0f, 0f, rotateSpeed * deltaTime);
            if (now - spawnTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }


    }
}
