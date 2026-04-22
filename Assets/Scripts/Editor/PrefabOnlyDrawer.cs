using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenGS
{
    [CustomPropertyDrawer(typeof(PrefabOnlyAttribute))]
    public class PrefabOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
        
            // オブジェクトフィールドの描画
            Object oldValue = property.objectReferenceValue;
            Object newValue = EditorGUI.ObjectField(position, label, oldValue, typeof(GameObject), false);

            if (newValue != null)
            {
                string path = AssetDatabase.GetAssetPath(newValue);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // プレハブのみ許可
                if (PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab)
                {
                    property.objectReferenceValue = newValue;
                }
                else
                {
                    Debug.LogWarning("Only prefabs are allowed.");
                }
            }
            else
            {
                property.objectReferenceValue = null;
            }

            EditorGUI.EndProperty();
        }
    }
    
    
}