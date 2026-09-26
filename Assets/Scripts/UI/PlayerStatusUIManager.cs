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
        private PlayerAgent myPlayerAgent;
        private PlayerStatus myStatus;
        private string myPlayerId;
        private GrenadeSlotImage[] grenadeSlotImages;
        private IDisposable ammoSub;
        private IDisposable weaponSub;
        private IDisposable killSub;
        private bool registrySubscribed;
        private PlayerRegistry subscribedRegistry;
        
        private PlayerGrenadeComponent grenadeComponent;
        [SerializeField] private float grenadeDisplayRefreshInterval = 0.1f;
        private float nextGrenadeDisplayRefreshTime;
        [SerializeField] private float playerResolveRetryInterval = 0.25f;
        private float nextPlayerResolveTime;

        private void OnValidate()
        {
            if (!float.IsFinite(grenadeDisplayRefreshInterval)) grenadeDisplayRefreshInterval = 0.1f;
            if (!float.IsFinite(playerResolveRetryInterval)) playerResolveRetryInterval = 0.25f;
            grenadeDisplayRefreshInterval = Mathf.Max(0.02f, grenadeDisplayRefreshInterval);
            playerResolveRetryInterval = Mathf.Max(0.05f, playerResolveRetryInterval);
        }

        private void Awake()
        {
            grenadeDisplayRefreshInterval = NormalizeInterval(grenadeDisplayRefreshInterval, 0.1f, 0.02f);
            playerResolveRetryInterval = NormalizeInterval(playerResolveRetryInterval, 0.25f, 0.05f);
            AutoBindHudReferences();
        }

        private void AutoBindHudReferences()
        {
            healthUI ??= new HealthUIElements();
            armorUI ??= new ArmorUIElements();
            boosterUI ??= new BoosterUIElements();
            weaponUI ??= new WeaponUIElements();

            var gauges = GetComponentsInChildren<Gauge>(true);
            foreach (var gauge in gauges)
            {
                if (gauge == null)
                {
                    continue;
                }

                switch (gauge.gameObject.name)
                {
                    case "HPGauge":
                        healthUI.hpGauge ??= gauge;
                        break;
                    case "BoostGauge":
                        boosterUI.boosterGauge ??= gauge;
                        break;
                    case "BombGauge":
                        weaponUI.grenadeChargeGauge ??= gauge;
                        break;
                }
            }

            if (weaponUI.weaponIcon == null)
            {
                foreach (var image in GetComponentsInChildren<Image>(true))
                {
                    if (image != null && image.gameObject.name == "WeaponImage")
                    {
                        weaponUI.weaponIcon = image;
                        break;
                    }
                }
            }

            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text == null)
                {
                    continue;
                }

                switch (text.gameObject.name)
                {
                    case "MagazineLabel(Bullet)":
                        weaponUI.ammoText ??= text;
                        break;
                    case "RemainsLabel":
                        weaponUI.grenadeCountText ??= text;
                        break;
                }
            }
        }

        private static float NormalizeInterval(float value, float fallback, float minimum)
        {
            return float.IsFinite(value) ? Mathf.Max(minimum, value) : fallback;
        }

        private void OnEnable()
        {
            TryBindRegistryEvents();

            // MessagePipe (GameEventBroker) 購読
            ammoSub = GameEventBroker.Subscribe<AmmoUpdateEvent>(HandleAmmoUpdate);
            weaponSub = GameEventBroker.Subscribe<WeaponChangeEvent>(HandleWeaponChange);

            TryFindMyPlayer();
        }

        private void OnDisable()
        {
            if (registrySubscribed && subscribedRegistry != null)
            {
                subscribedRegistry.OnPlayerHealthChanged -= HandleHealthChanged;
                subscribedRegistry.OnPlayerArmorChanged -= HandleArmorChanged;
                subscribedRegistry.OnPlayerBoosterChanged -= HandleBoosterChanged;
                subscribedRegistry.OnPlayerDied -= HandlePlayerDied;
                subscribedRegistry.OnPlayerRespawned -= HandlePlayerRespawned;
                subscribedRegistry.OnPlayerRegistered -= HandlePlayerRegistered;
            }
            registrySubscribed = false;
            subscribedRegistry = null;

            UnbindInstantItemEvents();
            UnbindGrenadeSlotEvents();

            ammoSub?.Dispose();
            weaponSub?.Dispose();
            killSub?.Dispose();
            ammoSub = null;
            weaponSub = null;
            killSub = null;
            myPlayer = null;
            myCharaPlayer = null;
            myPlayerAgent = null;
            myStatus = null;
            myPlayerId = null;
            grenadeComponent = null;
        }

        private void Update()
        {
            TryBindRegistryEvents();

            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f)
            {
                return;
            }

            if (myPlayerAgent == null && myCharaPlayer == null && now >= nextPlayerResolveTime)
            {
                nextPlayerResolveTime = now + Mathf.Max(0.05f, playerResolveRetryInterval);
                TryFindMyPlayer();
            }

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

            }

            if (now >= nextGrenadeDisplayRefreshTime)
            {
                nextGrenadeDisplayRefreshTime = now + Mathf.Max(0.02f, grenadeDisplayRefreshInterval);
                UpdateGrenadeDisplay();
                RefreshGrenadeSlotDisplays();
            }
        }

        private void TryBindRegistryEvents()
        {
            if (PlayerRegistry.Instance == null)
            {
                return;
            }

            if (registrySubscribed && subscribedRegistry == PlayerRegistry.Instance)
            {
                return;
            }

            if (registrySubscribed && subscribedRegistry != null)
            {
                subscribedRegistry.OnPlayerHealthChanged -= HandleHealthChanged;
                subscribedRegistry.OnPlayerArmorChanged -= HandleArmorChanged;
                subscribedRegistry.OnPlayerBoosterChanged -= HandleBoosterChanged;
                subscribedRegistry.OnPlayerDied -= HandlePlayerDied;
                subscribedRegistry.OnPlayerRespawned -= HandlePlayerRespawned;
                subscribedRegistry.OnPlayerRegistered -= HandlePlayerRegistered;
            }

            PlayerRegistry.Instance.OnPlayerHealthChanged += HandleHealthChanged;
            PlayerRegistry.Instance.OnPlayerArmorChanged += HandleArmorChanged;
            PlayerRegistry.Instance.OnPlayerBoosterChanged += HandleBoosterChanged;
            PlayerRegistry.Instance.OnPlayerDied += HandlePlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned += HandlePlayerRespawned;
            PlayerRegistry.Instance.OnPlayerRegistered += HandlePlayerRegistered;
            registrySubscribed = true;
            subscribedRegistry = PlayerRegistry.Instance;
        }

        private void TryFindMyPlayer()
        {
            if (PlayerRegistry.Instance == null)
            {
                foreach (var agent in FindObjectsByType<PlayerAgent>(FindObjectsSortMode.None))
                {
                    if (agent != null && agent.PlayerType() == EPlayerType.MyPlayer)
                    {
                        myPlayerAgent = agent;
                        return;
                    }
                }
                return;
            }

            foreach (var p in PlayerRegistry.Instance.GetAllPlayers())
            {
                if (p != null && p.PlayerType() == EPlayerType.MyPlayer)
                {
                    SetMyPlayer(p);
                    return;
                }
            }

            // Do not use an arbitrary PlayerAgent here: remote players may
            // be registered before the local player during scene startup.
            foreach (var agent in FindObjectsByType<PlayerAgent>(FindObjectsSortMode.None))
            {
                if (agent != null && agent.PlayerType() == EPlayerType.MyPlayer)
                {
                    myPlayerAgent = agent;
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
            if (player != null && player.PlayerType() == EPlayerType.MyPlayer)
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
            var grenadeCount = myPlayerAgent != null
                ? myPlayerAgent.GetGrenadeCount(grenadeType)
                : myPlayer?.Status?.GrenadeCount ?? 0;
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

                if (!slotImage.SyncFromPlayer)
                {
                    continue;
                }

                if (myCharaPlayer == null && myPlayerAgent == null)
                {
                    slotImage.Clear();
                    continue;
                }

                if (myPlayerAgent != null)
                {
                    slotImage.SetInstantItemType(index < myPlayerAgent.GetInstantItemSlotCount()
                        ? myPlayerAgent.GetInstantItemType(index)
                        : OpenGSCore.EInstantItemType.None);
                }
                else if (index < myCharaPlayer.GetInstantItemSlotCount())
                {
                    slotImage.SetInstantItemType(myCharaPlayer.GetInstantItemType(index));
                }
                else
                {
                    slotImage.Clear();
                }
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

                if (myPlayerAgent != null)
                {
                    slotImage.SetGrenadeType(myPlayerAgent.GetGrenadeSlotType(index));
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
