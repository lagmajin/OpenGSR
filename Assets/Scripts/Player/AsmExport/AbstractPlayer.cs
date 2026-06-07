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
    public abstract class AbstractPlayer : MonoBehaviour, IPowerupable, IDamageable, IPlayer, IMovable, IEventActor
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

        [SerializeField] [BoxGroup("Health")] protected float damageInvincibleTime = 0.2f;

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
        protected bool invisible = false;
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

        protected SpriteRenderer spriteRenderer;

        private int attackBuffVersion;
        private int defenseBuffVersion;
        private int speedBuffVersion;
        private int invisibleBuffVersion;

        // ─── PlayerStatus (HP・Booster を一元管理) ──────────────────

        public PlayerStatus Status { get; set; } = new PlayerStatus();

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
            ResetPowerupState();
            Status?.FullRecovery(); // Recover HP, Booster, Grenades
            Status?.FullCombatRecovery();
            ApplyMatchModeInitialStats();
            ExitSpectatorMode();
            canJump = true;
            canWarp = true;
            isSitting = false;

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
            ResetPowerupState();
            Status?.FullRecovery(); // Recover HP, Booster, Grenades
            Status?.FullCombatRecovery();
            ApplyMatchModeInitialStats();
            ExitSpectatorMode();
            canJump = true;
            canWarp = true;
            isSitting = false;

            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.PublishPlayerRespawned(this);
            }
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
            if (isDead || Status == null) return;

            // Armor reduction logic: Armor absorbs 10% of damage, boosted by defense buff.
            float finalDamage = damage;
            if (Status.Armor > 0)
            {
                float absorbed = damage * 0.1f * defenseMultiplier;
                if (absorbed > Status.Armor)
                {
                    absorbed = Status.Armor;
                }
                Status.ReduceArmor(absorbed);
                finalDamage -= absorbed;
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
            if (rigidbody2D != null)
            {
                rigidbody2D.AddForce(vec.normalized * force, ForceMode2D.Impulse);
            }
        }

        public void AddDamageAndForce2(float damage, Vector2 point)
        {
            AddDamageAndForce(damage, new Vector3(point.x, point.y, 0f), 1.0f);
        }

        public void Heal(float heal = 0)
        {
            if (heal <= 0) return;
            Status.AddHp(heal);
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
                    }
                }
                StartCoroutine(LavaCounter());
            }
        }

        public virtual void AddSlipDamage(float v, string id)
        {
            if (v <= 0f)
            {
                return;
            }

            AddDamage(Vector2.zero, v, eDamageType.Lava);
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
            PlayGeneralSound(EPlayerGeneralSound.TakeItem);
            SpawnPlayerEffect(PlayerEffectPrefabMasterData != null ? PlayerEffectPrefabMasterData.TakePowerUpItemEffect : null);
            StartCoroutine(IncreaseAttackCounter(sec));
        }

        public virtual void IncreaseDefense(float sec)
        {
            PlayGeneralSound(EPlayerGeneralSound.TakeItem);
            SpawnPlayerEffect(PlayerEffectPrefabMasterData != null ? PlayerEffectPrefabMasterData.TakeDefenseUpItemEffect : null);
            StartCoroutine(IncreaseDefenseCounter(sec));
        }

        public virtual void Invisible(float sec)
        {
            StartCoroutine(InvisibleCounter(sec));
        }

        public virtual void SpeedUp(float sec)
        {
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
            Debug.Log($"[{GetType().Name}] PoisonBullet applied for {sec} sec");
        }

        public virtual void FireBullet(float sec)
        {
            Debug.Log($"[{GetType().Name}] FireBullet applied for {sec} sec");
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
            isSitting = true;
            weaponSlots?.GetCurrentGun()?.Sit();
        }

        public bool IsStandUp() => !isSitting;

        public void StandUp()
        {
            isSitting = false;
            weaponSlots?.GetCurrentGun()?.StandUp();
        }

        public bool IsLieDown() => false;

        public void LieDown()
        {
            Debug.Log("LieDown");
        }

        // ─── プレイヤータイプ ────────────────────────────────────────

        public EPlayerType PlayerType() => playerType;

        public EPlayerCharacter Character() => character;

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
            warpCounter = coolTime;
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

            OpenGS.SoundManager.Instance.PlayOneShotSafe(clip, volume, pitch, $"{GetType().Name}:{sound}");
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
            yield return new WaitForSecondsRealtime(time);
        }

        protected IEnumerator IncreaseAttackCounter(float time = 30.0f)
        {
            if (time <= 0) time = 30.0f;

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
            if (time <= 0) time = 30.0f;

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
            if (time <= 0) time = 30.0f;

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
            if (time <= 0) time = 30.0f;

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

            var matchScene = FindFirstObjectByType<AbstractMatchMainScript>();
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

            var matchScene = FindFirstObjectByType<AbstractMatchMainScript>();
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
        }

        protected void UnSubscribeEvent()
        {
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
    }
}
