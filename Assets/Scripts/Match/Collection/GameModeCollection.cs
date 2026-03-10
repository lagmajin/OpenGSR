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

        [Required] public GameObject dmMatchMainScript;
        [Required] public GameObject tdmMatchMainScript;

        [Required] public GameObject suvMatchMainScript;
        [Required] public GameObject tsuvMatchMainScript;
        [Required] public GameObject ctfMatchMainScript;

        [Required] public GameObject armMatchMainScript;
        [Required] public GameObject godModeMainScript;



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



                //var gameManager = GameGeneralManager.GetInstance;

                //var rule = gameManager.MatchRoom.Rule;

                //var mode = rule.Mode;

                //Debug.Log("Boot" + mode.ToString());

                //SetupDeathMatch();

                //SetupTDMMatch();

                SetupDeathMatch();

                //SetupCTFMatch();

                //booted = true;

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
            //if (!isAnyOn())
            {
                dmMatchMainScript?.SetActive(true);




                if (TryGetComponent<DMMatchMainScript>(out var script))
                {
                    //GameGeneralManager.GetInstance.MainScript = script;
                }


                if (autoDeleteOthers)
                {
                    DeleteNotUseScripts();
                }
            }
        }

        private void SetupTDMMatch()
        {

            //if (!IsAnyOn())
            {
                tdmMatchMainScript?.SetActive(true);
                if (autoDeleteOthers)
                {
                    DeleteNotUseScripts();
                }
            }


        }

        private void SetupSUV()
        {
            if (!IsAnyOn())
            {
                suvMatchMainScript?.SetActive(true);

                if (autoDeleteOthers)
                {
                    DeleteNotUseScripts();
                }

            }


        }

        private void SetupTSUV()
        {
            if (!IsAnyOn())
            {
            }

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }

        }

        private void SetupCTFMatch()
        {
            //if (!IsAnyOn())
            {
                ctfMatchMainScript.SetActive(true);
            }

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

            return null;
        }




    }

}
