using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using System.IO;
using UnityEditor;
using UnityEngine;

using OpenGSCore;
using System.Collections.Generic;
using System;
using System.Linq;

namespace OpenGS
{
    public class PlayerEquipLoader
    {
        private readonly string _filePath;

        public PlayerEquipLoader(string fileName = "PlayerEquip.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public PlayerEquipData Load()
        {
            if (!File.Exists(_filePath))
            {
                Debug.LogWarning($"[PlayerEquipLoader] File not found: {_filePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                JObject jObj = JObject.Parse(json);

                var characterStr = jObj["PlayerCharacter"]?.ToString();
                var itemArray = jObj["InstantItemSlot"] as JArray;

                if (!System.Enum.TryParse(characterStr, out EPlayerCharacter playerCharacter))
                    return null;

                var items = itemArray?
                    .Select(t => Enum.TryParse(t.ToString(), out EInstantItemType type) ? type : default)
                    .ToArray();

                return new PlayerEquipData
                {
                    PlayerCharacter = playerCharacter,
                    InstantItemSlots = items ?? Array.Empty<EInstantItemType>()
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerEquipLoader] Failed to load: {ex.Message}");
                return null;
            }
        }
    }
    public class PlayerEquipData
    {
        public EPlayerCharacter PlayerCharacter;
        public EInstantItemType[] InstantItemSlots;
    }


    public class PlayerEquipEditor : EditorWindow
    {
        private EPlayerCharacter playerCharacter = EPlayerCharacter.Ami;

        private EInstantItemType itemSlot1;
        private EInstantItemType itemSlot2;
        private EInstantItemType itemSlot3;

        private string fileName = "PlayerEquip.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, fileName);

        [MenuItem("Tools/PlayerEquipEditor")]

        public static void ShowWindow()
        {
            GetWindow<PlayerEquipEditor>("PlayerEquipEditor");
        }
        private void OnEnable()
        {



        }
        private void OnGUI()
        {
            GUILayout.Label("Instant Items", EditorStyles.boldLabel);

            itemSlot1 = (EInstantItemType)EditorGUILayout.EnumPopup("Slot 1", itemSlot1);
            itemSlot2 = (EInstantItemType)EditorGUILayout.EnumPopup("Slot 2", itemSlot2);
            itemSlot3 = (EInstantItemType)EditorGUILayout.EnumPopup("Slot 3", itemSlot3);

            if (GUILayout.Button("Save"))
            {
                SaveFile();
            }

            if (GUILayout.Button("OpenExploler"))
            {
                OpenJsonFile();
            }

        }

        [Button("保存")]
        private void SaveFile()
        {
            var json = new JObject();

            

            var slots = new EInstantItemType[] {itemSlot1, itemSlot2, itemSlot3};
            var jObj = new JObject
            {
                ["PlayerCharacter"] = playerCharacter.ToString(),
                ["InstantItemSlot"] = new JArray(slots)
            };



            File.WriteAllText(FilePath, json.ToString());
        }
        [Button("エクスプローラーで開く")]
        private void OpenJsonFile()
        {
            //string fullPath = Path.GetFullPath(filePath);

#if UNITY_EDITOR_WIN
            //System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
#elif UNITY_EDITOR_OSX
    System.Diagnostics.Process.Start("open", $"-R \"{fullPath}\"");
#elif UNITY_EDITOR_LINUX
    System.Diagnostics.Process.Start("xdg-open", Path.GetDirectoryName(fullPath));
#else
    Debug.LogWarning("OpenJsonFile not supported on this platform");
#endif
        }
    }
}