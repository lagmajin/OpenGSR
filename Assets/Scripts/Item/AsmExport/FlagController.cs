using DG.Tweening;
using System;
using UnityEngine;




namespace OpenGS
{


    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(MultipleTags))]
    public class FlagController : MonoBehaviour, IFlagInfo
    {
        public enum EFlagState
        {
            AtBase,
            Carried,
            Dropped
        }

        [Header("Settings")]
        [SerializeField] public ETeam team = ETeam.NoTeam;
        [SerializeField] public Sprite redFlag;
        [SerializeField] public Sprite blueFlag;
        [SerializeField] private float autoReturnTime = 30f;
        [SerializeField] private GameObject droppedSmokeEffectPrefab;
        [SerializeField] private GameObject returnEffectPrefab;

        [Header("Components")]
        [SerializeField] private CTFGameSoundMasterData ctfSoundMasterData;
        [SerializeField] private FlagStand myFlagStand;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public enum EFlagReturnReason
        {
            AutoReturn,
            FriendlyRecovered,
            CapturedAtBase
        }

        public event Action<FlagController, AbstractPlayer> EnemyPickedUp;
        public event Action<FlagController, AbstractPlayer, EFlagReturnReason> ReturnedToBase;
        public event Action<FlagController> Dropped;

        private EFlagState currentState = EFlagState.AtBase;
        private float returnTimer = 0f;
        private Transform carrier;
        private GameObject activeDroppedEffect;

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
            if (currentState == EFlagState.Dropped)
            {
                returnTimer -= Time.deltaTime;
                if (returnTimer <= 0)
                {
                    ReturnToBase();
                }
            }
            else if (currentState == EFlagState.Carried && carrier != null)
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
                if (currentState == EFlagState.AtBase)
                {
                    return;
                }

                // 味方のフラッグを拾った（リターン）
                ReturnToBase(player, EFlagReturnReason.FriendlyRecovered);
            }
            else
            {
                // 敵のフラッグを拾った（キャプチャ開始）
                currentState = EFlagState.Carried;
                carrier = player.transform;
                player.EnemyFlagCaptured();
                player.BindEnemyFlag(this);
                EnemyPickedUp?.Invoke(this, player);
            }
        }

        public void OnDropped()
        {
            if (currentState != EFlagState.Carried)
            {
                return;
            }

            currentState = EFlagState.Dropped;
            carrier = null;
            returnTimer = autoReturnTime;
            PlayDroppedEffect();
            Dropped?.Invoke(this);
        }

        public void ReturnToBase(AbstractPlayer player = null, EFlagReturnReason reason = EFlagReturnReason.AutoReturn)
        {
            currentState = EFlagState.AtBase;
            carrier = null;
            returnTimer = 0f;

            ClearDroppedEffect();
            PlayReturnEffect();

            ReturnedToBase?.Invoke(this, player, reason);
            if (myFlagStand != null)
            {
                myFlagStand.SetFlag();
            }
            Destroy(gameObject);
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

            Instantiate(prefab, transform.position, Quaternion.identity);
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
            if (other.TryGetComponent(out AbstractPlayer player))
            {
                if (currentState == EFlagState.Carried)
                {
                    return;
                }

                OnPickedUp(player);
            }
        }
    }
}

