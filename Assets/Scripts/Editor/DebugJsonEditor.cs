using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using System;


namespace OpenGS
{
    //[System.Serializable]
    public class LocalNetworkSettingsLoader
    {
        private readonly string _filePath;

        public LocalNetworkSettingsLoader(string filePath = "Assets/Settings.json")
        {
            _filePath = Path.GetFullPath(filePath);
        }

        public DebugLocalNetworkSettings Load()
        {
            if (!File.Exists(_filePath))
            {
                Debug.LogWarning($"[LocalNetworkSettingsLoader] File not found: {_filePath}");
                return new DebugLocalNetworkSettings(); // デフォルト返す
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                var jObj = JObject.Parse(json);

                return new DebugLocalNetworkSettings
                {
                    localServerTestMode = jObj["DebugMode"]?.Value<bool>() ?? false,
                    localTCPPort = jObj["LocalTCPPort"]?.Value<int>() ?? 7777,
                    localUDPPort = jObj["LocalUDPPort"]?.Value<int>() ?? 7777,
                    externalServerIP = jObj["ExternalServerIP"]?.Value<string>() ?? "127.0.0.1"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalNetworkSettingsLoader] Failed to parse settings: {ex.Message}");
                return new DebugLocalNetworkSettings();
            }
        }

        public void Save(DebugLocalNetworkSettings settings)
        {
            var jObj = new JObject
            {
                ["DebugMode"] = settings.localServerTestMode,
                ["LocalTCPPort"] = settings.localTCPPort,
                ["LocalUDPPort"] = settings.localUDPPort,
                ["ExternalServerIP"] = settings.externalServerIP
            };

            try
            {
                File.WriteAllText(_filePath, jObj.ToString());
                Debug.Log($"[LocalNetworkSettingsLoader] Saved to {_filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalNetworkSettingsLoader] Failed to save: {ex.Message}");
            }
        }
    }

    public class SettingsEditor : EditorWindow
    {
        private DebugLocalNetworkSettings settings;
        private string filePath = "Assets/Settings.json";

        [MenuItem("Tools/Settings Editor")]
        public static void ShowWindow()
        {
            GetWindow<SettingsEditor>("LocalNetworkSettings Editor");
        }

        private void OnEnable()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);

                Debug.Log("json:"+json.ToString());

                JObject jObj = JObject.Parse(json);

                settings = new DebugLocalNetworkSettings
                {
                    localServerTestMode = jObj["DebugMode"]?.Value<bool>() ?? false,
                    localTCPPort = jObj["LocalTCPPort"]?.Value<int>() ?? 7777,
                    localUDPPort = jObj["LocalUDPPort"]?.Value<int>() ?? 7777,
                    externalServerIP = jObj["ExternalServerIP"]?.Value<string>() ?? "127.0.0.1"
                };
            }
            else
            {
                settings = new DebugLocalNetworkSettings(); // 初期設定
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Edit Settings", EditorStyles.boldLabel);

            // ローカルサーバーテストモードのチェックボックス
            settings.localServerTestMode = EditorGUILayout.Toggle("Local Server Test Mode", settings.localServerTestMode);
            settings.localTCPPort = EditorGUILayout.IntField("Overide default local tcp port",settings.localTCPPort);
            settings.localUDPPort = EditorGUILayout.IntField("Overide default local udp port", settings.localUDPPort);

            // 外部サーバのIP入力フィールド
            settings.externalServerIP = EditorGUILayout.TextField("External Server IP", settings.externalServerIP);

            // 保存ボタン
            if (GUILayout.Button("Save"))
            {
                SaveFile();
            }

            if (GUILayout.Button("Open exploler"))
            {
                OpenExploler();
            }

            if(GUILayout.Button("Open Text Editor"))
            {
                OpenTextEditor();
            }
        }

        private void SaveFile()
        {
            var json = new JObject();

            json["DebugMode"] = settings.localServerTestMode;
            json["LocalTCPPort"] = settings.localTCPPort;
            json["LocalUDPPort"] = settings.localUDPPort;


            File.WriteAllText(filePath, json.ToString());

            // 設定をJSON形式で保存
            //string json = JsonUtility.ToJson(settings, true);
            //File.WriteAllText(filePath, json);
            //AssetDatabase.Refresh(); // アセットのリフレッシュ
           // Debug.Log("Settings saved!");
        }

        private void OpenExploler()
        {
            string fullPath = Path.GetFullPath(filePath);

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

        private void OpenTextEditor()
        {
            string fullPath = Path.GetFullPath(filePath);

#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("notepad.exe", $"\"{fullPath}\"");
#elif UNITY_EDITOR_OSX
    System.Diagnostics.Process.Start("open", $"-a TextEdit \"{fullPath}\"");
#elif UNITY_EDITOR_LINUX
    System.Diagnostics.Process.Start("xdg-open", fullPath);
#else
    Debug.LogWarning("OpenEditor not supported on this platform");
#endif
        }

    }
}