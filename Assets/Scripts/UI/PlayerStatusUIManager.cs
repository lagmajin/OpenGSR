using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UniRx;

namespace OpenGS
{
    [Serializable]
    public class HealthUIElements
    {
        [Header("Components")]
        public Gauge hpGauge;
        public TextMeshProUGUI hpText;
        public Image hpFillImage;

        [Header("Colors")]
        public Color healthyColor = new Color(0.2f, 1f, 0.2f);   // 緑
        public Color warningColor = new Color(1f, 0.8f, 0.2f);   // 黄
        public Color criticalColor = new Color(1f, 0.2f, 0.2f);   // 赤
    }

    [Serializable]
    public class BoosterUIElements
    {
        [Header("Components")]
        public Gauge boosterGauge;
        public TextMeshProUGUI boosterText;
        public Image boosterFillImage;
    }

    [Serializable]
    public class WeaponUIElements
    {
        [Header("Components")]
        public TextMeshProUGUI weaponNameText;
        public TextMeshProUGUI ammoText;
        public Image weaponIcon;
        public TextMeshProUGUI statusText; // キル数などの追加情報用

        [Header("Grenade")]
        public Image grenadeIcon;
        public TextMeshProUGUI grenadeTypeText;
        public Gauge grenadeChargeGauge; // グレネード溜めゲージ
        public TextMeshProUGUI grenadeCountText;
    }

    [Serializable]
    public class ArmorUIElements
    {
        [Header("Components")]
        public Gauge armorGauge;
        public TextMeshProUGUI armorText;
        public Image armorFillImage;

        [Header("Colors")]
        public Color armorColor = new Color(0.2f, 0.6f, 1f);   // 青系
    }

    /// <summary>
    /// 自プレイヤーのステータス（HP, ブースター, アーマー, 弾薬, 武器情報）を一括管理するUIマネージャー。
    /// PlayerRegistry および GameEventBroker からのイベントを購読して表示を更新する。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStatusUIManager : MonoBehaviour
    {
        [SerializeField] private HealthUIElements healthUI;
        [SerializeField] private ArmorUIElements armorUI;
        [SerializeField] private BoosterUIElements boosterUI;
        [SerializeField] private WeaponUIElements weaponUI;
        [SerializeField] private GameObject deathPanel;

        private AbstractPlayer myPlayer;
        private CharaController myCharaPlayer;
        private PlayerStatus myStatus;
        private string myPlayerId;
        private GrenadeSlotImage[] grenadeSlotImages;
        private IDisposable ammoSub;
        private IDisposable weaponSub;
        private IDisposable killSub;
        
        private PlayerGrenadeComponent grenadeComponent;

        private void OnEnable()
        {
            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.OnPlayerHealthChanged += HandleHealthChanged;
                PlayerRegistry.Instance.OnPlayerArmorChanged += HandleArmorChanged;
                PlayerRegistry.Instance.OnPlayerBoosterChanged += HandleBoosterChanged;
                PlayerRegistry.Instance.OnPlayerDied += HandlePlayerDied;
                PlayerRegistry.Instance.OnPlayerRespawned += HandlePlayerRespawned;
                PlayerRegistry.Instance.OnPlayerRegistered += HandlePlayerRegistered;
            }

            // MessagePipe (GameEventBroker) 購読
            ammoSub = GameEventBroker.Subscribe<AmmoUpdateEvent>(HandleAmmoUpdate);
            weaponSub = GameEventBroker.Subscribe<WeaponChangeEvent>(HandleWeaponChange);

            TryFindMyPlayer();
        }

        private void OnDisable()
        {
            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.OnPlayerHealthChanged -= HandleHealthChanged;
                PlayerRegistry.Instance.OnPlayerArmorChanged -= HandleArmorChanged;
                PlayerRegistry.Instance.OnPlayerBoosterChanged -= HandleBoosterChanged;
                PlayerRegistry.Instance.OnPlayerDied -= HandlePlayerDied;
                PlayerRegistry.Instance.OnPlayerRespawned -= HandlePlayerRespawned;
                PlayerRegistry.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
            }

            UnbindInstantItemEvents();
            UnbindGrenadeSlotEvents();

            ammoSub?.Dispose();
            weaponSub?.Dispose();
            killSub?.Dispose();
        }

        private void Update()
        {
            if (weaponUI != null)
            {
                // グレネードの溜め状態などは毎フレーム更新が必要な場合がある
                if (grenadeComponent != null && weaponUI.grenadeChargeGauge != null)
                {
                    float ratio = grenadeComponent.CurrentChargeRatio;
                    weaponUI.grenadeChargeGauge.UpdateGauge(ratio, 1.0f);

                    // 溜め中以外は非表示にするなどの演出も可能
                    weaponUI.grenadeChargeGauge.gameObject.SetActive(ratio > 0);
                }

                UpdateGrenadeDisplay();
            }

            RefreshGrenadeSlotDisplays();
        }

        private void TryFindMyPlayer()
        {
            if (PlayerRegistry.Instance == null) return;

            foreach (var p in PlayerRegistry.Instance.GetAllPlayers())
            {
                if (p != null && p.PlayerType() == EPlayerType.MyPlayer)
                {
                    SetMyPlayer(p);
                    return;
                }
            }
        }

        private void SetMyPlayer(AbstractPlayer player)
        {
            if (player == null)
            {
                return;
            }

            UnbindInstantItemEvents();
            UnbindGrenadeSlotEvents();

            myPlayer = player;
            myPlayerId = player.UniqueID().ToString();
            
            grenadeComponent = player.GetComponent<PlayerGrenadeComponent>();
            myCharaPlayer = player as CharaController;
            myStatus = player.Status;
            CacheGrenadeSlotImages();
            BindInstantItemEvents();
            BindGrenadeSlotEvents();

            // UniRx の KillCount を購読
            killSub?.Dispose();
            if (player.Status != null)
            {
                killSub = player.Status.KillCountStream.Subscribe(UpdateKillCountDisplay);
            }
            
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (myPlayer == null) return;

            // HP 初期化
            UpdateHPDisplay(myPlayer.GetHP(), myPlayer.GetMaxHP());

            // アーマー初期化
            UpdateArmorDisplay(myPlayer.GetArmor(), myPlayer.GetMaxArmor());
            
            // ブースター初期化
            UpdateBoosterDisplay(myPlayer.GetBooster(), myPlayer.GetMaxBooster());

            // 死亡パネル
            if (deathPanel != null) deathPanel.SetActive(myPlayer.IsDead());

            // 武器情報（初期状態の取得はポーリング or 初期化イベント待ち）
            UpdateWeaponFromCurrentPlayer();
            UpdateGrenadeDisplay();
            RefreshInstantItemDisplays();
            RefreshGrenadeSlotDisplays();
        }

        private void UpdateWeaponFromCurrentPlayer()
        {
            if (myPlayer == null) return;

            // WeaponSlots から現在の武器を取得して表示
            // IGunInfo を取得する
            var currentWeaponObj = myPlayer.gameObject.GetComponentInChildren<AbstractGunController>();
            if (currentWeaponObj != null)
            {
                var weaponName = currentWeaponObj.data != null
                    ? WeaponVisualResolver.GetDisplayName(currentWeaponObj.data.weaponType)
                    : currentWeaponObj.Name;
                var weaponIcon = currentWeaponObj.data != null
                    ? WeaponVisualResolver.GetInGameSprite(currentWeaponObj.data.weaponType)
                    : null;
                weaponIcon ??= currentWeaponObj.GunBigIcon();

                UpdateWeaponDisplay(weaponName, currentWeaponObj.MagazineCount(), currentWeaponObj.MagazineMaxCount(), weaponIcon);
            }
        }

        #region Event Handlers

        private void HandlePlayerRegistered(AbstractPlayer player)
        {
            if (player.PlayerType() == EPlayerType.MyPlayer)
            {
                SetMyPlayer(player);
            }
        }

        private void HandleHealthChanged(AbstractPlayer player, float newHp)
        {
            if (!IsMyPlayer(player)) return;
            UpdateHPDisplay(newHp, player.GetMaxHP());
        }

        private void HandleArmorChanged(AbstractPlayer player, float newArmor)
        {
            if (!IsMyPlayer(player)) return;
            UpdateArmorDisplay(newArmor, player.GetMaxArmor());
        }

        private void HandleBoosterChanged(AbstractPlayer player, float newBooster)
        {
            if (!IsMyPlayer(player)) return;
            UpdateBoosterDisplay(newBooster, player.GetMaxBooster());
        }

        private void HandlePlayerDied(AbstractPlayer player)
        {
            if (!IsMyPlayer(player)) return;
            
            if (deathPanel != null) deathPanel.SetActive(true);
            UpdateHPDisplay(0f, player.GetMaxHP());
        }

        private void HandlePlayerRespawned(AbstractPlayer player)
        {
            if (!IsMyPlayer(player)) return;

            if (deathPanel != null) deathPanel.SetActive(false);
            UpdateHPDisplay(player.GetHP(), player.GetMaxHP());
            UpdateBoosterDisplay(player.GetBooster(), player.GetMaxBooster());
            RefreshInstantItemDisplays();
            RefreshGrenadeSlotDisplays();
        }

        private void HandleInstantItemsChanged()
        {
            RefreshInstantItemDisplays();
        }

        private void HandleGrenadeSlotsChanged()
        {
            RefreshGrenadeSlotDisplays();
        }

        private void HandleAmmoUpdate(AmmoUpdateEvent evt)
        {
            if (evt.PlayerID() != myPlayerId) return;
            
            // 弾薬表示更新
            if (weaponUI.ammoText != null)
            {
                weaponUI.ammoText.text = $"{evt.CurrentAmmo()} / {evt.MaxAmmo()}";
            }
        }

        private void HandleWeaponChange(WeaponChangeEvent evt)
        {
            if (evt.PlayerID() != myPlayerId) return;

            // 武器名更新
            if (weaponUI.weaponNameText != null)
            {
                weaponUI.weaponNameText.text = evt.WeaponType();
            }
            
            // アイコンなどの更新が必要な場合は、ここで改めて IGunInfo を取得し直すなどの処理を検討
            // 今回は簡易的に更新
            UpdateWeaponFromCurrentPlayer();
        }

        #endregion

        #region UI Update Methods

        private void UpdateKillCountDisplay(int kills)
        {
            if (weaponUI.statusText != null)
            {
                weaponUI.statusText.text = $"KILLS: {kills}";
            }
        }

        private void UpdateHPDisplay(float current, float max)
        {
            if (healthUI.hpGauge != null)
            {
                healthUI.hpGauge.UpdateGauge(current, max);
            }

            if (healthUI.hpText != null)
            {
                healthUI.hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }

            if (healthUI.hpFillImage != null)
            {
                float ratio = current / max;
                if (ratio > 0.5f) healthUI.hpFillImage.color = healthUI.healthyColor;
                else if (ratio > 0.25f) healthUI.hpFillImage.color = healthUI.warningColor;
                else healthUI.hpFillImage.color = healthUI.criticalColor;
            }
        }

        private void UpdateArmorDisplay(float current, float max)
        {
            if (armorUI.armorGauge != null)
            {
                armorUI.armorGauge.UpdateGauge(current, max);
            }

            if (armorUI.armorText != null)
            {
                armorUI.armorText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }

            if (armorUI.armorFillImage != null)
            {
                armorUI.armorFillImage.color = armorUI.armorColor;
            }
        }

        private void UpdateBoosterDisplay(float current, float max)
        {
            if (boosterUI.boosterGauge != null)
            {
                boosterUI.boosterGauge.UpdateGauge(current, max);
            }

            if (boosterUI.boosterText != null)
            {
                boosterUI.boosterText.text = $"{Mathf.CeilToInt(current)}"; // ブースターは現在値のみ
            }
        }

        private void UpdateWeaponDisplay(string name, int currentAmmo, int maxAmmo, Sprite icon)
        {
            if (weaponUI.weaponNameText != null) weaponUI.weaponNameText.text = name;
            if (weaponUI.ammoText != null) weaponUI.ammoText.text = $"{currentAmmo} / {maxAmmo}";
            if (weaponUI.weaponIcon != null && icon != null)
            {
                weaponUI.weaponIcon.sprite = icon;
                weaponUI.weaponIcon.gameObject.SetActive(true);
            }
        }

        private void UpdateGrenadeDisplay()
        {
            if (weaponUI == null || grenadeComponent == null)
            {
                return;
            }

            var grenadeType = grenadeComponent.CurrentGrenadeType;
            var grenadeName = GrenadeVisualResolver.GetDisplayName(grenadeType);
            var grenadeCount = myPlayer?.Status?.GrenadeCount ?? 0;
            var grenadeIcon = GrenadeVisualResolver.GetPackHudSprite(grenadeType);

            if (weaponUI.grenadeTypeText != null)
            {
                weaponUI.grenadeTypeText.text = grenadeName;
            }

            if (weaponUI.grenadeCountText != null)
            {
                weaponUI.grenadeCountText.text = $"x{grenadeCount}";
            }

            if (weaponUI.grenadeIcon != null && grenadeIcon != null)
            {
                weaponUI.grenadeIcon.sprite = grenadeIcon;
                weaponUI.grenadeIcon.gameObject.SetActive(true);
            }
        }

        private void RefreshInstantItemDisplays()
        {
            var slotImages = GetComponentsInChildren<InstantItemSlotImage>(true);
            if (slotImages == null || slotImages.Length == 0)
            {
                return;
            }

            for (var index = 0; index < slotImages.Length; index++)
            {
                var slotImage = slotImages[index];
                if (slotImage == null)
                {
                    continue;
                }

                if (myCharaPlayer == null || index >= myCharaPlayer.GetInstantItemSlotCount())
                {
                    slotImage.Clear();
                    continue;
                }

                slotImage.SetInstantItemType(myCharaPlayer.GetInstantItemType(index));
            }
        }

        private void RefreshGrenadeSlotDisplays()
        {
            CacheGrenadeSlotImages();

            if (grenadeSlotImages == null || grenadeSlotImages.Length == 0)
            {
                return;
            }

            for (var index = 0; index < grenadeSlotImages.Length; index++)
            {
                var slotImage = grenadeSlotImages[index];
                if (slotImage == null)
                {
                    continue;
                }

                if (myStatus == null || index >= myStatus.GrenadeSlots.Count)
                {
                    slotImage.Clear();
                    continue;
                }

                slotImage.SetGrenadeType(myStatus.GetGrenadeSlot(index));
            }
        }

        private void CacheGrenadeSlotImages()
        {
            if (grenadeSlotImages != null && grenadeSlotImages.Length > 0)
            {
                return;
            }

            grenadeSlotImages = GetComponentsInChildren<GrenadeSlotImage>(true);
        }

        private void BindInstantItemEvents()
        {
            if (myCharaPlayer == null)
            {
                return;
            }

            myCharaPlayer.OnInstantItemsChanged -= HandleInstantItemsChanged;
            myCharaPlayer.OnInstantItemsChanged += HandleInstantItemsChanged;
        }

        private void UnbindInstantItemEvents()
        {
            if (myCharaPlayer == null)
            {
                return;
            }

            myCharaPlayer.OnInstantItemsChanged -= HandleInstantItemsChanged;
            myCharaPlayer = null;
        }

        private void BindGrenadeSlotEvents()
        {
            if (myStatus == null)
            {
                return;
            }

            myStatus.GrenadeSlotsChanged -= HandleGrenadeSlotsChanged;
            myStatus.GrenadeSlotsChanged += HandleGrenadeSlotsChanged;
        }

        private void UnbindGrenadeSlotEvents()
        {
            if (myStatus == null)
            {
                return;
            }

            myStatus.GrenadeSlotsChanged -= HandleGrenadeSlotsChanged;
            myStatus = null;
        }

        #endregion

        private bool IsMyPlayer(AbstractPlayer player)
        {
            return player != null && myPlayer != null && player.UniqueID() == myPlayer.UniqueID();
        }
    }
}
