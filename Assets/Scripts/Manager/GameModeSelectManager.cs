using UnityEngine;

using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace OpenGS
{
    internal interface IGameModeSelectManager
    {

    }

    //#GameModeSelectManager
    public class GameModeSelectManager : IGameModeSelectManager
    {
        public OfflineGameModeSelect OfflineGameSelect { get; set; }

        public OnlineGameModeSelect OnlineGameSelect { get; set; }

        public static GameModeSelectManager Instance { get; } = new();

        public static readonly string defaultName = "OnlineGameModeSelect.json";

        private GameModeSelectManager()
        {

        }

        public void SaveDebugOnlineSelectToFile()
        {
            if (DebugFlagManager.IsDebug())
            {
                //var json1= new StreamWriter(Application.persistentDataPath + "/" + defaultName);

                var mode = OnlineGameSelect.GameMode;

                Debug.Log("Save debug select...");


            }
        }

        public void LoadDebugOnlineSelectFromFile()
        {
            if (DebugFlagManager.IsDebug())
            {
                var json1 = new StreamReader(Application.persistentDataPath + "/" + DebugPlayerSelect.defalutName);

                var mapSelect = json1.ReadToEnd();

                Debug.Log(mapSelect);

                //JsonConvert.DeserializeObject(json1.)

                var json2 = new StreamReader(Application.persistentDataPath + "/" + DebugGameModeSelect.defalutName);

                var modeSelect = json2.ReadToEnd();

                Debug.Log(modeSelect.ToString());


                

            }
        }



        public void SaveDebugMissionSelect()
        {
            if (DebugFlagManager.IsDebug())
            {

            }
            else
            {

            }


        }

        public void LoadDebugMissionSelect()
        {
            if (DebugFlagManager.IsDebug())
            {

            }
            else
            {

            }

        }

    }
}
