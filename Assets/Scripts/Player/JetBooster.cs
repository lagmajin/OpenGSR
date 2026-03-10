using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class JetBooster : MonoBehaviour
    {
        [SerializeField] private float maxFuel = 2.0f;
        [SerializeField] private float fuelRecoverRate = 0.5f;
        [SerializeField] private float boostAccel = 10f;
        [SerializeField] private float maxBoostSpeed = 5f;
        [SerializeField] private float groundedRecoveryDelay = 0.5f; // 0
        [SerializeField] private float gravity = 10f;         // 通常重力
        [SerializeField] private float gravityDuringBoost = 3f; // ブースト中の

        [Header("Visual Settings")]
        [SerializeField] private Color boostColor = Color.cyan;
        [SerializeField] private SpriteRenderer boosterRenderer;
        [SerializeField] private ParticleSystem boostParticles;

        [Header("Master Data (Optional)")]
        [SerializeField] private ShopMasterData shopMasterData;

        [ShowInInspector]private float currentFuel;
        private float currentBoostSpeed;
        private bool isActive=false;

        //private bool isGrounded;
        private float groundedTime;
        private float verticalSpeed;

        //[SerializeField] private PlayerAgent agent;

        [SerializeField]private PlayerAgent player;
        public void Activate(bool active)
        {
            // Determine whether boost should be active (input + fuel)
            bool willActivate = active && currentFuel > 0f;

            // If we start boosting this frame and were grounded, give a small upward impulse
            if (willActivate && !isActive)
            {
                Debug.Log("🔥 Jet Boost Activated (initial burst)");
                if (player != null && player.isGrounded)
                {
                    player.verticalSpeed = Mathf.Max(player.verticalSpeed, 2f);
                    player.isGrounded = false;
                }
            }

            isActive = willActivate;

            if (isActive)
            {
                currentBoostSpeed = Mathf.Max(currentBoostSpeed, 2f);
            }
        }

        void Start()
        {
            currentFuel = maxFuel;
            LoadEquippedSettings();
            ApplyColor();
        }

        private void LoadEquippedSettings()
        {
            if (shopMasterData == null) return;

            string equippedId = UserSaveManager.GetEquippedId(EShopCategory.Booster);
            if (string.IsNullOrEmpty(equippedId)) return;

            var data = shopMasterData.GetItemById(equippedId);
            if (data != null)
            {
                boostColor = data.itemColor;
            }
        }

        public void SetColor(Color color)
        {
            boostColor = color;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (boosterRenderer != null) boosterRenderer.color = boostColor;
            if (boostParticles != null)
            {
                var main = boostParticles.main;
                main.startColor = boostColor;
            }
        }

        void Update()
        {
            Activate(Input.GetMouseButton(1));
            float dt = Time.deltaTime;

            if (isActive && currentFuel > 0f)
            {
                ApplyBoost(dt);
                if (currentFuel <= 0f)
                {
                    // out of fuel, stop boosting
                    isActive = false;
                    ResetBoost();
                }
            }
            else
            {
                ResetBoost();
            }

            RecoverFuelIfGrounded();

            currentFuel = Mathf.Max(currentFuel, 0f);
        }

        void ApplyBoost(float dt)
        {
            currentFuel -= dt;
            currentBoostSpeed = Mathf.Min(currentBoostSpeed + boostAccel * dt, maxBoostSpeed);
            player.verticalSpeed += currentBoostSpeed * dt;
            // Optionally:
            // player.verticalSpeed -= gravityDuringBoost * dt;
        }

        void ResetBoost()
        {
            currentBoostSpeed = 0f;
            isActive = false;
        }

        void RecoverFuelIfGrounded()
        {
            if (player.isGrounded && Time.time - groundedTime > groundedRecoveryDelay)
            {
                currentFuel = Mathf.Min(currentFuel + fuelRecoverRate * Time.deltaTime, maxFuel);
            }
        }

        public void OnLanding()
        {
            player.verticalSpeed = 0f;
            currentBoostSpeed = 0f;
            player.isGrounded = true;
            groundedTime = Time.time;

        }

        public float GetFuelRatio() => currentFuel / maxFuel;
        public bool IsOutOfFuel() => currentFuel <= 0f;
        public float CurrentFuel => currentFuel;
    }
}