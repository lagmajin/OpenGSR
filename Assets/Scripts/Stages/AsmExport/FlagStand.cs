using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenGS
{






    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class FlagStand : MonoBehaviour, IFlagStand
    {
        [SerializeField]
        bool showFlagNavigator = true;
        [SerializeField]
        ETeam team;
        string flagStandName = "None";
        [SerializeField] public GameObject flagSlot;
        public GameObject flagNavigator;


        private bool hasFlag = false;

        //[SerializeField]private AbstractBattleSceneMediateObject object;


        [SerializeField] public FlagMasterData flagMasterData;

        [SerializeField] private SystemSoundMasterData systemSoundMasterData;
        [SerializeField] private CTFGameSoundMasterData ctfSoundMasterData;
        [SerializeField] private EffectPrefabMasterData effectPrefabMasterData;
        //[SerializeField] private AbstractBattleSceneMediateObject mediateObject;
        [SerializeField] private CTFMediateObject ctfMediateObject;
        [SerializeField] [Required] private GameObject particleSlot;
        private void Start()
        {

            if (!flagSlot)
            {

            }


        }

        void Reset()
        {

        }

        [Button("ファンファーレテスト")]
        public void PlayFlagReturnSound()
        {


        }

        public void FlagReady()
        {

        }


        [Button("ナビゲーター表示")]
        public void SetActiveFlagNavigator(bool active = true)
        {
            flagNavigator.SetActive(active);


        }



        private void Update()
        {

        }

        public string FlagStandName()
        {


            return "BlueStand";
        }

        public bool HasFlag()
        {

            return hasFlag;
        }

        [Button("フラッグセット")]
        public void SetFlag()
        {
            RemoveFlag(); // 念のため既存を削除

            hasFlag = true;
            GameObject flagObj = null;

            if (team == ETeam.Red)
            {
                flagObj = Instantiate(flagMasterData.redFlagInSlot, flagSlot.transform);
            }
            else if (team == ETeam.Blue)
            {
                flagObj = Instantiate(flagMasterData.blueFlagInSlot, flagSlot.transform);
            }

            if (flagObj != null)
            {
                flagObj.transform.localPosition = Vector3.zero;
                
                // FlagController があればベース情報をセット
                if (flagObj.TryGetComponent(out FlagController fc))
                {
                    fc.SetInitialBase(this);
                }
            }

            // UI更新
            UpdateUIState();
        }

        [Button("フラッグ削除")]
        public void RemoveFlag()
        {
            foreach (Transform c in flagSlot.transform)
            {
                Destroy(c.gameObject);
            }
            hasFlag = false;
            
            // UI更新
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            if (CTFScoreUIManager.Instance != null)
            {
                if (team == ETeam.Red) CTFScoreUIManager.Instance.UpdateRedFlagState(hasFlag, false, !hasFlag);
                else CTFScoreUIManager.Instance.UpdateBlueFlagState(hasFlag, false, !hasFlag);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IPlayer pl))
            {
                ETeam playerTeam = pl.Team();

                if (playerTeam == team)
                {
                    // 自チームのプレイヤーがベースに来た
                    if (pl.HasEnemyFlag() && HasFlag())
                    {
                        // キャプチャ成功！(自軍フラッグがベースにあり、かつ敵フラッグを持っている)
                        Debug.Log("Capture Success: " + team);
                        CTFMatchMainScript.Instance?.PlayerFlagCaptured(team);
                        
                        // プレイヤーのフラッグ状態をクリア（実際には持っているフラッグをDestroyするなどの処理が必要）
                        // ここではPlayer側の実装に合わせて呼び出す
                        pl.EnemyFlagReturnedToBase(); 
                    }
                }
                else
                {
                    // 敵チームのプレイヤーがベースに来た
                    if (HasFlag())
                    {
                        // 敵に奪われた
                        Debug.Log("Flag Taken by: " + playerTeam);
                        
                        // スタンドからフラッグを消し、物理フラッグアイテム（FlagController）を生成するか、
                        // あるいはプレイヤーに直接持たせる。
                        // ここでは FlagController の OnPickedUp に相当する挙動を作る。
                        
                        RemoveFlag();
                        
                        // 物理的なフラッグアイテムを生成してプレイヤーに持たせる
                        GameObject flagPrefab = (team == ETeam.Red) ? flagMasterData.redFlag : flagMasterData.blueFlag;
                        var droppedFlag = Instantiate(flagPrefab, transform.position, Quaternion.identity);
                        if (droppedFlag.TryGetComponent(out FlagController fc))
                        {
                            fc.SetInitialBase(this);
                            if (other.TryGetComponent(out AbstractPlayer ap))
                            {
                                fc.OnPickedUp(ap);
                            }
                        }
                    }
                }
            }
        }
    }
}
