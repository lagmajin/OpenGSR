using OpenGSCore;
using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using Sirenix.OdinInspector;

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

        [SerializeField] [BoxGroup("Status")] private float itemInterval = 0.1f;
        [SerializeField] [BoxGroup("Status")] private float dashInterval = 0.1f;
        [SerializeField] [BoxGroup("Status")] private float interval = 0.1f;
        [SerializeField] [BoxGroup("Status")] private float rollingInterval = 60.0f;
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

        [SerializeField] protected Rigidbody2D rigidbody2D;
        [SerializeField] [Required] protected WeaponSlots weaponSlots;

        [SerializeField] public GameObject Hand;
        [SerializeField] private bool noSound = false;

        [SerializeField] private EPlayerType playerType = EPlayerType.Unknown;

        // ─── 状態フィールド ─────────────────────────────────────────

        public float moveSpeed = 0.4f;

        protected bool isDead = false;
        protected bool isJump = false;
        protected bool invisible = false;
        protected float jumpPos = 0.0f;
        protected float jumpInterval = 10.0f;
        protected bool canEquip = false;
        protected int dashCount = 0;

        private float _lastDamageTime = -10f;
        private float warpDelayCounter;
        private float increaseItemCounter;
        private bool hasTeam;
        private ETeam myTeam;
        private bool canWarp;
        private bool canJump;
        private MultipleTags myTags;

        protected SpriteRenderer spriteRenderer;

        // ─── PlayerStatus (HP・Booster を一元管理) ──────────────────

        public PlayerStatus Status { get; set; } = new PlayerStatus();

        // ─── IPlayer: ライフサイクル ─────────────────────────────────
        private bool hasEnemyFlag = false;

        public virtual void OnDead()
        {
            if (hasEnemyFlag)
            {
                hasEnemyFlag = false;
                CTFMatchMainScript.Instance?.PlayerFlagLost(myTeam == ETeam.Red ? ETeam.Blue : ETeam.Red);
            }
        }

        public virtual void OnBurst()
        {
            if (hasEnemyFlag)
            {
                hasEnemyFlag = false;
                CTFMatchMainScript.Instance?.PlayerFlagLost(myTeam == ETeam.Red ? ETeam.Blue : ETeam.Red);
            }
        }

        public virtual void OnSpawn()
        {
            Status?.FullRecovery(); // Recover HP, Booster, Grenades

            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.PublishPlayerSpawned(this);
            }
        }

        public virtual void OnReSpawn()
        {
            Status?.FullRecovery(); // Recover HP, Booster, Grenades

            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.PublishPlayerRespawned(this);
            }
        }

        public virtual void ReserveReSpawn(float delay)
        {
        }

        // ─── IPlayer: 状態クエリ ─────────────────────────────────────

        public bool IsGround() => check != null && check.IsGround;

        public bool IsDead() => isDead;

        public bool IsRolling() => false;

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

        public void EnemyFlagReturnedToBase()
        {
            hasEnemyFlag = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{character} delivered enemy flag to base!");
#endif
        }

        // ─── IPlayer: 装備 ──────────────────────────────────────────

        public bool CanEquip() => weaponSlots != null && weaponSlots.CanEquip();

        public bool CanWarp() => canWarp;

        public void EquipWeapon()
        {
        }

        public void EquipWeapon(GameObject weaponPrefab)
        {
        }

        public void DropCurrentWeapon()
        {
            Debug.Log("DropCurrentWeapon");
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

            // Armor reduction logic: Armor absorbs 10% of damage
            float finalDamage = damage;
            if (Status.Armor > 0)
            {
                float absorbed = damage * 0.1f;
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
                var effect = Instantiate(PlayerEffectPrefabMasterData.HitEffect);
                effect.transform.position = gameObject.transform.position;
                StartCoroutine(LavaCounter());
            }
        }

        public virtual void AddSlipDamage(float v, string id)
        {
        }

        // ─── IPowerupable ────────────────────────────────────────────

        public bool IsSpeedUpNow() => false;

        public bool IsIncreaseAttackNow() => false;

        public bool IsIncreaseDefenseNow() => false;

        public virtual void Burst()
        {
        }

        public void Berserk()
        {
        }

        public virtual void IncreaseAttack(float sec)
        {
            StartCoroutine(IncreaseAttackCounter(sec));
        }

        public virtual void IncreaseDefense(float sec)
        {
            StartCoroutine(IncreaseDefenseCounter(sec));
        }

        public virtual void Invisible(float sec)
        {
        }

        public virtual void SpeedUp(float sec)
        {
        }

        public virtual void PoisonBullet(float sec)
        {
        }

        public virtual void FireBullet(float sec)
        {
        }

        // ─── IMovable ────────────────────────────────────────────────

        public void Jump()
        {
        }

        public bool Sitting() => false;

        public virtual void Sit()
        {
        }

        public bool IsStandUp() => false;

        public void StandUp()
        {
        }

        public bool IsLieDown() => false;

        public void LieDown()
        {
            Debug.Log("LieDown");
        }

        // ─── プレイヤータイプ ────────────────────────────────────────

        public EPlayerType PlayerType() => playerType;

        public void SetPlayerType(EPlayerType type = EPlayerType.Unknown)
        {
            playerType = type;
        }

        // ─── その他の動作 ────────────────────────────────────────────

        public bool ReloadingNow() => false;

        public virtual void ReloadStart()
        {
            Debug.Log("ReloadStart");
        }

        public virtual void UseItem(int i = 0)
        {
        }

        public bool CanJump() => canJump;

        public int GrenadeCount() => Status?.GrenadeCount ?? 0;

        public virtual void OnDropWeapon()
        {
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
        }

        public void AddDamageAndForce2Helper(float damage, Vector2 point)
        {
        }

        // ─── コルーチン ──────────────────────────────────────────────

        public IEnumerator InvincibleCounter(float time = 4.0f)
        {
            yield return new WaitForSecondsRealtime(time);
        }

        protected IEnumerator IncreaseAttackCounter(float time = 30.0f)
        {
            if (time <= 0) time = 30.0f;

            increaseItemCounter = time;
            while (increaseItemCounter >= 0f)
            {
                yield return new WaitForSecondsRealtime(itemInterval);
                increaseItemCounter -= itemInterval;
            }
        }

        public IEnumerator IncreaseDefenseCounter(float time = 30.0f)
        {
            if (time <= 0) time = 30.0f;

            increaseItemCounter = time;
            while (increaseItemCounter >= 0f)
            {
                yield return new WaitForSecondsRealtime(itemInterval);
                increaseItemCounter -= itemInterval;
            }
        }

        protected IEnumerator ReSpawnCounter(float time = 5.0f)
        {
            yield return new WaitForSecondsRealtime(time);
        }

        protected IEnumerator WarpCounter()
        {
            while (warpCounter >= 0f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                warpCounter -= interval;
            }
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
    }
}
