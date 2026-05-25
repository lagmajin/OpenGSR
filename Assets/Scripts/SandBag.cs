using UnityEngine;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SandBag : MonoBehaviour
    {
        public float maxHealth = 100f;
        [SerializeField] private float hitDamage = 10f;
        private float currentHealth = 0.0f;

        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        void Start()
        {
            currentHealth = Mathf.Max(0f, maxHealth);
        }

        void Update()
        {
        }

        public void OnHit()
        {
            if (IsDead)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - hitDamage);
            ShowDamage();

            if (IsDead)
            {
                Die();
            }
        }

        public void ShowDamage()
        {
            Debug.Log($"[SandBag] hp={currentHealth}/{maxHealth}");
        }

        public void Die()
        {
            Debug.Log("[SandBag] destroyed");
            Destroy(gameObject);
        }
    }


}
