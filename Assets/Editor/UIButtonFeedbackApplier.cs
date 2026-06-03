using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS.EditorTools
{
    public static class UIButtonFeedbackApplier
    {
        [MenuItem("OpenGSR/Tools/Apply UIButton Feedback To Selection")]
        public static void ApplyToSelection()
        {
            var targets = Selection.gameObjects;
            if (targets == null || targets.Length == 0)
            {
                Debug.LogWarning("[UIButtonFeedbackApplier] No selected GameObjects.");
                return;
            }

            var processed = new HashSet<GameObject>();
            var addedCount = 0;

            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                foreach (var button in target.GetComponentsInChildren<Button>(true))
                {
                    if (button == null || !processed.Add(button.gameObject))
                    {
                        continue;
                    }

                    if (button.GetComponent<UIButtonFeedback>() != null)
                    {
                        continue;
                    }

                    Undo.AddComponent<UIButtonFeedback>(button.gameObject);
                    addedCount++;
                }
            }

            Debug.Log($"[UIButtonFeedbackApplier] Added UIButtonFeedback to {addedCount} button object(s).");
        }

        [MenuItem("OpenGSR/Tools/Apply UIButton Feedback To Selection", true)]
        private static bool ValidateApplyToSelection()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }
    }
}
