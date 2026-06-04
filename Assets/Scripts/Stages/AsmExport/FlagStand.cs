using DG.Tweening;
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class FlagStand : MonoBehaviour, IFlagStand
    {
        public event Action<FlagStand, FlagController> FlagSpawned;
        public event Action<FlagStand, AbstractPlayer> FlagCaptured;

        [SerializeField]
        bool showFlagNavigator = true;
        [SerializeField]
        ETeam team;
        string flagStandName = "None";
        [SerializeField] public GameObject flagSlot;
        public GameObject flagNavigator;

        private bool hasFlag = false;
        private FlagController currentFlagController;

        [SerializeField] public FlagMasterData flagMasterData;

        [SerializeField] private SystemSoundMasterData systemSoundMasterData;
        [SerializeField] private CTFGameSoundMasterData ctfSoundMasterData;
        [SerializeField] private EffectPrefabMasterData effectPrefabMasterData;
        [SerializeField] private CTFMediateObject ctfMediateObject;
        [SerializeField] [Required] private GameObject particleSlot;

        private void Start()
        {
            if (!flagSlot)
            {
                flagSlot = particleSlot;
            }

            if (flagNavigator != null)
            {
                flagNavigator.SetActive(showFlagNavigator);
            }

            UpdateUIState();
        }

        void Reset()
        {
            flagStandName = team.ToString() + "Stand";
        }

        [Button("ファンファーレテスト")]
        public void PlayFlagReturnSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySystemSound(ESystemSound.Fanfare);
            }
        }

        public void FlagReady()
        {
            hasFlag = false;
            UpdateUIState();
        }

        [Button("ナビゲーター表示")]
        public void SetActiveFlagNavigator(bool active = true)
        {
            if (flagNavigator != null)
            {
                flagNavigator.SetActive(active);
            }
        }

        private void Update()
        {
            if (flagNavigator != null)
            {
                flagNavigator.SetActive(showFlagNavigator);
            }
        }

        public ETeam Team => team;

        public string FlagStandName()
        {
            if (!string.IsNullOrEmpty(flagStandName) && flagStandName != "None")
            {
                return flagStandName;
            }

            return team == ETeam.Red ? "RedStand" : "BlueStand";
        }

        public bool HasFlag()
        {
            return hasFlag;
        }

        [Button("フラッグセット")]
        public void SetFlag()
        {
            RemoveFlag();

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

                if (flagObj.TryGetComponent(out FlagController fc))
                {
                    fc.SetInitialBase(this);
                    BindFlagController(fc);
                }
            }

            UpdateUIState();
        }

        [Button("フラッグ削除")]
        public void RemoveFlag()
        {
            UnbindCurrentFlagController();

            foreach (Transform c in flagSlot.transform)
            {
                Destroy(c.gameObject);
            }

            hasFlag = false;
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

        private void BindFlagController(FlagController flagController)
        {
            if (flagController == null)
            {
                return;
            }

            currentFlagController = flagController;
            FlagSpawned?.Invoke(this, currentFlagController);
        }

        private void UnbindCurrentFlagController()
        {
            if (currentFlagController != null)
            {
                currentFlagController = null;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IPlayer pl))
            {
                ETeam playerTeam = pl.Team();

                if (playerTeam == team)
                {
                    if (pl.HasEnemyFlag() && HasFlag())
                    {
                        Debug.Log("Capture Success: " + team);
                        if (pl is AbstractPlayer ap)
                        {
                            FlagCaptured?.Invoke(this, ap);
                        }
                    }
                }
                else
                {
                    if (HasFlag())
                    {
                        Debug.Log("Flag Taken by: " + playerTeam);

                        RemoveFlag();

                        GameObject flagPrefab = (team == ETeam.Red) ? flagMasterData.redFlag : flagMasterData.blueFlag;
                        var droppedFlag = Instantiate(flagPrefab, transform.position, Quaternion.identity);
                        if (droppedFlag.TryGetComponent(out FlagController fc))
                        {
                            BindFlagController(fc);
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
