using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public sealed class GlobalVolumeHotkeyController : MonoBehaviour
    {
        [SerializeField] private float volumeStep = 0.05f;
        [SerializeField] private float overlayVisibleSeconds = 1.25f;

        private static GlobalVolumeHotkeyController _instance;
        private Canvas overlayCanvas;
        private Text overlayText;
        private Coroutine hideOverlayCoroutine;
        private bool isSubscribedToSettingsChanges;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var existing = FindFirstObjectByType<GlobalVolumeHotkeyController>();
            if (existing != null)
            {
                _instance = existing;
                DontDestroyOnLoad(existing.gameObject);
                return;
            }

            var go = new GameObject(nameof(GlobalVolumeHotkeyController));
            _instance = go.AddComponent<GlobalVolumeHotkeyController>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlayIfNeeded();
            SubscribeToSettings();
            RefreshOverlayText(false);
        }

        private void OnDestroy()
        {
            UnsubscribeFromSettings();
        }

        private void Update()
        {
            if (IsTypingInInputField())
            {
                return;
            }

            var delta = 0f;
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                delta += volumeStep;
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                delta -= volumeStep;
            }

            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            AdjustMasterVolume(delta);
        }

        private void AdjustMasterVolume(float delta)
        {
            var settingsManager = SettingsManager.Instance;
            var soundSettings = settingsManager.GetSoundSettings();
            if (soundSettings == null)
            {
                return;
            }

            soundSettings.MasterVolume = Mathf.Clamp01(soundSettings.MasterVolume + delta);
            settingsManager.ApplySoundSettings(soundSettings);

            Debug.Log($"[GlobalVolumeHotkeyController] MasterVolume -> {(int)(soundSettings.MasterVolume * 100)}%");
            RefreshOverlayText(true);
        }

        private void SubscribeToSettings()
        {
            if (isSubscribedToSettingsChanges)
            {
                return;
            }

            SettingsManager.Instance.OnSoundSettingsChanged += HandleSoundSettingsChanged;
            isSubscribedToSettingsChanges = true;
        }

        private void UnsubscribeFromSettings()
        {
            if (!isSubscribedToSettingsChanges)
            {
                return;
            }

            SettingsManager.Instance.OnSoundSettingsChanged -= HandleSoundSettingsChanged;
            isSubscribedToSettingsChanges = false;
        }

        private void HandleSoundSettingsChanged(SoundSettings _)
        {
            RefreshOverlayText(false);
        }

        private void CreateOverlayIfNeeded()
        {
            if (overlayCanvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("GlobalVolumeOverlay");
            canvasGo.transform.SetParent(transform, false);

            overlayCanvas = canvasGo.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = short.MaxValue;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("VolumeText");
            textGo.transform.SetParent(canvasGo.transform, false);

            overlayText = textGo.AddComponent<Text>();
            overlayText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            overlayText.fontSize = 14;
            overlayText.alignment = TextAnchor.UpperLeft;
            overlayText.color = new Color(1f, 1f, 1f, 0.9f);
            overlayText.raycastTarget = false;
            overlayText.text = "";

            var rect = overlayText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(14f, -14f);
            rect.sizeDelta = new Vector2(260f, 24f);
        }

        private void RefreshOverlayText(bool forceShow)
        {
            if (overlayText == null)
            {
                return;
            }

            var soundSettings = SettingsManager.Instance.GetSoundSettings();
            var masterVolume = soundSettings != null ? soundSettings.MasterVolume : 1f;
            var muteSuffix = soundSettings != null && soundSettings.MuteAll ? " (Muted)" : "";
            overlayText.text = $"Master Volume: {(int)(masterVolume * 100)}%{muteSuffix}";
            overlayText.gameObject.SetActive(true);

            if (hideOverlayCoroutine != null)
            {
                StopCoroutine(hideOverlayCoroutine);
            }

            hideOverlayCoroutine = StartCoroutine(HideOverlayAfterDelay(forceShow ? overlayVisibleSeconds : 0.4f));
        }

        private IEnumerator HideOverlayAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (overlayText != null)
            {
                overlayText.gameObject.SetActive(false);
            }
        }

        private static bool IsTypingInInputField()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
            {
                return false;
            }

            return selected.GetComponent<TMP_InputField>() != null
                || selected.GetComponent<InputField>() != null;
        }
    }
}
