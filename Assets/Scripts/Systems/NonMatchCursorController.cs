using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class NonMatchCursorController : MonoBehaviour
    {
        public enum CursorControlMode
        {
            AutoBySceneName,
            ForceEnabled,
            ForceDisabled
        }

        [SerializeField] private CursorControlMode mode = CursorControlMode.AutoBySceneName;
        [SerializeField] private string[] matchSceneKeywords = { "Match", "Battle" };
        [SerializeField] private bool applyEveryFrame = true;
        [SerializeField] private bool affectMatchScene = false;

        [Header("Non-Match Cursor")]
        [SerializeField] private bool nonMatchVisible = true;
        [SerializeField] private CursorLockMode nonMatchLockMode = CursorLockMode.None;
        [SerializeField] private Texture2D nonMatchCursorTexture;
        [SerializeField] private Vector2 nonMatchCursorHotspot = Vector2.zero;
        [SerializeField] private CursorMode nonMatchCursorMode = CursorMode.Auto;

        private bool previousVisible;
        private CursorLockMode previousLockMode;
        private bool capturedPreviousState;

        private void OnEnable()
        {
            CaptureCurrentState();
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshCursorState();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestorePreviousState();
        }

        private void Update()
        {
            if (applyEveryFrame)
            {
                RefreshCursorState();
            }
        }

        private void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            RefreshCursorState();
        }

        private void RefreshCursorState()
        {
            bool shouldEnable = mode switch
            {
                CursorControlMode.ForceEnabled => true,
                CursorControlMode.ForceDisabled => false,
                _ => IsNonMatchScene(SceneManager.GetActiveScene().name)
            };

            if (shouldEnable)
            {
                Cursor.visible = nonMatchVisible;
                Cursor.lockState = nonMatchLockMode;
                Cursor.SetCursor(nonMatchCursorTexture, nonMatchCursorHotspot, nonMatchCursorMode);
                return;
            }

            if (affectMatchScene)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private bool IsNonMatchScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return true;
            }

            if (matchSceneKeywords == null || matchSceneKeywords.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < matchSceneKeywords.Length; i++)
            {
                string keyword = matchSceneKeywords[i];
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                if (sceneName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void CaptureCurrentState()
        {
            previousVisible = Cursor.visible;
            previousLockMode = Cursor.lockState;
            capturedPreviousState = true;
        }

        private void RestorePreviousState()
        {
            if (!capturedPreviousState)
            {
                return;
            }

            Cursor.visible = previousVisible;
            Cursor.lockState = previousLockMode;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
