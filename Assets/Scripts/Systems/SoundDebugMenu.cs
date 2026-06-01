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
        [SerializeField] private EPlayerSound playerSoundPreview = EPlayerSound.DamageMale1;
        [SerializeField] private EPlayerGeneralSound playerGeneralSoundPreview = EPlayerGeneralSound.JumpStart;
        [SerializeField] private EGrenadeSound grenadeSoundPreview = EGrenadeSound.ExplosionGrenade;
        [SerializeField] private ETakeItemSound takeItemSoundPreview = ETakeItemSound.TakeHealItemSound;

        private string lastReport = string.Empty;

        private void Start()
        {
            if (!validateOnStart)
            {
                return;
            }

            bool valid = SoundManager.Instance.ValidateSoundSetup(true);
            bool playerGeneralValid = ValidatePlayerGeneralSoundSetup();
            if (!valid)
            {
                Debug.LogWarning("[SoundDebugMenu] Sound mappings have missing entries. See logs for details.");
            }

            if (!playerGeneralValid)
            {
                Debug.LogWarning("[SoundDebugMenu] PlayerGeneralSound mappings have missing entries. See logs for details.");
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

            var area = new Rect(10f, 10f, 360f, 375f);
            GUILayout.BeginArea(area, "Sound Debug Menu", GUI.skin.window);

            GUILayout.Label($"Toggle: {toggleOverlayKey}");
            GUILayout.Space(6f);

            if (GUILayout.Button("Validate Sound Setup"))
            {
                bool valid = SoundManager.Instance.ValidateSoundSetup(true);
                lastReport = valid ? "Validate: OK" : "Validate: Missing mappings";
            }

            GUILayout.Space(6f);

            GUILayout.Label($"System: {SoundVisualResolver.GetSystemDisplayName(systemSoundPreview)}");
            if (GUILayout.Button("Play System Preview"))
            {
                SoundManager.Instance.PlaySystemSound(systemSoundPreview);
            }

            GUILayout.Label($"Match: {SoundVisualResolver.GetMatchDisplayName(matchSoundPreview)}");
            if (GUILayout.Button("Play Match Preview"))
            {
                SoundManager.Instance.PlayGameSound(matchSoundPreview);
            }

            GUILayout.Label($"Effect: {SoundVisualResolver.GetEffectDisplayName(effectSoundPreview)}");
            if (GUILayout.Button("Play Effect Preview"))
            {
                SoundManager.Instance.PlaySoundEffect(effectSoundPreview);
            }

            GUILayout.Label($"Player: {SoundVisualResolver.GetPlayerDisplayName(playerSoundPreview)}");
            if (GUILayout.Button("Play Player Preview"))
            {
                SoundManager.Instance.PlayPlayerSound(playerSoundPreview);
            }

            GUILayout.Label($"PlayerGeneral: {SoundVisualResolver.GetPlayerGeneralDisplayName(playerGeneralSoundPreview)}");
            if (GUILayout.Button("Play PlayerGeneral Preview"))
            {
                if (TryPlayPlayerGeneralPreview(playerGeneralSoundPreview))
                {
                    lastReport = $"Played: {SoundVisualResolver.GetPlayerGeneralDisplayName(playerGeneralSoundPreview)}";
                }
            }

            GUILayout.Label($"Grenade: {SoundVisualResolver.GetGrenadeDisplayName(grenadeSoundPreview)}");
            if (GUILayout.Button("Play Grenade Explosion Preview"))
            {
                if (TryPlayGrenadeExplosionPreview(grenadeSoundPreview))
                {
                    lastReport = $"Played: {SoundVisualResolver.GetGrenadeDisplayName(grenadeSoundPreview)}";
                }
            }

            GUILayout.Label($"TakeItem: {SoundVisualResolver.GetTakeItemDisplayName(takeItemSoundPreview)}");
            if (GUILayout.Button("Play TakeItem Preview"))
            {
                SoundManager.Instance.PlayTakeItemSound(takeItemSoundPreview);
            }

            if (!string.IsNullOrWhiteSpace(lastReport))
            {
                GUILayout.Space(6f);
                GUILayout.Label(lastReport);
            }

            GUILayout.EndArea();
        }

        private bool TryPlayPlayerGeneralPreview(EPlayerGeneralSound sound)
        {
            var masterData = Resources.Load<PlayerGeneralSoundMasterData>("MasterData/Sound/Players/PlayerGeneralSound");
            if (masterData == null)
            {
                Debug.LogWarning("[SoundDebugMenu] PlayerGeneralSoundMasterData not found.");
                return false;
            }

            if (!masterData.TryGetSound(sound, out var clip) || clip == null)
            {
                Debug.LogWarning($"[SoundDebugMenu] Player general sound missing: {sound}");
                return false;
            }

            SoundManager.Instance.PlayOneShotSafe(clip, 1.0f, 1.0f, $"SoundDebugMenu:{sound}", true);
            return true;
        }

        private bool ValidatePlayerGeneralSoundSetup()
        {
            var masterData = Resources.Load<PlayerGeneralSoundMasterData>("MasterData/Sound/Players/PlayerGeneralSound");
            if (masterData == null)
            {
                return false;
            }

            return masterData.ValidateAllMappings(true);
        }

        private bool TryPlayGrenadeExplosionPreview(EGrenadeSound sound)
        {
            var clip = SoundVisualResolver.GetGrenadeExplosionClip(sound);
            if (clip == null)
            {
                Debug.LogWarning($"[SoundDebugMenu] Grenade explosion sound missing: {sound}");
                return false;
            }

            SoundManager.Instance.PlayOneShotSafe(clip, 1.0f, 1.0f, $"SoundDebugMenu:{sound}", true);
            return true;
        }
    }
}
