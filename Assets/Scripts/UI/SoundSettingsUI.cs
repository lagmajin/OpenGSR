using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OpenGS
{
    /// <summary>
    /// サウンド設定UIクラス
    /// サウンド設定の表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class SoundSettingsUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("マスター設定")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private Toggle muteAllToggle;
        
        [Header("BGM設定")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private TextMeshProUGUI bgmVolumeText;
        
        [Header("SE設定")]
        [SerializeField] private Slider seVolumeSlider;
        [SerializeField] private TextMeshProUGUI seVolumeText;

        [Header("環境音")]
        [SerializeField] private Toggle reverbToggle;
        
        [Header("ボイス設定")]
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private TextMeshProUGUI voiceVolumeText;
        
        [Header("テスト")]
        [SerializeField] private Button testBGMButton;
        [SerializeField] private Button testSEButton;
        [SerializeField] private Button testVoiceButton;

        // ─── 内部状態 ───────────────────────────────────────────────

        private SoundSettings currentSettings;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            InitializeUI();
            SetupListeners();
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
        }

        private void OnDestroy()
        {
            masterVolumeSlider?.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            muteAllToggle?.onValueChanged.RemoveListener(OnMuteAllChanged);
            bgmVolumeSlider?.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            seVolumeSlider?.onValueChanged.RemoveListener(OnSEVolumeChanged);
            reverbToggle?.onValueChanged.RemoveListener(OnReverbChanged);
            voiceVolumeSlider?.onValueChanged.RemoveListener(OnVoiceVolumeChanged);
            testBGMButton?.onClick.RemoveListener(OnTestBGMClicked);
            testSEButton?.onClick.RemoveListener(OnTestSEClicked);
            testVoiceButton?.onClick.RemoveListener(OnTestVoiceClicked);
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // スライダーの初期化
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.minValue = 0f;
                bgmVolumeSlider.maxValue = 1f;
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.minValue = 0f;
                seVolumeSlider.maxValue = 1f;
            }

            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.minValue = 0f;
                voiceVolumeSlider.maxValue = 1f;
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            // マスター設定
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (muteAllToggle != null)
            {
                muteAllToggle.onValueChanged.AddListener(OnMuteAllChanged);
            }

            // BGM設定
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            // SE設定
            if (seVolumeSlider != null)
            {
                seVolumeSlider.onValueChanged.AddListener(OnSEVolumeChanged);
            }

            if (reverbToggle != null)
            {
                reverbToggle.onValueChanged.AddListener(OnReverbChanged);
            }

            // ボイス設定
            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            }

            // テストボタン
            if (testBGMButton != null)
            {
                testBGMButton.onClick.AddListener(OnTestBGMClicked);
            }

            if (testSEButton != null)
            {
                testSEButton.onClick.AddListener(OnTestSEClicked);
            }

            if (testVoiceButton != null)
            {
                testVoiceButton.onClick.AddListener(OnTestVoiceClicked);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// 設定を適用する
        /// </summary>
        public void ApplySettings()
        {
            if (SettingsManager.Instance == null) return;
            SettingsManager.Instance.ApplySoundSettings(currentSettings);
            Debug.Log("[SoundSettingsUI] サウンド設定を適用しました");
        }

        // ─── 設定読み込み ─────────────────────────────────────────

        /// <summary>
        /// 現在の設定を読み込む
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (SettingsManager.Instance == null) return;
            currentSettings = SettingsManager.Instance.GetSoundSettings();
            UpdateUI();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            currentSettings.MasterVolume = Normalize01(currentSettings.MasterVolume, 1f);
            currentSettings.BGMVolume = Normalize01(currentSettings.BGMVolume, 0.8f);
            currentSettings.SEVolume = Normalize01(currentSettings.SEVolume, 1f);
            currentSettings.VoiceVolume = Normalize01(currentSettings.VoiceVolume, 1f);

            // マスター設定
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = currentSettings.MasterVolume;
            }

            if (masterVolumeText != null)
            {
                masterVolumeText.text = $"{(int)(currentSettings.MasterVolume * 100)}%";
            }

            if (muteAllToggle != null)
            {
                muteAllToggle.isOn = currentSettings.MuteAll;
            }

            // BGM設定
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = currentSettings.BGMVolume;
            }

            if (bgmVolumeText != null)
            {
                bgmVolumeText.text = $"{(int)(currentSettings.BGMVolume * 100)}%";
            }

            // SE設定
            if (seVolumeSlider != null)
            {
                seVolumeSlider.value = currentSettings.SEVolume;
            }

            if (seVolumeText != null)
            {
                seVolumeText.text = $"{(int)(currentSettings.SEVolume * 100)}%";
            }

            if (reverbToggle != null)
            {
                reverbToggle.isOn = currentSettings.Reverb;
            }

            // ボイス設定
            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.value = currentSettings.VoiceVolume;
            }

            if (voiceVolumeText != null)
            {
                voiceVolumeText.text = $"{(int)(currentSettings.VoiceVolume * 100)}%";
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnMasterVolumeChanged(float value)
        {
            currentSettings.MasterVolume = value;

            if (masterVolumeText != null)
            {
                masterVolumeText.text = $"{(int)(value * 100)}%";
            }
        }

        private void OnMuteAllChanged(bool isOn)
        {
            currentSettings.MuteAll = isOn;

            // ミュート時にスライダーを無効化
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.interactable = !isOn;
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.interactable = !isOn;
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.interactable = !isOn;
            }

            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.interactable = !isOn;
            }
        }

        private void OnBGMVolumeChanged(float value)
        {
            currentSettings.BGMVolume = value;

            if (bgmVolumeText != null)
            {
                bgmVolumeText.text = $"{(int)(value * 100)}%";
            }
        }

        private void OnSEVolumeChanged(float value)
        {
            currentSettings.SEVolume = value;

            if (seVolumeText != null)
            {
                seVolumeText.text = $"{(int)(value * 100)}%";
            }
        }

        private void OnReverbChanged(bool isOn)
        {
            currentSettings.Reverb = isOn;
        }

        private void OnVoiceVolumeChanged(float value)
        {
            currentSettings.VoiceVolume = value;

            if (voiceVolumeText != null)
            {
                voiceVolumeText.text = $"{(int)(value * 100)}%";
            }
        }

        private static float Normalize01(float value, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Clamp01(value) : fallback;
        }

        private void OnTestBGMClicked()
        {
            Debug.Log("[SoundSettingsUI] BGMテスト再生");
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlayBGM(EBgm.WaitRoom, 0.15f);
        }

        private void OnTestSEClicked()
        {
            Debug.Log("[SoundSettingsUI] SEテスト再生");
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySystemSound(ESystemSound.Click);
        }

        private void OnTestVoiceClicked()
        {
            Debug.Log("[SoundSettingsUI] ボイステスト再生");
            if (SoundManager.Instance == null) return;
            // 現在のサウンドサービスに独立したボイスカテゴリがないため、
            // 利用可能なプレイヤーボイスで動作確認する。
            SoundManager.Instance.PlayPlayerSound(EPlayerSound.DamageMale1);
        }
    }
}
