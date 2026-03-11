




using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;



namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OfflineMissionWaitRoom : AbstractScene
    {
        private GameObject missionSelectDialog;
        private GameObject mirrorNetworkManager;

        [SerializeField] [Required] private MissionWaitRoomMediateObject mediateObject;


        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);

        }
        private void Start()
        {

        }

        private void Update()
        {


        }

        private void OnApplicationQuit()
        {
            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.SaveDebugMissionSelect();
            }

            //GeneralServerNetworkManager.GetInstance().Disconnect();
        }

        private void AppointedRoomOwner()
        {

        }

        public void ShowMissionSelectDialog()
        {

        }

        public void MissionDifficlucyChanged()
        {

        }

        public void EnterMission()
        {


        }

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }
    }
}
