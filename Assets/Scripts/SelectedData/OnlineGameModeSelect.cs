

using JetBrains.Annotations;
using Newtonsoft.Json;
using OpenGSCore;
//using UnityEditor.SceneManagement;

namespace OpenGS
{



    [JsonObject][System.Serializable]

    public class OnlineGameModeSelect
    {

        public EGameMode GameMode { get; set; } = EGameMode.Unknown;

        public EMap Map { get; set; } = EMap.Unknown;

        public bool TeamBalance { get; set; } = true;


        public TimeLimit TimeLimit { get; set; }

        public OnlineGameModeSelect()
        {

        }



    }

    public class OfflineGameModeSelect : IOfflineGameModeSelect
    {
        public EGameMode GameMode { get; set; } = EGameMode.Unknown;

        public int Capacity { get; set; } = 0;


        public EMap Map { get; set; } = EMap.Unknown;

        public bool TeamBalance { get; set; } = true;

        public OfflineGameModeSelect()
        {

        }

        public OfflineGameModeSelect(EGameMode gameMode = EGameMode.Unknown, EMap stage = EMap.Unknown, int capacity = 8)
        {

            GameMode = gameMode;
            Map = stage;
            Capacity = capacity;

        }


    }


    public class OnlineMissionModeSelect
    {

    }



    public class OfflineMissionModeSelect
    {
        private int? Mission { get; set; } = null;
        private int? Quest { get; set; } = null;





    }


}
