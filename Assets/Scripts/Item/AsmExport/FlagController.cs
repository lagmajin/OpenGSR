using DG.Tweening;
//using ICSharpCode.SharpZipLib.Zip;
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

        [Header("Components")]
        [SerializeField] private CTFGameSoundMasterData ctfSoundMasterData;
        [SerializeField] private FlagStand myFlagStand;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private EFlagState currentState = EFlagState.AtBase;
        private float returnTimer = 0f;
        private Transform carrier;

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
            currentState = EFlagState.AtBase;
        }

        public void OnPickedUp(AbstractPlayer player)
        {
            if (player.Team() == team)
            {
                // 味方のフラッグを拾った（リターン）
                ReturnToBase();
                CTFMatchMainScript.Instance?.PlayerFlagReturned(team);
            }
            else
            {
                // 敵のフラッグを拾った（キャプチャ開始）
                currentState = EFlagState.Carried;
                carrier = player.transform;
                player.EnemyFlagCaptured();
                CTFMatchMainScript.Instance?.PlayerFlagPickedUp(team, player.gameObject.name);
            }
        }

        public void OnDropped()
        {
            currentState = EFlagState.Dropped;
            carrier = null;
            returnTimer = autoReturnTime;
            CTFMatchMainScript.Instance?.PlayerFlagLost(team);
        }

        public void ReturnToBase()
        {
            if (myFlagStand != null)
            {
                myFlagStand.SetFlag();
            }
            Destroy(gameObject);
        }

        public string FlagName() => (team == ETeam.Red) ? "RedFlag" : "BlueFlag";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (currentState == EFlagState.Carried) return;

            if (other.TryGetComponent(out AbstractPlayer player))
            {
                OnPickedUp(player);
            }
        }
    }
}

