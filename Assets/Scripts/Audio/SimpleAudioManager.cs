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

        [Range(0f, 1f)] public float MasterBGMVolume = 1f;
        [Range(0f, 1f)] public float MasterSEVolume = 1f;

        private AudioSource _bgmSource1;
        private AudioSource _bgmSource2;
        private List<AudioSource> _seSources = new List<AudioSource>();
        private const int INITIAL_SE_SOURCES = 5;

        private AudioSource _currentBgmSource;
        private Coroutine _fadeCoroutine;

        private Dictionary<string, AudioConfig.AudioItem> _bgmDict = new Dictionary<string, AudioConfig.AudioItem>();
        private Dictionary<string, AudioConfig.AudioItem> _seDict = new Dictionary<string, AudioConfig.AudioItem>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
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

            for (int i = 0; i < INITIAL_SE_SOURCES; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _seSources.Add(source);
            }

            if (_audioConfig != null)
            {
                foreach (var item in _audioConfig.BGMList) _bgmDict[item.Name] = item;
                foreach (var item in _audioConfig.SEList) _seDict[item.Name] = item;
            }
            
            Debug.Log("[SimpleAudioManager] Initialized with BGM sources and SE pool.");
        }

        public void PlayBGM(string name, float fadeTime = -1)
        {
            if (!_bgmDict.TryGetValue(name, out var item))
            {
                Debug.LogWarning($"[SimpleAudioManager] BGM not found in config: {name}");
                return;
            }
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
            _currentBgmSource.volume = Mathf.Clamp01(volume) * MasterBGMVolume;
            _currentBgmSource.Play();
            
            Debug.Log($"[SimpleAudioManager] BGM Start Playing: {clip.name} (Volume: {_currentBgmSource.volume})");
        }

        public void StopBGM(float fadeTime = -1)
        {
            float time = fadeTime < 0 ? _defaultBgmFadeTime : fadeTime;
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
                elapsed += Time.deltaTime;
                _currentBgmSource.volume = Mathf.Lerp(startVol, 0, elapsed / fadeTime);
                yield return null;
            }

            _currentBgmSource.Stop();
            _currentBgmSource.volume = 0;
        }

        public void PlaySE(string name, float volume = 1.0f, float pitch = 1.0f)
        {
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
        public void SetBGMVolume(float volume) => MasterBGMVolume = Mathf.Clamp01(volume);
        public void SetSEVolume(float volume) => MasterSEVolume = Mathf.Clamp01(volume);
    }
}
