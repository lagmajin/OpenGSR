using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



namespace OpenGS
{


    [DisallowMultipleComponent]
    public class ShotTester : MonoBehaviour
    {
        [SerializeField] private bool auto = true;
        [SerializeField] private float autoInterval = 1.0f;
        [SerializeField][Required]private GameObject bullet;

        private float elapsed;

        private void Awake()
        {
            autoInterval = Mathf.Max(0.01f, float.IsFinite(autoInterval) ? autoInterval : 1f);
        }


        // Start is called before the first frame update
        void Start()
        {
            elapsed = 0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (!auto)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            elapsed = (float.IsFinite(elapsed) ? elapsed : 0f) + deltaTime;
            if (elapsed >= autoInterval)
            {
                elapsed = 0f;
                Test();
            }
        }

        [Button("" )]
        public void Test()
        {
            if (bullet == null)
            {
                Debug.LogWarning("[ShotTester] bullet prefab is not assigned.");
                return;
            }

            Instantiate(bullet, transform.position, transform.rotation);
        }
    }


}
