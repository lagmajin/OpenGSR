using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class BulletImpactEffect : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

            Destroy(gameObject, 0.2f);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }


}