#pragma warning disable 8632
#pragma warning disable 0414
#pragma warning disable 0218
using Newtonsoft.Json;
//using RuntimeScriptField;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace OpenGS
{
    internal interface IGameModeCollection
    {

    }

    [DisallowMultipleComponent]
    public class GameModeCollection : MonoBehaviour, IGameModeCollection
    {
        //[OdinSerialize][Inject] ClientSessionData data;

        [Required, AutoCreateIfMissing("DMMatchMainScript", typeof(DMMatchMainScript))] public GameObject dmMatchMainScript;
        [Required, AutoCreateIfMissing("TDMMatchMainScript", typeof(TDMMatchMainScript))] public GameObject tdmMatchMainScript;

        [Required, AutoCreateIfMissing("SUVMatchMainScript")] public GameObject suvMatchMainScript;
        [Required, AutoCreateIfMissing("TSUVMatchMainScript", typeof(TSUVMainScript))] public GameObject tsuvMatchMainScript;
        [Required, AutoCreateIfMissing("CTFMatchMainScript", typeof(CTFMatchMainScript))] public GameObject ctfMatchMainScript;

        [Required, AutoCreateIfMissing("ArmMatchMainScript")] public GameObject armMatchMainScript;
        [Required, AutoCreateIfMissing("GodModeMainScript", typeof(GodModeMainScript))] public GameObject godModeMainScript;



        [Required][SerializeField] public DMMatchMainScript scriptTest;
        [Required]
        [SerializeField] public TDMMatchMainScript matchMainScript;



        public GameObject BattleUIManager;



        public GameObject SoundStorageManager;

        //public ComponentReference test;


        private bool booted = false;

        [SerializeField]
        public bool bootImmidietry = true;

        [SerializeField]
        public bool autoDeleteOthers = true;


        private List<GameObject> mainscriptList;

        public GeneralSceneMasterData generalScene;
        //public 


        void Start()
        {


            //GameGeneralManager.GetInstance.LoadDebugSelect();

            //test.AddTo(this.gameObject);


            Application.targetFrameRate = 75;


            if (bootImmidietry)
            {
                Boot();
            }
        }

        void Boot()
        {
            if (!booted)
            {
                var mode = MatchModeResolver.ResolveCurrentGameMode();
                Debug.Log($"[GameModeCollection] Boot mode={mode}");

                switch (mode)
                {
                    case OpenGSCore.EGameMode.TeamDeathMatch:
                        SetupTDMMatch();
                        break;
                    case OpenGSCore.EGameMode.Survival:
                        SetupSUV();
                        break;
                    case OpenGSCore.EGameMode.TeamSurvival:
                        SetupTSUV();
                        break;
                    case OpenGSCore.EGameMode.CaptureTheFlag:
                        SetupCTFMatch();
                        break;
                    case OpenGSCore.EGameMode.ArmsRace:
                        SetupArmsRace();
                        break;
                    case OpenGSCore.EGameMode.DeathMatch:
                    default:
                        SetupDeathMatch();
                        break;
                }

                booted = true;
            }
        }

        bool IsAnyOn()
        {
            /*

            if(dmMatchMainScript.isActiveAndEnabled)
            {
                //isActiveAndEnabled

                return false;
            }

            if(tdmMatchMainScript.isActiveAndEnabled)
            {
                return false;
            }

            if(suvMatchMainScript.isActiveAndEnabled)
            {
                return false;
            }

            if(tsuvMatchMainScript.isActiveAndEnabled)
            {
                return false;
            }

            */

            return true;
        }

        private void SetupDeathMatch()
        {
            dmMatchMainScript?.SetActive(true);
            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupTDMMatch()
        {
            tdmMatchMainScript?.SetActive(true);
            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupSUV()
        {
            suvMatchMainScript?.SetActive(true);

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupTSUV()
        {
            tsuvMatchMainScript?.SetActive(true);

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }

        }

        private void SetupCTFMatch()
        {
            ctfMatchMainScript?.SetActive(true);

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupArmsRace()
        {
            if (!IsAnyOn())
            {
            }

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }


        private void DeleteNotUseScripts()
        {
            if (!dmMatchMainScript.activeSelf)
            {
                Destroy(dmMatchMainScript.gameObject);
            }

            if (!tdmMatchMainScript.activeSelf)
            {
                Destroy(tdmMatchMainScript.gameObject);
            }

            if (!suvMatchMainScript.activeSelf)
            {
                Destroy(suvMatchMainScript.gameObject);
            }

            if (!tsuvMatchMainScript.gameObject.activeSelf)
            {
                Debug.Log("CTF ok");

                Destroy(tsuvMatchMainScript.gameObject);
            }


            if (!ctfMatchMainScript.gameObject.activeSelf)
            {
                Debug.Log("CTF ok");

                Destroy(ctfMatchMainScript.gameObject);
            }

            if (!armMatchMainScript.gameObject.activeSelf)
            {
                Destroy(armMatchMainScript);
            }

        }

        private void BootError()
        {

        }
        [Button("タイトル移動テスト")]
        private void BackToWaitRoom()
        {

        }
        [Button("タイトル移動テスト")]
        private void BackToOnlineWaitRoom()
        {

        }

        [Button("タイトル移動テスト")]
        private void BackToTitle()
        {
            SceneManager.LoadScene(generalScene.TitleScene());

        }


        public IDMMatchMainScript DMMatchMainScript()
        {
            var result = dmMatchMainScript.GetComponent<IDMMatchMainScript>();


            return result;
        }

        public AbstractMatchMainScript? CurrentGameMainScript()
        {
            if (dmMatchMainScript != null && dmMatchMainScript.activeSelf)
            {
                return dmMatchMainScript.GetComponent<DMMatchMainScript>();
            }

            if (tdmMatchMainScript != null && tdmMatchMainScript.activeSelf)
            {
                return tdmMatchMainScript.GetComponent<TDMMatchMainScript>();
            }

            if (suvMatchMainScript != null && suvMatchMainScript.activeSelf)
            {
                return suvMatchMainScript.GetComponent<AbstractMatchMainScript>();
            }

            if (tsuvMatchMainScript != null && tsuvMatchMainScript.activeSelf)
            {
                return tsuvMatchMainScript.GetComponent<AbstractMatchMainScript>();
            }

            if (ctfMatchMainScript != null && ctfMatchMainScript.activeSelf)
            {
                return ctfMatchMainScript.GetComponent<AbstractMatchMainScript>();
            }

            if (armMatchMainScript != null && armMatchMainScript.activeSelf)
            {
                return armMatchMainScript.GetComponent<AbstractMatchMainScript>();
            }

            if (godModeMainScript != null && godModeMainScript.activeSelf)
            {
                return godModeMainScript.GetComponent<AbstractMatchMainScript>();
            }

            Debug.LogWarning("[GameModeCollection] CurrentGameMainScript() could not find an active match main script.");
            return null;
        }




    }

}
