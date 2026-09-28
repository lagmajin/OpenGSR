#pragma warning disable 8632
#pragma warning disable 0414
#pragma warning disable 0218
using Newtonsoft.Json;
//using RuntimeScriptField;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
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

        [Required, AutoCreateIfMissing("SUVMatchMainScript", typeof(SurvivalMatchMainScript))] public GameObject suvMatchMainScript;
        [Required, AutoCreateIfMissing("TSUVMatchMainScript", typeof(TSUVMainScript))] public GameObject tsuvMatchMainScript;
        [Required, AutoCreateIfMissing("CTFMatchMainScript", typeof(CTFMatchMainScript))] public GameObject ctfMatchMainScript;

        [Required, AutoCreateIfMissing("ArmMatchMainScript")] public GameObject armMatchMainScript;
        [Required, AutoCreateIfMissing("GodModeMainScript", typeof(GodModeMainScript))] public GameObject godModeMainScript;

        /// <summary>
        /// Builds the match scripts a scene left out.
        /// <para>
        /// Every mode has a slot here and the attribute on it says the slot is
        /// meant to be filled, but the attribute is an editor drawer, so it does
        /// nothing when the game runs. A scene that shipped with an empty slot
        /// therefore started with that mode unplayable and said so only in a log
        /// line nobody reads: the mode was selectable in the lobby, the match
        /// began, and nothing ever happened because the script that was supposed
        /// to be running was not there.
        /// </para>
        /// <para>
        /// Each one is built on its own object rather than added to this one,
        /// because a mode script is a singleton that destroys a duplicate, and
        /// sharing an object with the collection would make the two destroy each
        /// other.
        /// </para>
        /// </summary>
        private void Awake()
        {
            suvMatchMainScript = EnsureMatchScript(suvMatchMainScript, "SUVMatchMainScript", typeof(SurvivalMatchMainScript));
            tsuvMatchMainScript = EnsureMatchScript(tsuvMatchMainScript, "TSUVMatchMainScript", typeof(TSUVMainScript));
            ctfMatchMainScript = EnsureMatchScript(ctfMatchMainScript, "CTFMatchMainScript", typeof(CTFMatchMainScript));
            armMatchMainScript = EnsureMatchScript(armMatchMainScript, "ArmMatchMainScript", null);
            godModeMainScript = EnsureMatchScript(godModeMainScript, "GodModeMainScript", typeof(GodModeMainScript));
        }

        /// <summary>
        /// Returns the assigned script, or one built now when the scene left the
        /// slot empty. A mode with no script behind it cannot be played, so the
        /// alternative is a mode that is offered and does nothing.
        /// </summary>
        private GameObject EnsureMatchScript(GameObject assigned, string objectName, Type? componentType)
        {
            if (assigned != null)
            {
                return assigned;
            }

            var created = new GameObject(objectName);

            if (componentType != null)
            {
                created.AddComponent(componentType);
            }

            Debug.Log(
                $"[GameModeCollection] {objectName} was not assigned in this scene, so it was built at runtime. " +
                "Assign it in the scene to stop the mode being assembled on every load.");
            return created;
        }




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


            Application.targetFrameRate = SettingsManager.Instance.GetGraphicsSettings().TargetFrameRate;


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
            ActivateMatchScript(dmMatchMainScript, "DeathMatch");
            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupTDMMatch()
        {
            ActivateMatchScript(tdmMatchMainScript, "TeamDeathMatch");
            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupSUV()
        {
            ActivateMatchScript(suvMatchMainScript, "Survival");

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupTSUV()
        {
            ActivateMatchScript(tsuvMatchMainScript, "TeamSurvival");

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }

        }

        private void SetupCTFMatch()
        {
            ActivateMatchScript(ctfMatchMainScript, "CaptureTheFlag");

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private void SetupArmsRace()
        {
            ActivateMatchScript(armMatchMainScript, "ArmsRace");

            if (autoDeleteOthers)
            {
                DeleteNotUseScripts();
            }
        }

        private static bool ActivateMatchScript(GameObject matchScript, string modeName)
        {
            if (matchScript == null)
            {
                Debug.LogError($"[GameModeCollection] {modeName} match script is not assigned.");
                return false;
            }

            matchScript.SetActive(true);
            return true;
        }


        private void DeleteNotUseScripts()
        {
            if (dmMatchMainScript != null && !dmMatchMainScript.activeSelf)
            {
                Destroy(dmMatchMainScript.gameObject);
            }

            if (tdmMatchMainScript != null && !tdmMatchMainScript.activeSelf)
            {
                Destroy(tdmMatchMainScript.gameObject);
            }

            if (suvMatchMainScript != null && !suvMatchMainScript.activeSelf)
            {
                Destroy(suvMatchMainScript.gameObject);
            }

            if (tsuvMatchMainScript != null && !tsuvMatchMainScript.gameObject.activeSelf)
            {
                Debug.Log("CTF ok");

                Destroy(tsuvMatchMainScript.gameObject);
            }


            if (ctfMatchMainScript != null && !ctfMatchMainScript.gameObject.activeSelf)
            {
                Debug.Log("CTF ok");

                Destroy(ctfMatchMainScript.gameObject);
            }

            if (armMatchMainScript != null && !armMatchMainScript.gameObject.activeSelf)
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
            LoadConfiguredScene(generalScene != null ? generalScene.OfflineWaitRoomScene() : null, "OfflineWaitRoom");
        }
        [Button("タイトル移動テスト")]
        private void BackToOnlineWaitRoom()
        {
            LoadConfiguredScene(generalScene != null ? generalScene.OnlineWaitRoomScene() : null, "OnlineWaitRoom");
        }

        [Button("タイトル移動テスト")]
        private void BackToTitle()
        {
            if (generalScene == null)
            {
                Debug.LogError("[GameModeCollection] GeneralSceneMasterData is not assigned.");
                return;
            }

            var sceneName = generalScene.TitleScene();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[GameModeCollection] Title scene is not configured.");
                return;
            }

            LoadConfiguredScene(sceneName, "Title");

        }

        private static void LoadConfiguredScene(string sceneName, string label)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError($"[GameModeCollection] {label} scene is not configured.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GameModeCollection] {label} scene is not in build settings: {sceneName}");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }


        public IDMMatchMainScript DMMatchMainScript()
        {
            if (dmMatchMainScript == null)
            {
                Debug.LogWarning("[GameModeCollection] DM match main script is not assigned.");
                return null;
            }

            var result = dmMatchMainScript.GetComponent<IDMMatchMainScript>();
            if (result == null)
            {
                Debug.LogWarning("[GameModeCollection] Assigned DM object has no IDMMatchMainScript component.");
            }

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
