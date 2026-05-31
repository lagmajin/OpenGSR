using OpenGS;
using UnityEditor;
using UnityEngine;

namespace OpenGS.EditorTools
{
    [CustomPropertyDrawer(typeof(AutoCreateIfMissingAttribute))]
    public sealed class AutoCreateIfMissingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = (AutoCreateIfMissingAttribute)this.attribute;
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var buttonWidth = 84f;
            var fieldRect = position;
            fieldRect.width = Mathf.Max(0f, position.width - buttonWidth - 4f);
            var buttonRect = position;
            buttonRect.x = fieldRect.xMax + 4f;
            buttonRect.width = buttonWidth;

            if (property.objectReferenceValue == null)
            {
                EditorGUI.PropertyField(fieldRect, property, label, true);

                if (GUI.Button(buttonRect, attribute.ButtonLabel))
                {
                    CreateMissingObject(property, attribute);
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        private static void CreateMissingObject(SerializedProperty property, AutoCreateIfMissingAttribute attribute)
        {
            if (property.serializedObject.targetObject is not Component component)
            {
                Debug.LogWarning($"[AutoCreateIfMissing] {property.propertyPath} target is not a Component.");
                return;
            }

            var owner = component.gameObject;
            var childName = string.IsNullOrWhiteSpace(attribute.ChildName) ? property.name : attribute.ChildName;
            var childTransform = owner.transform.Find(childName);
            var child = childTransform != null ? childTransform.gameObject : null;

            if (child == null)
            {
                child = new GameObject(childName);
                Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
                Undo.SetTransformParent(child.transform, owner.transform, $"Create {childName}");
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = Vector3.one;
                child.SetActive(false);
            }

            if (attribute.ComponentType != null && typeof(Component).IsAssignableFrom(attribute.ComponentType) && child.GetComponent(attribute.ComponentType) == null)
            {
                Undo.AddComponent(child, attribute.ComponentType);
            }

            property.objectReferenceValue = child;
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
            Selection.activeGameObject = child;
            EditorGUIUtility.PingObject(child);
        }
    }
}
