using OpenGSCore;
using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using Sirenix.OdinInspector;
using Zenject;

namespace OpenGS
{
    /// <summary>
    /// 特殊武器の種類
    /// </summary>
    public enum ESpecialWeapon
    {
        FlameThrower,
        GrenadeLauncher
    }

    /// <summary>
    /// プレイヤーの基底クラス。
    /// IPowerupable / IDamageable / IPlayer / IMovable / IEventActor を実装する。
    /// HP・Booster は PlayerStatus に一元管理する。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public abstract class AbstractPlayer : MonoBehaviour, IPowerupable, IDamageable, IPlayer, IMovable, IEventActor, OpenGS.Network.INetworkTransform
    {
        // ─── Inspector フィールド ────────────────────────────────────

        [SerializeField] private PlayerInput input;
        [SerializeField] private GroundCheck check;

        [SerializeField] [Required] protected EPlayerCharacter character;

        [SerializeField] public Guid uniqueId = Guid.NewGuid();

        [SerializeField] public Animator animator;

        [SerializeField] [BoxGroup("Status")] private float interval = 0.1f;
        [SerializeField] [BoxGroup("Status")] private float lavaDamageInterval = 1.2f;
        [SerializeField] [BoxGroup("Status")] private float lavaDamageCounter = 0.0f;
        [SerializeField] [BoxGroup("Status")] private float warpCounter = 0.0f;
        [SerializeField] [BoxGroup("Status")] private float fallDeathY = -80f;

        [SerializeField] [BoxGroup("Health")] protected float damageInvincibleTime = 0.2f;
        [SerializeField] [BoxGroup("Health")] protected float elementalDamageTickInterval = 1.0f;
        [SerializeField] [BoxGroup("Health")] protected float poisonDamagePerTick = 2.0f;
        [SerializeField] [BoxGroup("Health")] protected float fireDamagePerTick = 4.0f;

        [SerializeField] public AnimationCurve dashCurve;
        [SerializeField] public AnimationCurve jumpCurve;

        [SerializeField] [BoxGroup("MasterData")] [Required] public PlayerGeneralSoundMasterData GeneralSoundMasterData;
        [SerializeField] [BoxGroup("MasterData")] [Required] protected EffectPrefabMasterData EffectPrefabMasterData;
        [SerializeField] [BoxGroup("MasterData")] [Required] protected PlayerEffectMasterData PlayerEffectPrefabMasterData;
        [SerializeField] [BoxGroup("MasterData")] [Required] protected AllGrenadeListMasterData GrenadeMasterDataList;

        [SerializeField] protected new Rigidbody2D rigidbody2D;
        [SerializeField] [Required] protected WeaponSlots weaponSlots;

        [SerializeField] public GameObject Hand;

        [SerializeField] private EPlayerType playerType = EPlayerType.Unknown;

        // ─── 状態フィールド ─────────────────────────────────────────

        public float moveSpeed = 0.4f;
        protected float baseMoveSpeed = 0.4f;
        protected float attackMultiplier = 1f;
        protected float defenseMultiplier = 1f;
        protected float moveSpeedMultiplier = 1f;
        protected const float BuffedMultiplier = 2f;
        protected const float InvisibleAlpha = 0.3f;

        protected bool isDead = false;
        protected bool isJump = false;
        protected bool isSitting = false;
        protected bool isLyingDown = false;
        protected bool invisible = false;
        protected bool isInvincible = false;
        protected float jumpPos = 0.0f;
        protected float jumpInterval = 10.0f;
        protected bool canEquip = false;
        protected int dashCount = 0;

        private float warpDelayCounter;
        private float increaseItemCounter;
        private Coroutine reSpawnCoroutine;
        private bool hasTeam;
        private ETeam myTeam;
        private bool canWarp;
        private bool canJump;
        private bool isSpectatorMode;
        private float cachedBaseMaxHp = -1f;
        private MultipleTags myTags;
        private IEffectService effectService;
        private float standingMoveSpeedCache;
        private bool cachedStandingMoveSpeed;
        private float cachedCameraZoomScale = 1f;
        private float cachedProneCameraZoomScale = 1f;
        private AbstractMatchMainScript cachedMatchScene;
        private IDisposable poseEventSubscription;
        private Coroutine elementalDamageCoroutine;
        private Coroutine lavaDamageCoroutine;

        protected virtual void OnValidate()
        {
            interval = SafeNonNegative(interval, 0.1f);
            lavaDamageInterval = SafeNonNegative(lavaDamageInterval, 1.2f);
            lavaDamageCounter = SafeNonNegative(lavaDamageCounter, 0f);
            warpCounter = SafeNonNegative(warpCounter, 0f);
            damageInvincibleTime = SafeNonNegative(damageInvincibleTime, 0.2f);
            elementalDamageTickInterval = Mathf.Max(0.05f, SafeNonNegative(elementalDamageTickInterval, 1f));
            poisonDamagePerTick = SafeNonNegative(poisonDamagePerTick, 2f);
            fireDamagePerTick = SafeNonNegative(fireDamagePerTick, 4f);
            moveSpeed = SafeNonNegative(moveSpeed, 0.4f);
            if (!float.IsFinite(fallDeathY)) fallDeathY = -80f;
        }

        private static float SafeNonNegative(float value, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Max(0f, value) : fallback;
        }

        protected SpriteRenderer spriteRenderer;

        private int attackBuffVersion;
        private int defenseBuffVersion;
        private int speedBuffVersion;
        private int invisibleBuffVersion;
        private int invincibilityVersion;

        // ─── PlayerStatus (HP・Booster を一元管理) ──────────────────

        public PlayerStatus Status { get; set; } = new PlayerStatus();

        public uint NetworkId => StableNetworkId(UniqueID().ToString());
        public Vector3 Position { get => transform.position; set => transform.position = value; }
        public Quaternion Rotation { get => transform.rotation; set => transform.rotation = value; }
        public Vector3 Velocity => rigidbody2D != null ? (Vector3)rigidbody2D.linearVelocity : Vector3.zero;
        public string OwnerPlayerId => UniqueID().ToString();

        // ─── IPlayer: ライフサイクル ─────────────────────────────────
        private bool hasEnemyFlag = false;
        private FlagController carriedEnemyFlag = null;

        public virtual void OnDead()
        {
            if (hasEnemyFlag)
            {
                hasEnemyFlag = false;
                carriedEnemyFlag?.OnDropped();
                carriedEnemyFlag = null;
            }

            weaponSlots?.ClearSpecialWeapon();

            PlayDeathAnimation();

            if (PlayerType() == EPlayerType.MyPlayer && MatchModeResolver.CanRespawnCurrentMatch() == false)
            {
                EnterSpectatorMode();
            }
        }

        public virtual void OnBurst()
        {
            if (hasEnemyFlag)
            {
                hasEnemyFlag = false;
                carriedEnemyFlag?.OnDropped();
                carriedEnemyFlag = null;
            }

            PlayDeathAnimation();
        }

        public virtual void OnSpawn()
        {
            isDead = false;
            isInvincible = false;
            invincibilityVersion++;
            ResetPowerupState();
            Status?.FullRecovery(); // Recover HP, Booster, Grenades
            Status?.FullCombatRecovery();
            ApplyMatchModeInitialStats();
            ExitSpectatorMode();
            canJump = true;
            canWarp = true;
            isSitting = false;
            isLyingDown = false;

            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.PublishPlayerSpawned(this);
            }
        }

        public virtual void OnReSpawn()
        {
            if (!MatchModeResolver.CanRespawnCurrentMatch())
            {
                EnterSpectatorMode();
                return;
            }

            isDead = false;
            isInvincible = false;
            invincibilityVersion++;
            ResetPowerupState();
            Status?.FullRecovery(); // Recover HP, Booster, Grenades
            Status?.FullCombatRecovery();
            ApplyMatchModeInitialStats();
            ExitSpectatorMode();
            canJump = true;
            canWarp = true;
            isSitting = false;
            isLyingDown = false;

            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.PublishPlayerRespawned(this);
            }

            GameEventBroker.Publish(new PlayerRespawnEvent(UniqueID().ToString(), (Vector2)transform.position));
        }

        public virtual void ReserveReSpawn(float delay)
        {
            if (!MatchModeResolver.CanRespawnCurrentMatch())
            {
                EnterSpectatorMode();
                return;
            }

            if (reSpawnCoroutine != null)
            {
                StopCoroutine(reSpawnCoroutine);
            }

            reSpawnCoroutine = StartCoroutine(ReserveReSpawnRoutine(Mathf.Max(0f, delay)));
        }

        // ─── IPlayer: 状態クエリ ─────────────────────────────────────

        public bool IsGround() => check != null && check.IsGround;

        public bool IsDead() => isDead;

        public bool IsRolling()
        {
            var dashAndRolling = GetComponent<DashAndRolling>();
            return dashAndRolling != null && dashAndRolling.IsRollPressed;
        }

        public Guid UniqueID() => uniqueId;

        public void SetUniqueID(Guid id)
        {
            var previousId = UniqueID().ToString();
            var idChanged = uniqueId != id;

            if (idChanged && PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.UnregisterPlayer(uniqueId);
            }

            uniqueId = id;

            if (idChanged && PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.RegisterPlayer(this);
            }

            if (OpenGS.Network.LagCompensationManager.Instance != null)
            {
                OpenGS.Network.LagCompensationManager.Instance.UnregisterNetworkObject(previousId);
                OpenGS.Network.LagCompensationManager.Instance.RegisterNetworkObject(this);
            }
        }

        // ─── IPlayer: チーム ─────────────────────────────────────────

        public bool HasTeam() => hasTeam;

        public ETeam Team() => myTeam;

        public void SetTeam(ETeam team)
        {
            myTeam = team;
            hasTeam = team != ETeam.NoTeam;
        }

        // ─── IPlayer: フラッグ ──────────────────────────────────────

        public bool HasEnemyFlag() => hasEnemyFlag;

        public void EnemyFlagCaptured()
        {
            hasEnemyFlag = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{character} captured enemy flag!");
#endif
        }

        public void BindEnemyFlag(FlagController flag)
        {
            carriedEnemyFlag = flag;
        }

        public void EnemyFlagReturnedToBase()
        {
            EnemyFlagReturnedToBase(false);
        }

        public void EnemyFlagReturnedToBase(bool fromCapture)
        {
            hasEnemyFlag = false;
            if (carriedEnemyFlag != null)
            {
                var reason = fromCapture
                    ? FlagController.EFlagReturnReason.CapturedAtBase
                    : FlagController.EFlagReturnReason.FriendlyRecovered;
                carriedEnemyFlag.ReturnToBase(null, reason);
            }
            carriedEnemyFlag = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{character} delivered enemy flag to base!");
#endif
        }

        // ─── IPlayer: 装備 ──────────────────────────────────────────

        public bool CanEquip() => weaponSlots != null && weaponSlots.CanEquip();

        public bool HasAnyWeapon() => weaponSlots != null && weaponSlots.HasAnyRegularWeapon();

        public bool CanWarp() => canWarp;

        public void EquipWeapon()
        {
        }

        public void EquipWeapon(GameObject weaponPrefab)
        {
            if (weaponPrefab == null)
            {
                return;
            }

            weaponSlots?.EquipWeapon(weaponPrefab);
        }

        public void EquipSpecialWeapon(GameObject weaponPrefab, int ammo)
        {
            if (weaponPrefab == null)
            {
                return;
            }

            weaponSlots?.EquipSpecialWeapon(weaponPrefab, ammo);
        }

        public void SetCurrentWeaponMagazine(int magazine)
        {
            var gun = weaponSlots?.GetCurrentGun();
            gun?.SetMagazineCount(magazine);
        }

        public void DropCurrentWeapon()
        {
            weaponSlots?.DropCurrentWeapon();
        }

        public void SwapWeapon()
        {
            if (weaponSlots != null)
            {
                weaponSlots.FlipWeapon();
            }
        }

        // ─── IPlayer: HP / Booster / Armor ──────────────────────────

        public virtual float GetHP() => Status?.Hp ?? 0f;

        public virtual float GetMaxHP() => Status?.MaxHp ?? 512f;

        public virtual float GetArmor() => Status?.Armor ?? 0f;

        public virtual float GetMaxArmor() => Status?.MaxArmor ?? 100f;

        public virtual float GetBooster() => Status?.Booster ?? 0f;

        public virtual float GetMaxBooster() => Status?.MaxBooster ?? 100f;

        // ─── IPlayer: プレイヤーリンク ────────────────────────────────

        public void CreatePlayerLink(EPlayerType type, string id)
        {
            if (type == EPlayerType.MyPlayer)
            {
                gameObject.AddComponent<PlayerDataLinker>();
            }
        }

        // ─── IDamageable ────────────────────────────────────────────

        public virtual void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            if (isDead || isInvincible || Status == null || !float.IsFinite(damage) || damage <= 0f) return;

            // Armor reduction logic: Armor absorbs 10% of damage, boosted by defense buff.
            float finalDamage = damage;
            if (Status.Armor > 0)
            {
                var safeDefenseMultiplier = float.IsFinite(defenseMultiplier)
                    ? Mathf.Max(0f, defenseMultiplier)
                    : 1f;
                float absorbed = damage * 0.1f * safeDefenseMultiplier;
                if (!float.IsFinite(absorbed))
                {
                    absorbed = 0f;
                }

                if (absorbed > Status.Armor)
                {
                    absorbed = Status.Armor;
                }
                Status.ReduceArmor(absorbed);
                finalDamage -= absorbed;
            }

            if (!float.IsFinite(finalDamage) || finalDamage <= 0f)
            {
                return;
            }

            Status.ReduceHp(finalDamage);

            if (Status.Hp <= 0)
            {
                isDead = true;
                Status.AddDeath();
                // DeathCount increment is handled by PlayerRegistry.ApplyDamage
                OnDead();
            }
        }

        public void AddDamageAndForce(float damage, Vector3 vec, float force = 1.0f)
        {
            if (!float.IsFinite(damage) || damage <= 0f
                || !float.IsFinite(force)
                || !float.IsFinite(vec.x) || !float.IsFinite(vec.y) || !float.IsFinite(vec.z))
            {
                return;
            }

            if (rigidbody2D != null)
            {
                rigidbody2D.AddForce(vec.normalized * force, ForceMode2D.Impulse);
            }

            // Keep the damage contract of this method: callers expect both
            // knockback and health reduction. Previously only the force was
            // applied, making several contact/impact attacks harmless.
            AddDamage(Vector2.zero, damage, eDamageType.None);
        }

        public void AddDamageAndForce2(float damage, Vector2 point)
        {
            AddDamageAndForce(damage, new Vector3(point.x, point.y, 0f), 1.0f);
        }

        /// <summary>
        /// Adopts the health the server says this player has.
        /// <para>
        /// The server owns health, so the value it broadcasts is the truth and
        /// the local one is only a prediction. This base type carries no health
        /// of its own, so it does nothing; a hierarchy that keeps a health bar
        /// overrides it to replace its local value with the server one.
        /// </para>
        /// </summary>
        public virtual bool ApplyServerHealth(int remainingHealth, int maxHealth)
        {
            return false;
        }


        public void Heal(float heal = 0)
        {
            if (!float.IsFinite(heal) || heal <= 0f || Status == null) return;
            var previousHp = Status?.Hp ?? 0f;
            Status.AddHp(heal);

            if (PlayerRegistry.Instance != null && Status != null && !Mathf.Approximately(previousHp, Status.Hp))
            {
                PlayerRegistry.Instance.NotifyPlayerHealthChanged(this, Status.Hp);
            }
        }

        public virtual void TakeLavaDamage()
        {
            if (lavaDamageCounter <= 0f)
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(PlayerEffectPrefabMasterData != null ? PlayerEffectPrefabMasterData.HitEffect : null, gameObject.transform.position, Quaternion.identity);
                }
                else
                {
                    if (PlayerEffectPrefabMasterData != null && PlayerEffectPrefabMasterData.HitEffect != null)
                    {
                        var effect = Instantiate(PlayerEffectPrefabMasterData.HitEffect);
                        effect.transform.position = gameObject.transform.position;
                        Destroy(effect, 5f);
                    }
                }
                if (lavaDamageCoroutine != null)
                {
                    StopCoroutine(lavaDamageCoroutine);
                }
                lavaDamageCoroutine = StartCoroutine(LavaCounter());
            }
        }

        public virtual void AddSlipDamage(float v, string id)
        {
            if (!float.IsFinite(v) || v <= 0f)
            {
                return;
            }

            AddDamage(Vector2.zero, v, eDamageType.Lava);
        }

        protected bool CheckFallDeath()
        {
            if (isDead)
            {
                return false;
            }

            if (transform.position.y >= fallDeathY)
            {
                return false;
            }

            isDead = true;
            if (Status != null)
            {
                Status.Hp = 0f;
                PlayerRegistry.Instance?.NotifyPlayerHealthChanged(this, 0f);
            }
            Status?.AddDeath();
            PlayerRegistry.Instance?.NotifyPlayerDied(this);
            GameEventBroker.Publish(new PlayerDeadEvent(EDeadReason.Suicide, gameObject.name, UniqueID().ToString(), Team()));
            OnDead();
            return true;
        }

        // ─── IPowerupable ────────────────────────────────────────────

        public bool IsSpeedUpNow() => moveSpeedMultiplier > 1f;

        public bool IsIncreaseAttackNow() => attackMultiplier > 1f;

        public bool IsIncreaseDefenseNow() => defenseMultiplier > 1f;

        public float AttackMultiplier() => attackMultiplier;

        public float DefenseMultiplier() => defenseMultiplier;

        public float MoveSpeedMultiplier() => moveSpeedMultiplier;

        public virtual void Burst()
        {
            OnBurst();
        }

        public void Berserk()
        {
            IncreaseAttack(10f);
            SpeedUp(10f);
        }

        public virtual void IncreaseAttack(float sec)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            PlayGeneralSound(EPlayerGeneralSound.TakeItem);
            SpawnPlayerEffect(PlayerEffectPrefabMasterData != null ? PlayerEffectPrefabMasterData.TakePowerUpItemEffect : null);
            StartCoroutine(IncreaseAttackCounter(sec));
        }

        public virtual void IncreaseDefense(float sec)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            PlayGeneralSound(EPlayerGeneralSound.TakeItem);
            SpawnPlayerEffect(PlayerEffectPrefabMasterData != null ? PlayerEffectPrefabMasterData.TakeDefenseUpItemEffect : null);
            StartCoroutine(IncreaseDefenseCounter(sec));
        }

        public virtual void Invisible(float sec)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            StartCoroutine(InvisibleCounter(sec));
        }

        public virtual void SpeedUp(float sec)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            PlayGeneralSound(EPlayerGeneralSound.TakeItem);
            SpawnPlayerEffect(PlayerEffectPrefabMasterData != null ? PlayerEffectPrefabMasterData.TakeSpeedUpItemEffect : null);
            StartCoroutine(SpeedUpCounter(sec));
        }

        public virtual void RefillGrenade()
        {
            PlayGeneralSound(EPlayerGeneralSound.TakeGrenade);
            Status?.RefillGrenade();
        }

        public virtual void RefillGrenade(EGrenadeType type, int amount = 3)
        {
            if (Status == null)
            {
                return;
            }

            PlayGeneralSound(EPlayerGeneralSound.TakeGrenade);
            Status.RefillGrenade(type, amount);
        }

        public virtual void PoisonBullet(float sec)
        {
            StartElementalDamage(sec, poisonDamagePerTick, eDamageType.Poison, "PoisonBullet");
        }

        public virtual void FireBullet(float sec)
        {
            StartElementalDamage(sec, fireDamagePerTick, eDamageType.Fire, "FireBullet");
        }

        private void StartElementalDamage(float duration, float damagePerTick, eDamageType damageType, string effectName)
        {
            if (isDead || !float.IsFinite(duration) || !float.IsFinite(damagePerTick) ||
                duration <= 0f || damagePerTick <= 0f)
            {
                return;
            }

            if (elementalDamageCoroutine != null)
            {
                StopCoroutine(elementalDamageCoroutine);
            }

            elementalDamageCoroutine = StartCoroutine(ElementalDamageCoroutine(duration, damagePerTick, damageType, effectName));
        }

        private IEnumerator ElementalDamageCoroutine(float duration, float damagePerTick, eDamageType damageType, string effectName)
        {
            var endTime = Time.realtimeSinceStartup + duration;
            var tickInterval = Mathf.Max(0.05f, elementalDamageTickInterval);

            Debug.Log($"[{GetType().Name}] {effectName} applied for {duration:0.##} sec");
            while (!isDead && Time.realtimeSinceStartup < endTime)
            {
                yield return new WaitForSecondsRealtime(tickInterval);
                if (Time.realtimeSinceStartup >= endTime || isDead)
                {
                    break;
                }

                AddDamage(Vector2.zero, damagePerTick, damageType);
            }

            elementalDamageCoroutine = null;
        }

        // ─── IMovable ────────────────────────────────────────────────

        public void Jump()
        {
            if (!CanJump() || rigidbody2D == null)
            {
                return;
            }

            isJump = true;
            rigidbody2D.AddForce(Vector2.up * jumpCurve.Evaluate(1.0f) * 10f, ForceMode2D.Impulse);
            PlayGeneralSound(EPlayerGeneralSound.JumpStart);
        }

        public bool Sitting() => isSitting;

        public virtual void Sit()
        {
            ApplySitState(true);
        }

        public bool IsStandUp() => !isSitting;

        public void StandUp()
        {
            ApplyStandUpState(true);
        }

        public bool IsLieDown() => isLyingDown;

        public void LieDown()
        {
            ApplyLieDownState(true);
        }

        // ─── プレイヤータイプ ────────────────────────────────────────

        public EPlayerType PlayerType() => playerType;

        public EPlayerCharacter Character() => character;

        private static uint StableNetworkId(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (string.IsNullOrEmpty(value)) return 0u;
                foreach (var character in value)
                {
                    hash ^= char.ToUpperInvariant(character);
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        public void SetPlayerType(EPlayerType type = EPlayerType.Unknown)
        {
            playerType = type;
        }

        // ─── その他の動作 ────────────────────────────────────────────

        public bool ReloadingNow()
        {
            var gun = weaponSlots?.GetCurrentGun();
            return gun != null && gun.CanReload() && !gun.CanShot();
        }

        public virtual void ReloadStart()
        {
            weaponSlots?.GetCurrentGun()?.ReloadStart();
        }

        public virtual void UseItem(int i = 0)
        {
            if (i == 0 && Status != null)
            {
                if (Status.UseGrenade())
                {
                    PlayGeneralSound(EPlayerGeneralSound.TakeGrenade);
                    Debug.Log($"[{GetType().Name}] Grenade used via base UseItem");
                    return;
                }
            }

            Debug.Log($"[{GetType().Name}] UseItem called with slot {i}");
        }

        public bool CanJump() => canJump;

        public int GrenadeCount() => Status?.GrenadeCount ?? 0;

        public virtual void OnDropWeapon()
        {
            PlayGeneralSound(EPlayerGeneralSound.DropItem);
            weaponSlots?.DropCurrentWeapon();
        }

        /// <summary>
        /// プレイヤーがマウス方向に向いている向きを返す (1: 右, -1: 左)
        /// </summary>
        public int GetFacingDirection()
        {
            if (Camera.main == null) return 1;
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);
            var direction = Input.mousePosition - screenPos;
            return direction.x >= 0 ? 1 : -1;
        }

        public void Warp(float coolTime = 2.0f)
        {
            if (!CanWarp())
            {
                return;
            }

            canWarp = false;
            warpCounter = SafeNonNegative(coolTime, 2f);
            StartCoroutine(WarpCounter());
        }

        public void AddDamageAndForce2Helper(float damage, Vector2 point)
        {
            AddDamageAndForce(damage, point, 1.0f);
        }

        protected void PlayGeneralSound(EPlayerGeneralSound sound, float volume = 1.0f, float pitch = 1.0f)
        {
            TryPlayGeneralSound(sound, volume, pitch);
        }

        public bool TryPlayGeneralSound(EPlayerGeneralSound sound, float volume = 1.0f, float pitch = 1.0f)
        {
            var masterData = ResolveGeneralSoundMasterData();
            if (masterData == null)
            {
                return false;
            }

            if (!masterData.TryGetSound(sound, out var clip) || clip == null)
            {
                return false;
            }

            OpenGS.SoundManager.Instance?.PlayOneShotSafe(clip, volume, pitch, $"{GetType().Name}:{sound}");
            return true;
        }

        protected PlayerGeneralSoundMasterData ResolveGeneralSoundMasterData()
        {
            if (GeneralSoundMasterData != null)
            {
                return GeneralSoundMasterData;
            }

            return Resources.Load<PlayerGeneralSoundMasterData>("MasterData/Sound/Players/PlayerGeneralSound");
        }

        // ─── コルーチン ──────────────────────────────────────────────

        public IEnumerator InvincibleCounter(float time = 4.0f)
        {
            if (!float.IsFinite(time) || time <= 0f)
            {
                isInvincible = false;
                yield break;
            }

            var version = ++invincibilityVersion;
            isInvincible = true;
            yield return new WaitForSecondsRealtime(time);

            if (version == invincibilityVersion)
            {
                isInvincible = false;
            }
        }

        /// <summary>
        /// Cancels a timed buff that was applied optimistically.
        /// <para>
        /// A field item is applied locally the moment it is touched so the
        /// pickup feels instant, and the server is asked afterwards. If it
        /// refuses, the buff has to be taken back. Bumping the version counter
        /// makes the running coroutine skip its restore, so the reset has to
        /// happen here as well.
        /// </para>
        /// </summary>
        public virtual void CancelTimedBuffs()
        {
            attackBuffVersion++;
            defenseBuffVersion++;
            speedBuffVersion++;
            invisibleBuffVersion++;

            attackMultiplier = 1f;
            defenseMultiplier = 1f;
            moveSpeedMultiplier = 1f;
            invisible = false;
            moveSpeed = baseMoveSpeed;
        }
        protected IEnumerator IncreaseAttackCounter(float time = 30.0f)
        {
            if (!float.IsFinite(time) || time <= 0) time = 30.0f;

            var version = ++attackBuffVersion;
            attackMultiplier = BuffedMultiplier;
            yield return new WaitForSecondsRealtime(time);
            if (version == attackBuffVersion)
            {
                attackMultiplier = 1f;
            }
        }

        public IEnumerator IncreaseDefenseCounter(float time = 30.0f)
        {
            if (!float.IsFinite(time) || time <= 0) time = 30.0f;

            var version = ++defenseBuffVersion;
            defenseMultiplier = BuffedMultiplier;
            yield return new WaitForSecondsRealtime(time);
            if (version == defenseBuffVersion)
            {
                defenseMultiplier = 1f;
            }
        }

        protected IEnumerator SpeedUpCounter(float time = 30.0f)
        {
            if (!float.IsFinite(time) || time <= 0) time = 30.0f;

            var version = ++speedBuffVersion;
            moveSpeedMultiplier = BuffedMultiplier;
            moveSpeed = baseMoveSpeed * moveSpeedMultiplier;
            yield return new WaitForSecondsRealtime(time);
            if (version == speedBuffVersion)
            {
                moveSpeedMultiplier = 1f;
                moveSpeed = baseMoveSpeed;
            }
        }

        protected IEnumerator InvisibleCounter(float time = 30.0f)
        {
            if (!float.IsFinite(time) || time <= 0) time = 30.0f;

            var version = ++invisibleBuffVersion;
            invisible = true;
            SetSpriteAlpha(InvisibleAlpha);
            yield return new WaitForSecondsRealtime(time);
            if (version == invisibleBuffVersion)
            {
                invisible = false;
                SetSpriteAlpha(1f);
            }
        }

        protected IEnumerator ReSpawnCounter(float time = 5.0f)
        {
            if (!float.IsFinite(time) || time < 0f) time = 5.0f;
            yield return new WaitForSecondsRealtime(time);
        }

        protected void ApplyMatchModeInitialStats()
        {
            if (Status == null)
            {
                return;
            }

            if (cachedBaseMaxHp <= 0f)
            {
                cachedBaseMaxHp = Status.MaxHp;
            }

            var multiplier = MatchModeResolver.ResolveHealthMultiplier(MatchModeResolver.ResolveCurrentGameMode());
            if (multiplier <= 1f)
            {
                return;
            }

            var targetMaxHp = Mathf.Max(1f, cachedBaseMaxHp * multiplier);
            Status.MaxHp = targetMaxHp;
            Status.Hp = targetMaxHp;
        }

        protected void EnterSpectatorMode()
        {
            if (PlayerType() != EPlayerType.MyPlayer)
            {
                return;
            }

            if (isSpectatorMode)
            {
                return;
            }

            isSpectatorMode = true;

            if (input != null)
            {
                input.enabled = false;
            }

            var matchScene = GetCachedMatchScene();
            matchScene?.EnterSpectatorMode(transform);

            GameEventBroker.Publish(new PlayerSpectatingEvent(UniqueID().ToString(), true));
        }

        protected void ExitSpectatorMode()
        {
            if (PlayerType() != EPlayerType.MyPlayer)
            {
                return;
            }

            if (!isSpectatorMode)
            {
                if (input != null)
                {
                    input.enabled = true;
                }
                return;
            }

            isSpectatorMode = false;

            if (input != null)
            {
                input.enabled = true;
            }

            var matchScene = GetCachedMatchScene();
            matchScene?.ExitSpectatorMode(transform);

            GameEventBroker.Publish(new PlayerSpectatingEvent(UniqueID().ToString(), false));
        }

        private IEnumerator ReserveReSpawnRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            reSpawnCoroutine = null;
            OnReSpawn();
        }

        protected IEnumerator WarpCounter()
        {
            while (warpCounter >= 0f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                warpCounter -= interval;
            }

            warpCounter = 0f;
            canWarp = true;
        }

        protected IEnumerator LavaCounter()
        {
            lavaDamageCounter = lavaDamageInterval;
            while (lavaDamageCounter >= 0f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                lavaDamageCounter -= interval;
            }
            lavaDamageCounter = 0f;
            lavaDamageCoroutine = null;
        }

        // ─── サウンドユーティリティ ──────────────────────────────────

        [CanBeNull]
        protected IBGMAndBGNManager SoundManager()
        {
            var temp = GameObject.FindGameObjectWithTag("SoundManager");
            if (temp == null) return null;
            return temp.GetComponent<IBGMAndBGNManager>();
        }

        // ─── イベント購読 ────────────────────────────────────────────

        protected void SubscribeEvent()
        {
            poseEventSubscription ??= GameEventBroker.Subscribe<PlayerPoseEvent>(ApplyPoseFromNetwork);
        }

        protected void UnSubscribeEvent()
        {
            poseEventSubscription?.Dispose();
            poseEventSubscription = null;
        }

        // ─── Odin Inspectorテストボタン ──────────────────────────────

        [Button("溶岩ダメージテスト")]
        public void TestTakeLavaDamage()
        {
            TakeLavaDamage();
        }

        [Button("ノックバックテスト")]
        public void KnockBack(Vector2 direction)
        {
            AddDamageAndForce(0f, direction, 1.0f);
        }

        protected virtual void Awake()
        {
            interval = Mathf.Max(0.01f, SafeNonNegative(interval, 0.1f));
            lavaDamageInterval = SafeNonNegative(lavaDamageInterval, 1.2f);
            lavaDamageCounter = SafeNonNegative(lavaDamageCounter, 0f);
            warpCounter = SafeNonNegative(warpCounter, 0f);
            moveSpeed = SafeNonNegative(moveSpeed, 0.4f);
            baseMoveSpeed = moveSpeed;
        }

        protected void ResetPowerupState()
        {
            attackBuffVersion++;
            defenseBuffVersion++;
            speedBuffVersion++;
            invisibleBuffVersion++;

            attackMultiplier = 1f;
            defenseMultiplier = 1f;
            moveSpeedMultiplier = 1f;
            invisible = false;
            moveSpeed = baseMoveSpeed;
            SetSpriteAlpha(1f);
        }

        protected void SetSpriteAlpha(float alpha)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            var color = spriteRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }

        protected void SpawnPlayerEffect(GameObject effectPrefab)
        {
            if (effectPrefab == null)
            {
                return;
            }

            if (effectService != null)
            {
                effectService.PlayOneShotEffect(effectPrefab, transform.position, Quaternion.identity);
                return;
            }

            var effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform, true);
            Destroy(effect, 5f);
        }

        public void PlayReloadCompleteEffect()
        {
            if (PlayerEffectPrefabMasterData != null && PlayerEffectPrefabMasterData.BoosterSparkEffectPrefab != null)
            {
                SpawnPlayerEffect(PlayerEffectPrefabMasterData.BoosterSparkEffectPrefab);
                return;
            }

            if (EffectPrefabMasterData != null && EffectPrefabMasterData.HitEffect != null)
            {
                SpawnPlayerEffect(EffectPrefabMasterData.HitEffect);
            }
        }

        private void CacheStandingMoveSpeed()
        {
            if (cachedStandingMoveSpeed)
            {
                return;
            }

            standingMoveSpeedCache = moveSpeed;
            cachedStandingMoveSpeed = true;
        }

        private void ApplySitState(bool notify)
        {
            if (isLyingDown)
            {
                isLyingDown = false;
                if (!Mathf.Approximately(cachedProneCameraZoomScale, 1f))
                {
                    GetCachedMatchScene()?.SetPlayerCameraZoom(1f / cachedProneCameraZoomScale);
                    cachedProneCameraZoomScale = 1f;
                }
            }

            if (isSitting)
            {
                return;
            }

            isSitting = true;
            CacheStandingMoveSpeed();
            moveSpeed = Mathf.Max(0.05f, baseMoveSpeed * 0.72f);
            SetPoseAnimatorState(isSit: true, isLieDown: false);

            var gun = weaponSlots?.GetCurrentGun();
            if (gun != null && gun.canZooming)
            {
                cachedCameraZoomScale = 1.18f;
                GetCachedMatchScene()?.SetPlayerCameraZoom(cachedCameraZoomScale);
            }
            else
            {
                cachedCameraZoomScale = 1f;
            }

            weaponSlots?.GetCurrentGun()?.Sit();
            if (notify)
            {
                PublishPose(EPlayerPoseState.Sit);
            }
        }

        private void ApplyStandUpState(bool notify)
        {
            var hadPose = isSitting || isLyingDown;
            if (!hadPose)
            {
                return;
            }

            isSitting = false;
            isLyingDown = false;
            if (cachedStandingMoveSpeed)
            {
                moveSpeed = standingMoveSpeedCache;
                cachedStandingMoveSpeed = false;
            }

            if (!Mathf.Approximately(cachedCameraZoomScale, 1f))
            {
                GetCachedMatchScene()?.SetPlayerCameraZoom(1f / cachedCameraZoomScale);
                cachedCameraZoomScale = 1f;
            }

            if (!Mathf.Approximately(cachedProneCameraZoomScale, 1f))
            {
                GetCachedMatchScene()?.SetPlayerCameraZoom(1f / cachedProneCameraZoomScale);
                cachedProneCameraZoomScale = 1f;
            }

            SetPoseAnimatorState(isSit: false, isLieDown: false);
            weaponSlots?.GetCurrentGun()?.StandUp();
            if (notify)
            {
                PublishPose(EPlayerPoseState.Stand);
            }
        }

        private void ApplyLieDownState(bool notify)
        {
            if (isLyingDown)
            {
                return;
            }

            isLyingDown = true;
            isSitting = false;
            CacheStandingMoveSpeed();
            moveSpeed = Mathf.Max(0.03f, baseMoveSpeed * 0.45f);
            SetPoseAnimatorState(isSit: false, isLieDown: true);

            var gun = weaponSlots?.GetCurrentGun();
            if (gun != null && gun.canZooming)
            {
                cachedProneCameraZoomScale = 1.28f;
                GetCachedMatchScene()?.SetPlayerCameraZoom(cachedProneCameraZoomScale);
            }
            else
            {
                cachedProneCameraZoomScale = 1f;
            }

            weaponSlots?.GetCurrentGun()?.Sit();
            if (notify)
            {
                PublishPose(EPlayerPoseState.LieDown);
            }
        }

        private AbstractMatchMainScript GetCachedMatchScene()
        {
            if (cachedMatchScene == null)
            {
                cachedMatchScene = FindFirstObjectByType<AbstractMatchMainScript>();
            }

            return cachedMatchScene;
        }

        private void PublishPose(EPlayerPoseState poseState)
        {
            if (!IsLocalPlayablePlayer())
            {
                return;
            }

            NetworkEventSerializer.SerializeAndSend(new PlayerPoseEvent(UniqueID().ToString(), poseState));
        }

        private void SetPoseAnimatorState(bool isSit, bool isLieDown)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool("IsSit", isSit);
            animator.SetBool("IsLieDown", isLieDown);
        }

        private void ApplyPoseFromNetwork(PlayerPoseEvent poseEvent)
        {
            if (poseEvent == null)
            {
                return;
            }

            if (!string.Equals(poseEvent.PlayerID(), UniqueID().ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            switch (poseEvent.PoseState())
            {
                case EPlayerPoseState.Sit:
                    ApplySitState(false);
                    break;
                case EPlayerPoseState.LieDown:
                    ApplyLieDownState(false);
                    break;
                default:
                    ApplyStandUpState(false);
                    break;
            }
        }

        private bool IsLocalPlayablePlayer()
        {
            return PlayerType() == EPlayerType.MyPlayer;
        }

        protected void PlayDeathAnimation()
        {
            var deathAnimation = GetComponentInChildren<DeathAnimation>();
            if (deathAnimation != null)
            {
                deathAnimation.Play();
            }
        }

        [Inject]
        private void InjectEffectService([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        protected virtual void OnEnable()
        {
            SubscribeEvent();
            OpenGS.Network.LagCompensationManager.Instance?.RegisterNetworkObject(this);
            ResolvePlayerRegistry()?.RegisterPlayer(this);
        }

        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            reSpawnCoroutine = null;
            lavaDamageCoroutine = null;
            if (elementalDamageCoroutine != null)
            {
                elementalDamageCoroutine = null;
            }

            isInvincible = false;
            canWarp = true;
            ResetPowerupState();

            UnSubscribeEvent();
            OpenGS.Network.LagCompensationManager.Instance?.UnregisterNetworkObject(OwnerPlayerId);
            ResolvePlayerRegistry()?.UnregisterPlayer(this);
        }

        protected virtual void OnDestroy()
        {
            Status?.Dispose();
        }

        private PlayerRegistry ResolvePlayerRegistry()
        {
            return PlayerRegistry.Instance != null
                ? PlayerRegistry.Instance
                : FindFirstObjectByType<PlayerRegistry>();
        }
    }
}
