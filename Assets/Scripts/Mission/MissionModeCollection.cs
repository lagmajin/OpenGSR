


using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionModeCollection : MonoBehaviour
    {
        public GameObject missionMainScript;
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
