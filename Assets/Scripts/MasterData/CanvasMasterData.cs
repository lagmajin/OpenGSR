using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/UI/CanvasMasterData")]
    public class CanvasMasterData : ScriptableObject
    {
        public GameObject ReSpawnUICanvas;
        public GameObject PlayerStatusUI;

        /// <summary>
        /// The kill feed and the scoreboard, which ship as one canvas because both
        /// are always wanted and neither is ever wanted alone.
        /// </summary>
        public GameObject MatchOverlay;
    }
}
