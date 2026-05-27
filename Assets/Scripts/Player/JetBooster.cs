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
        [SerializeField] private float gravityDuringBoost = 3f; // ブースト中の減衰
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
        private bool boostHeld;

        public void Activate(bool active)
        {
            boostHeld = active;
        }

        public void SetBoostHeld(bool active)
        {
            boostHeld = active;
        }

        public float StepBoost(float dt)
        {
            bool willActivate = boostHeld && currentFuel > 0f;

            if (willActivate && !isActive)
            {
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
                return ApplyBoost(dt);
            }
            ResetBoost();
            return 0f;
        }

        public void RecoverFuel(float dt)
        {
            if (player == null)
            {
                return;
            }

            if (player.isGrounded && Time.time - groundedTime > groundedRecoveryDelay)
            {
                currentFuel = Mathf.Min(currentFuel + fuelRecoverRate * dt, maxFuel);
            }
        }

        private float ApplyBoost(float dt)
        {
            currentFuel -= dt;
            currentBoostSpeed = Mathf.Min(currentBoostSpeed + boostAccel * dt, maxBoostSpeed);
            currentFuel = Mathf.Max(currentFuel, 0f);

            if (currentFuel <= 0f)
            {
                ResetBoost();
            }

            return Mathf.Max(0f, currentBoostSpeed - gravityDuringBoost);
        }

        private void ResetBoost()
        {
            currentBoostSpeed = 0f;
            isActive = false;
        }

        void Start()
        {
            currentFuel = maxFuel;
            LoadEquippedSettings();
            ApplyColor();
        }

        public void OnLanding()
        {
            currentBoostSpeed = 0f;
            isActive = false;
            groundedTime = Time.time;
        }

        private void LoadEquippedSettings()
        {
            string equippedId = UserSaveManager.GetEquippedId(EShopCategory.Booster);
            if (string.IsNullOrEmpty(equippedId)) return;

            ShopItemData data = null;
            if (shopMasterData != null)
            {
                data = shopMasterData.GetItemById(equippedId);
            }

            if (data == null)
            {
                data = ShopCatalogFactory.GetDefaultItemById(equippedId);
            }

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

        public float GetFuelRatio() => currentFuel / maxFuel;
        public bool IsOutOfFuel() => currentFuel <= 0f;
        public float CurrentFuel => currentFuel;
    }
}
