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

                if (OnlineGameSelect == null)
                {
                    Debug.LogWarning("[GameModeSelectManager] No online game mode selection to save.");
                    return;
                }

                var path = Path.Combine(Application.persistentDataPath, defaultName);
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(path, JsonConvert.SerializeObject(OnlineGameSelect, Formatting.Indented));
                Debug.Log($"[GameModeSelectManager] Saved debug online selection: {path}");


            }
        }

        public void LoadDebugOnlineSelectFromFile()
        {
            if (DebugFlagManager.IsDebug())
            {
                var path = Path.Combine(Application.persistentDataPath, defaultName);
                if (!File.Exists(path))
                {
                    Debug.Log($"[GameModeSelectManager] Debug selection file not found: {path}");
                    return;
                }

                try
                {
                    var loaded = JsonConvert.DeserializeObject<OnlineGameModeSelect>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        OnlineGameSelect = loaded;
                        Debug.Log($"[GameModeSelectManager] Loaded debug online selection: {path}");
                    }
                    else
                    {
                        Debug.LogWarning($"[GameModeSelectManager] Debug selection was empty: {path}");
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[GameModeSelectManager] Failed to load debug selection: {exception.Message}");
                }

                return;

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
