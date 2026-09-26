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

        private void OnValidate()
        {
            if (!float.IsFinite(maxFuel)) maxFuel = 2f;
            if (!float.IsFinite(fuelRecoverRate)) fuelRecoverRate = 0.5f;
            if (!float.IsFinite(boostAccel)) boostAccel = 10f;
            if (!float.IsFinite(maxBoostSpeed)) maxBoostSpeed = 5f;
            if (!float.IsFinite(groundedRecoveryDelay)) groundedRecoveryDelay = 0.5f;
            if (!float.IsFinite(gravityDuringBoost)) gravityDuringBoost = 3f;

            maxFuel = Mathf.Max(0.01f, maxFuel);
            fuelRecoverRate = Mathf.Max(0f, fuelRecoverRate);
            boostAccel = Mathf.Max(0f, boostAccel);
            maxBoostSpeed = Mathf.Max(0f, maxBoostSpeed);
            groundedRecoveryDelay = Mathf.Max(0f, groundedRecoveryDelay);
            gravityDuringBoost = Mathf.Max(0f, gravityDuringBoost);
        }

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
            if (!float.IsFinite(dt) || dt < 0f)
            {
                return 0f;
            }

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
            if (player == null || !float.IsFinite(dt) || dt < 0f)
            {
                return;
            }

            var currentTime = Time.time;
            if (!float.IsFinite(currentTime)) return;

            if (player.isGrounded && currentTime - groundedTime > groundedRecoveryDelay)
            {
                currentFuel = Mathf.Clamp(currentFuel + fuelRecoverRate * dt, 0f, maxFuel);
            }
        }

        private float ApplyBoost(float dt)
        {
            currentFuel = Mathf.Max(0f, currentFuel - dt);
            currentBoostSpeed = Mathf.Clamp(currentBoostSpeed + boostAccel * dt, 0f, maxBoostSpeed);
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
            var now = Time.time;
            groundedTime = float.IsFinite(now) && now >= 0f ? now : 0f;
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

        public float GetFuelRatio() => maxFuel > 0f ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;
        public bool IsOutOfFuel() => currentFuel <= 0f;
        public float CurrentFuel => currentFuel;
    }
}
