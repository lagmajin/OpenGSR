using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGSR.Audio
{
    public class SimpleAudioManager : MonoBehaviour
    {
        private static SimpleAudioManager _instance;
        public static SimpleAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SimpleAudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SimpleAudioManager");
                        _instance = go.AddComponent<SimpleAudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private AudioConfig _audioConfig;
        [SerializeField] private float _defaultBgmFadeTime = 1.0f;
        [SerializeField] private bool _reverbEnabled = false;

        [Range(0f, 1f)] public float MasterBGMVolume = 1f;
        [Range(0f, 1f)] public float MasterSEVolume = 1f;

        private AudioSource _bgmSource1;
        private AudioSource _bgmSource2;
        private AudioReverbFilter _reverbFilter;
        private List<AudioSource> _seSources = new List<AudioSource>();
        private const int INITIAL_SE_SOURCES = 5;

        private AudioSource _currentBgmSource;
        private float _currentBgmBaseVolume = 1f;
        private Coroutine _fadeCoroutine;
        private string _currentBgmName;

        private Dictionary<string, AudioConfig.AudioItem> _bgmDict = new Dictionary<string, AudioConfig.AudioItem>();
        private Dictionary<string, AudioConfig.AudioItem> _seDict = new Dictionary<string, AudioConfig.AudioItem>();

        private void OnValidate()
        {
            if (!float.IsFinite(_defaultBgmFadeTime)) _defaultBgmFadeTime = 1f;
            _defaultBgmFadeTime = Mathf.Max(0f, _defaultBgmFadeTime);
            MasterBGMVolume = float.IsFinite(MasterBGMVolume) ? Mathf.Clamp01(MasterBGMVolume) : 1f;
            MasterSEVolume = float.IsFinite(MasterSEVolume) ? Mathf.Clamp01(MasterSEVolume) : 1f;
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
            _defaultBgmFadeTime = float.IsFinite(_defaultBgmFadeTime) ? Mathf.Max(0f, _defaultBgmFadeTime) : 1f;
            MasterBGMVolume = float.IsFinite(MasterBGMVolume) ? Mathf.Clamp01(MasterBGMVolume) : 1f;
            MasterSEVolume = float.IsFinite(MasterSEVolume) ? Mathf.Clamp01(MasterSEVolume) : 1f;
            Init();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Init()
        {
            _bgmSource1 = gameObject.AddComponent<AudioSource>();
            _bgmSource2 = gameObject.AddComponent<AudioSource>();
            _bgmSource1.loop = true;
            _bgmSource2.loop = true;
            _bgmSource1.playOnAwake = false;
            _bgmSource2.playOnAwake = false;
            _currentBgmSource = _bgmSource1;
            // AudioReverbFilter は環境によって初期化時に警告や例外を出すことがあるため、
            // 既に付いている場合だけ制御し、新規追加はしない。
            _reverbFilter = gameObject.GetComponent<AudioReverbFilter>();
            ApplyReverbEnabled(_reverbEnabled);

            if (_audioConfig == null)
            {
                _audioConfig = Resources.Load<AudioConfig>("AudioConfig");
            }

            for (int i = 0; i < INITIAL_SE_SOURCES; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _seSources.Add(source);
            }

            if (_audioConfig != null)
            {
                if (_audioConfig.BGMList != null)
                {
                    foreach (var item in _audioConfig.BGMList)
                    {
                        if (item != null && !string.IsNullOrWhiteSpace(item.Name))
                        {
                            _bgmDict[item.Name] = item;
                        }
                    }
                }

                if (_audioConfig.SEList != null)
                {
                    foreach (var item in _audioConfig.SEList)
                    {
                        if (item != null && !string.IsNullOrWhiteSpace(item.Name))
                        {
                            _seDict[item.Name] = item;
                        }
                    }
                }
            }
            
            Debug.Log("[SimpleAudioManager] Initialized with BGM sources and SE pool.");
        }

        public void PlayBGM(string name, float fadeTime = -1)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning("[SimpleAudioManager] Ignoring empty BGM name.");
                return;
            }

            name = name.Trim();
            if (!_bgmDict.TryGetValue(name, out var item))
            {
                Debug.LogWarning($"[SimpleAudioManager] BGM not found in config: {name}");
                return;
            }
            _currentBgmName = name;
            PlayBGM(item.Clip, item.Volume, true);
        }

        public void PlayBGM(AudioClip clip, float volume = 1.0f, bool loop = true)
        {
            if (clip == null)
            {
                Debug.LogWarning("[SimpleAudioManager] PlayBGM called with null clip.");
                return;
            }

            // オーディオ環境のチェック
            if (FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                Debug.LogError("[SimpleAudioManager] CRITICAL: No AudioListener found in the scene! Sound will not be heard.");
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            
            _currentBgmSource.clip = clip;
            _currentBgmSource.loop = loop;
            _currentBgmBaseVolume = float.IsFinite(volume) ? Mathf.Clamp01(volume) : 1f;
            _currentBgmSource.volume = _currentBgmBaseVolume * MasterBGMVolume;
            _currentBgmSource.Play();
            if (string.IsNullOrWhiteSpace(_currentBgmName))
            {
                _currentBgmName = clip.name;
            }
            
            Debug.Log($"[SimpleAudioManager] BGM Start Playing: {clip.name} (Volume: {_currentBgmSource.volume})");
        }

        public void StopBGM(float fadeTime = -1)
        {
            if (!float.IsFinite(fadeTime)) fadeTime = -1f;
            float time = fadeTime < 0 ? _defaultBgmFadeTime : fadeTime;
            time = Mathf.Max(0f, time);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (time > 0)
                _fadeCoroutine = StartCoroutine(FadeOutBGM(time));
            else
                _currentBgmSource.Stop();
        }

        private IEnumerator FadeOutBGM(float fadeTime)
        {
            float elapsed = 0;
            float startVol = _currentBgmSource.volume;

            while (elapsed < fadeTime)
            {
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    break;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                elapsed = Mathf.Min(fadeTime, elapsed + deltaTime);
                _currentBgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeTime);
                yield return null;
            }

            _currentBgmSource.Stop();
            _currentBgmName = null;
            _currentBgmSource.volume = 0;
            _fadeCoroutine = null;
        }

        public void PlaySE(string name, float volume = 1.0f, float pitch = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning("[SimpleAudioManager] Ignoring empty SE name.");
                return;
            }

            name = name.Trim();
            if (_seDict.TryGetValue(name, out var item))
            {
                PlaySE(item.Clip, item.Volume * volume, pitch);
            }
            else
            {
                Debug.LogWarning($"[SimpleAudioManager] SE not found in config: {name}");
            }
        }

        public void PlaySE(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (clip == null) return;

            volume = float.IsFinite(volume) ? Mathf.Clamp01(volume) : 1f;
            pitch = float.IsFinite(pitch) ? Mathf.Clamp(pitch, 0.1f, 3f) : 1f;

            AudioSource source = GetAvailableSESource();
            if (source != null)
            {
                source.pitch = pitch;
                source.PlayOneShot(clip, volume * MasterSEVolume);
            }
        }

        private AudioSource GetAvailableSESource()
        {
            foreach (var source in _seSources)
            {
                if (!source.isPlaying) return source;
            }
            // 足りなければ追加
            var newSource = gameObject.AddComponent<AudioSource>();
            _seSources.Add(newSource);
            return newSource;
        }

        public bool IsPlayingBGM() => _currentBgmSource != null && _currentBgmSource.isPlaying;
        public bool IsPlayingBGM(string name)
        {
            return _currentBgmSource != null
                && _currentBgmSource.isPlaying
                && (
                    string.Equals(_currentBgmName, name, System.StringComparison.OrdinalIgnoreCase)
                    || (_currentBgmSource.clip != null && string.Equals(_currentBgmSource.clip.name, name, System.StringComparison.OrdinalIgnoreCase))
                );
        }
        public void SetCurrentBGMName(string name) => _currentBgmName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        public void SetBGMVolume(float volume)
        {
            MasterBGMVolume = float.IsFinite(volume) ? Mathf.Clamp01(volume) : 1f;
            if (_currentBgmSource != null && _currentBgmSource.isPlaying)
            {
                _currentBgmSource.volume = _currentBgmBaseVolume * MasterBGMVolume;
            }
        }
        public void SetSEVolume(float volume) => MasterSEVolume = float.IsFinite(volume) ? Mathf.Clamp01(volume) : 1f;
        public void SetReverbEnabled(bool enabled)
        {
            _reverbEnabled = enabled;
            ApplyReverbEnabled(enabled);
        }

        private void ApplyReverbEnabled(bool enabled)
        {
            if (_reverbFilter != null)
            {
                _reverbFilter.enabled = enabled;
            }
        }
    }
}
