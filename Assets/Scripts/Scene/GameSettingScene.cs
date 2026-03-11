
using System.Threading;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;


#pragma warning disable 0414

namespace OpenGS
{


    public class GameSettingScene : AbstractScene, IGameSettingScene
    {
        // Start is called before the first frame update

        [SerializeField] [Required] public GameSettingSceneMediateObject mediateObject;



        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }
        void Start()
        {



        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ApplyGameSetting();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {

            }

            if (Input.GetKeyDown(KeyCode.Return))
            {

            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitGame();
            }

        }

        private void OnApplicationQuit()
        {

        }

        void ApplyGameSetting()
        {
            var manager = GameGeneralManager.GetInstance;



        }

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }

        void ExitGame()
        {
            UnityEngine.Application.Quit();
        }
    }

}