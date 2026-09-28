using DG.Tweening;
using System;
using OpenGSCore;
using UnityEngine;




namespace OpenGS
{


    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(MultipleTags))]
    public class FlagController : MonoBehaviour, IFlagInfo
    {
        [Header("Settings")]
        [Tooltip("How long a dropped flag waits before it goes home. The default is the shared contract's; changing it here changes only this client's view of it, because the server keeps the time.")]
        [SerializeField] private float autoReturnTime = CaptureTheFlagRules.DefaultAutoReturnSeconds;
        [SerializeField] public ETeam team = ETeam.NoTeam;
        [SerializeField] public Sprite redFlag;
        [SerializeField] public Sprite blueFlag;
        [SerializeField] private GameObject droppedSmokeEffectPrefab;
        [SerializeField] private GameObject returnEffectPrefab;

        [Header("Components")]
        [SerializeField] private CTFGameSoundMasterData ctfSoundMasterData;
        [SerializeField] private FlagStand myFlagStand;
        [SerializeField] private SpriteRenderer spriteRenderer;

        // Where a flag is, and why one came home, are both the shared contract's
        // types now. This file used to carry its own enum for each, with AtBase,
        // Carried and Dropped where the shared vocabulary says FlagOnStand,
        // FlagCapturedPlayer and FlagOnGround. Three spellings of one state is
        // how the two sides ended up unable to agree on where a flag was, which
        // is the question the capture rule turns on.

        public event Action<FlagController, AbstractPlayer> EnemyPickedUp;
        public event Action<FlagController, AbstractPlayer, EFlagReturnReason> ReturnedToBase;
        public event Action<FlagController> Dropped;

        // Where a flag is, in the shared contract's spelling. This used to be a
        // second enum nested here, with AtBase, Carried and Dropped where the
        // shared one says FlagOnStand, FlagCapturedPlayer and FlagOnGround. Two
        // names for one state is how the two sides ended up unable to agree on
        // where a flag was, which is the question the capture rule turns on.
        public EFlagState CurrentState => currentState;

        private EFlagState currentState = EFlagState.FlagOnStand;
        private float returnTimer = 0f;
        private Transform carrier;
        private GameObject activeDroppedEffect;

        private void Awake()
        {
            // The wait is the shared contract's, not this file's. It used to be a
            // thirty second literal here and another one on the server, so the two
            // sides could disagree about when a flag was due back, and that is
            // not a cosmetic difference: a client that waits longer than the
            // server destroys the flag after the server has already put it home,
            // and a client that waits less brings one back the server still
            // believes is lying on the ground.
            autoReturnTime = CaptureTheFlagRules.SanitizeAutoReturnSeconds(autoReturnTime);
        }

        private void Start()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            UpdateSprite();
        }

        private void Update()
        {
            if (currentState == EFlagState.FlagOnGround)
            {
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    return;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                returnTimer = Mathf.Max(0f, returnTimer - deltaTime);
                if (returnTimer <= 0)
                {
                    ReturnToBase();
                }
            }
            else if (currentState == EFlagState.FlagCapturedPlayer && carrier != null)
            {
                // プレイヤーに追従（必要に応じて背負う位置などを調整）
                transform.position = carrier.position + new Vector3(0, 1f, 0);
            }
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.sprite = (team == ETeam.Red) ? redFlag : blueFlag;
        }

        public void SetInitialBase(FlagStand stand)
        {
            myFlagStand = stand;
        }

        public void OnPickedUp(AbstractPlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (player.Team() == team)
            {
                // ベース上の味方フラッグには反応しない。
                if (currentState == EFlagState.FlagOnStand)
                {
                    return;
                }

                // 味方のフラッグを拾った（リターン）
                ReturnToBase(player, EFlagReturnReason.FriendlyRecovered);
            }
            else
            {
                // 敵のフラッグを拾った（キャプチャ開始）
                currentState = EFlagState.FlagCapturedPlayer;
                carrier = player.transform;
                player.EnemyFlagCaptured();
                player.BindEnemyFlag(this);
                InvokeSafely(EnemyPickedUp, this, player, nameof(EnemyPickedUp));
            }
        }

        public void OnDropped()
        {
            if (currentState != EFlagState.FlagCapturedPlayer)
            {
                return;
            }

            currentState = EFlagState.FlagOnGround;
            carrier = null;
            returnTimer = autoReturnTime;
            PlayDroppedEffect();
            InvokeSafely(Dropped, this, nameof(Dropped));
        }

        public void ReturnToBase(AbstractPlayer player = null, EFlagReturnReason reason = EFlagReturnReason.AutoReturn)
        {
            currentState = EFlagState.FlagOnStand;
            carrier = null;
            returnTimer = 0f;

            ClearDroppedEffect();
            PlayReturnEffect();

            InvokeSafely(ReturnedToBase, this, player, reason, nameof(ReturnedToBase));
            if (myFlagStand != null)
            {
                myFlagStand.SetFlag();
            }
            Destroy(gameObject);
        }

        private static void InvokeSafely(Action<FlagController, AbstractPlayer> handlers, FlagController flag, AbstractPlayer player, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<FlagController, AbstractPlayer> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(flag, player);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FlagController] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<FlagController> handlers, FlagController flag, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<FlagController> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(flag);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FlagController] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<FlagController, AbstractPlayer, EFlagReturnReason> handlers, FlagController flag, AbstractPlayer player, EFlagReturnReason reason, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<FlagController, AbstractPlayer, EFlagReturnReason> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(flag, player, reason);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FlagController] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private void OnDestroy()
        {
            ClearDroppedEffect();
        }

        private void PlayDroppedEffect()
        {
            if (activeDroppedEffect != null)
            {
                return;
            }

            var prefab = GetDroppedSmokeEffectPrefab();
            if (prefab == null)
            {
                return;
            }

            activeDroppedEffect = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            activeDroppedEffect.name = $"{FlagName()}_DroppedSmoke";
        }

        private void ClearDroppedEffect()
        {
            if (activeDroppedEffect != null)
            {
                Destroy(activeDroppedEffect);
                activeDroppedEffect = null;
            }
        }

        private void PlayReturnEffect()
        {
            var prefab = GetReturnEffectPrefab();
            if (prefab == null)
            {
                return;
            }

            var spawnedEffect = Instantiate(prefab, transform.position, Quaternion.identity);
            Destroy(spawnedEffect, 5f);
        }

        private GameObject GetDroppedSmokeEffectPrefab()
        {
            if (droppedSmokeEffectPrefab != null)
            {
                return droppedSmokeEffectPrefab;
            }

            return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/SmokeBombEffect");
        }

        private GameObject GetReturnEffectPrefab()
        {
            if (returnEffectPrefab != null)
            {
                return returnEffectPrefab;
            }

            return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/SmokeBombEffect");
        }

        public string FlagName() => (team == ETeam.Red) ? "RedFlag" : "BlueFlag";

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other != null ? other.GetComponentInParent<AbstractPlayer>() : null;
            if (player != null)
            {
                if (currentState == EFlagState.FlagCapturedPlayer)
                {
                    return;
                }

                OnPickedUp(player);
            }
        }
    }
}


