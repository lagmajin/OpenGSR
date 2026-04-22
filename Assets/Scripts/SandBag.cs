using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SandBag : MonoBehaviour
    {
        public float maxHealth = 100f;  // サンドバックの最大HP
        private float currentHealth=0.0f;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnHit()
        {
           
        }

        public void ShowDamage()
        {

        }

        public void Die()
        {
            Destroy(gameObject);
        }
    }


}