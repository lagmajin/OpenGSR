


using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionModeCollection : MonoBehaviour
    {
        [AutoCreateIfMissing("MissionMainScript", typeof(MissionMainScript))]
        public GameObject missionMainScript;


        private void Start()
        {
            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.LoadDebugMissionSelect();
            }


        }


    }


}
