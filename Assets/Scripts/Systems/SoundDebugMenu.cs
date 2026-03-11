using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class SoundDebugMenu : MonoBehaviour
    {
        [SerializeField] private bool validateOnStart = true;
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private KeyCode toggleOverlayKey = KeyCode.F9;

        [SerializeField] private ESystemSound systemSoundPreview = ESystemSound.Click;
        [SerializeField] private EMatchSound matchSoundPreview = EMatchSound.GameStartVoice;
        [SerializeField] private ESoundEffect effectSoundPreview = ESoundEffect.Explosion;

        private string lastReport = string.Empty;

        private void Start()
        {
            if (!validateOnStart)
            {
                return;
            }

            bool valid = SoundManager.Instance.ValidateSoundSetup(true);
            if (!valid)
            {
                Debug.LogWarning("[SoundDebugMenu] Sound mappings have missing entries. See logs for details.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleOverlayKey))
            {
                showOverlay = !showOverlay;
            }
        }

        private void OnGUI()
        {
            if (!showOverlay)
            {
                return;
            }

            var area = new Rect(10f, 10f, 360f, 240f);
            GUILayout.BeginArea(area, "Sound Debug Menu", GUI.skin.window);

            GUILayout.Label($"Toggle: {toggleOverlayKey}");
            GUILayout.Space(6f);

            if (GUILayout.Button("Validate Sound Setup"))
            {
                bool valid = SoundManager.Instance.ValidateSoundSetup(true);
                lastReport = valid ? "Validate: OK" : "Validate: Missing mappings";
            }

            GUILayout.Space(6f);

            GUILayout.Label($"System: {systemSoundPreview}");
            if (GUILayout.Button("Play System Preview"))
            {
                SoundManager.Instance.PlaySystemSound(systemSoundPreview);
            }

            GUILayout.Label($"Match: {matchSoundPreview}");
            if (GUILayout.Button("Play Match Preview"))
            {
                SoundManager.Instance.PlayGameSound(matchSoundPreview);
            }

            GUILayout.Label($"Effect: {effectSoundPreview}");
            if (GUILayout.Button("Play Effect Preview"))
            {
                SoundManager.Instance.PlaySoundEffect(effectSoundPreview);
            }

            if (!string.IsNullOrWhiteSpace(lastReport))
            {
                GUILayout.Space(6f);
                GUILayout.Label(lastReport);
            }

            GUILayout.EndArea();
        }
    }
}
