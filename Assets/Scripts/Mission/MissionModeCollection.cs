


using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionModeCollection : MonoBehaviour
    {
        [AutoCreateIfMissing("MissionMainScript", typeof(MissionMainScript))]
        public GameObject missionMainScript;

        [AutoCreateIfMissing("SkyFighterMainScript", typeof(SkyFighterMainScript))]
        public GameObject skyFighterMainScript;


        private void Start()
        {
            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.LoadDebugMissionSelect();
            }


        }


    }


}
