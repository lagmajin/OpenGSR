using System.IO;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    public class PlayerEquipEditor : EditorWindow
    {
        private EPlayerCharacter playerCharacter = EPlayerCharacter.Ami;
        private EInstantItemType itemSlot1;
        private EInstantItemType itemSlot2;
        private EInstantItemType itemSlot3;
        private EGrenadeType grenadeSlot1 = EGrenadeType.Normal;
        private EGrenadeType grenadeSlot2 = EGrenadeType.Normal;
        private EGrenadeType grenadeSlot3 = EGrenadeType.Normal;

        private string fileName = "PlayerEquip.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, fileName);

        [MenuItem("Tools/PlayerEquipEditor")]
        public static void ShowWindow()
        {
            GetWindow<PlayerEquipEditor>("PlayerEquipEditor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Instant Items", EditorStyles.boldLabel);

            itemSlot1 = (EInstantItemType)EditorGUILayout.EnumPopup("Slot 1", itemSlot1);
            itemSlot2 = (EInstantItemType)EditorGUILayout.EnumPopup("Slot 2", itemSlot2);
            itemSlot3 = (EInstantItemType)EditorGUILayout.EnumPopup("Slot 3", itemSlot3);

            GUILayout.Space(8);
            GUILayout.Label("Grenade Slots", EditorStyles.boldLabel);

            grenadeSlot1 = (EGrenadeType)EditorGUILayout.EnumPopup("Grenade 1", grenadeSlot1);
            grenadeSlot2 = (EGrenadeType)EditorGUILayout.EnumPopup("Grenade 2", grenadeSlot2);
            grenadeSlot3 = (EGrenadeType)EditorGUILayout.EnumPopup("Grenade 3", grenadeSlot3);

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
            var slots = new EInstantItemType[] { itemSlot1, itemSlot2, itemSlot3 };
            var grenadeSlots = new EGrenadeType[] { grenadeSlot1, grenadeSlot2, grenadeSlot3 };
            var jObj = new JObject
            {
                ["PlayerCharacter"] = playerCharacter.ToString(),
                ["InstantItemSlot"] = new JArray(slots),
                ["GrenadeSlot"] = new JArray(grenadeSlots)
            };

            File.WriteAllText(FilePath, jObj.ToString());
        }

        [Button("エクスプローラーで開く")]
        private void OpenJsonFile()
        {
            var fullPath = Path.GetFullPath(FilePath);

#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
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
