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
            _currentBgmSource = _bgmSource1;

            for (int i = 0; i < INITIAL_SE_SOURCES; i++)
            {
                _seSources.Add(gameObject.AddComponent<AudioSource>());
            }

            if (_audioConfig != null)
            {
                foreach (var item in _audioConfig.BGMList) _bgmDict[item.Name] = item;
                foreach (var item in _audioConfig.SEList) _seDict[item.Name] = item;
            }
        }

        // =====================================
        // BGM Methods
        // =====================================
        public void PlayBGM(string name, float fadeTime = -1)
        {
            if (!_bgmDict.TryGetValue(name, out var item))
            {
                Debug.LogWarning($"[SimpleAudioManager] BGM not found: {name}");
                return;
            }
            if (_currentBgmSource.clip == item.Clip && _currentBgmSource.isPlaying) return;

            float time = fadeTime < 0 ? _defaultBgmFadeTime : fadeTime;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossFadeBGM(item, time));
        }

        public void StopBGM(float fadeTime = -1)
        {
            float time = fadeTime < 0 ? _defaultBgmFadeTime : fadeTime;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutBGM(time));
        }

        public void PauseBGM() => _currentBgmSource.Pause();
        public void ResumeBGM() => _currentBgmSource.UnPause();
        public bool IsPlayingBGM() => _currentBgmSource != null && _currentBgmSource.isPlaying;

        public void PlayBGM(AudioClip clip, float volume = 1.0f, bool loop = true)
        {
            if (clip == null) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _currentBgmSource.clip = clip;
            _currentBgmSource.loop = loop;
            _currentBgmSource.volume = Mathf.Clamp01(volume) * MasterBGMVolume;
            _currentBgmSource.Play();
        }

        private IEnumerator CrossFadeBGM(AudioConfig.AudioItem nextItem, float fadeTime)
        {
            AudioSource nextSource = (_currentBgmSource == _bgmSource1) ? _bgmSource2 : _bgmSource1;
            nextSource.clip = nextItem.Clip;
            nextSource.volume = 0;
            nextSource.Play();

            float elapsed = 0;
            float startVol = _currentBgmSource.volume;
            float targetVol = nextItem.Volume * MasterBGMVolume;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                _currentBgmSource.volume = Mathf.Lerp(startVol, 0, t);
                nextSource.volume = Mathf.Lerp(0, targetVol, t);
                yield return null;
            }

            _currentBgmSource.Stop();
            _currentBgmSource.volume = 0;
            nextSource.volume = targetVol;
            _currentBgmSource = nextSource;
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

        // =====================================
        // SE Methods
        // =====================================
        public void PlaySE(string name, float pitch = 1.0f)
        {
            if (!_seDict.TryGetValue(name, out var item))
            {
                Debug.LogWarning($"[SimpleAudioManager] SE not found: {name}");
                return;
            }

            AudioSource source = GetFreeSESource();
            source.pitch = pitch;
            source.PlayOneShot(item.Clip, item.Volume * MasterSEVolume);
        }

        public void PlaySE(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (clip == null) return;
            AudioSource source = GetFreeSESource();
            source.pitch = pitch;
            source.PlayOneShot(clip, volume * MasterSEVolume);
        }

        public void StopAllSE()
        {
            foreach (var s in _seSources) s.Stop();
        }

        private AudioSource GetFreeSESource()
        {
            foreach (var s in _seSources)
            {
                if (!s.isPlaying) return s;
            }
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            _seSources.Add(newSource);
            return newSource;
        }

        // =====================================
        // Settings
        // =====================================
        public void SetBGMVolume(float volume)
        {
            MasterBGMVolume = Mathf.Clamp01(volume);
            if (_currentBgmSource.isPlaying)
            {
                _currentBgmSource.volume = MasterBGMVolume;
            }
        }

        public void SetSEVolume(float volume)
        {
            MasterSEVolume = Mathf.Clamp01(volume);
        }
    }
}
