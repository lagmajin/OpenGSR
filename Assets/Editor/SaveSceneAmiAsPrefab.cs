using UnityEditor;
using UnityEngine;

namespace OpenGS.Editor
{
    public static class SaveSceneAmiAsPrefab
    {
        private const string OutputPath = "Assets/Prefabs/Player/Ami.prefab";

        [MenuItem("OpenGSR/Player/Save Scene Ami As Prefab")]
        public static void Save()
        {
            var ami = FindSceneAmi();
            if (ami == null)
            {
                EditorUtility.DisplayDialog("Ami not found", "シーン上の ///Player(Ami) が見つかりません。", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Save Ami Prefab",
                    "シーン上のAmiをAmi.prefabとして保存します。現在のAmi.prefabは上書きされます。",
                    "Save",
                    "Cancel"))
            {
                return;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(ami, OutputPath);
            if (prefab == null)
            {
                Debug.LogError($"[SaveSceneAmiAsPrefab] Failed to save {OutputPath}.");
                return;
            }

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[SaveSceneAmiAsPrefab] Saved scene Ami as {OutputPath}.");
        }

        private static GameObject FindSceneAmi()
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root != null && (root.name == "///Player(Ami)" || root.name.Contains("Player(Ami)")))
                {
                    return root;
                }
            }

            return null;
        }
    }
}
