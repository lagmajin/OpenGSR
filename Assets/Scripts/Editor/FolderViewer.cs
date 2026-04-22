using UnityEngine;

using UnityEditor;


public class FolderViewer : EditorWindow
{
    private string folderPath = "Assets/Sprites"; // 好きなフォルダに変えてOK
    private Vector2 scrollPos;

    [MenuItem("Tools/Folder Viewer")]
    public static void ShowWindow()
    {
        GetWindow<FolderViewer>("Fixed Folder");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("📁 Showing: " + folderPath, EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        string[] assetGUIDs = AssetDatabase.FindAssets("", new[] { folderPath });

        foreach (string guid in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                if (GUILayout.Button(asset.name, GUILayout.Height(20)))
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}