using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// BGM のリストを管理するマスターデータクラス。
    /// EBgm 列挙型と AudioClip を紐付けて登録する。
    /// </summary>
    [CreateAssetMenu(menuName = "MasterData/Sound/BGMMasterData")]
    public class BGMMasterData : ScriptableObject
    {
        [Serializable]
        public struct BGMMapping
        {
            public EBgm bgm;
            public AudioClip clip;
        }

        [SerializeField] private List<BGMMapping> bgmList = new List<BGMMapping>();

        private readonly Dictionary<EBgm, AudioClip> bgmMap = new Dictionary<EBgm, AudioClip>();

        private void OnEnable()
        {
            RebuildMap();
        }

        public void RebuildMap()
        {
            bgmMap.Clear();
            foreach (var item in bgmList)
            {
                bgmMap[item.bgm] = item.clip;
            }
        }

        public bool TryGetBGM(EBgm bgm, out AudioClip clip)
        {
            if (bgmMap.Count == 0) RebuildMap();
            return bgmMap.TryGetValue(bgm, out clip) && clip != null;
        }

        /// <summary>
        /// 文字列名からの取得も互換性のために残す
        /// </summary>
        public bool TryGetBGMByName(string name, out AudioClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(name)) return false;

            if (Enum.TryParse<EBgm>(name, true, out var result))
            {
                return TryGetBGM(result, out clip);
            }
            return false;
        }

        public int PreloadAll()
        {
            RebuildMap();

            var loadedCount = 0;
            foreach (EBgm bgm in Enum.GetValues(typeof(EBgm)))
            {
                if (bgm == EBgm.None)
                {
                    continue;
                }

                if (bgmMap.TryGetValue(bgm, out var existing) && existing != null)
                {
                    continue;
                }

                if (TryGetBGMByName(bgm.ToString(), out var loaded) && loaded != null)
                {
                    bgmMap[bgm] = loaded;
                    loadedCount++;
                }
            }

            return loadedCount;
        }

        public bool ValidateAllMappings(bool logWarnings = true)
        {
            RebuildMap();

            var warnings = new List<string>();
            foreach (EBgm bgm in Enum.GetValues(typeof(EBgm)))
            {
                if (bgm == EBgm.None)
                {
                    continue;
                }

                if (!TryGetBGM(bgm, out var clip) || clip == null)
                {
                    warnings.Add($"BGM missing: {bgm}");
                }
            }

            if (logWarnings)
            {
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"[BGMMasterData] {warning}");
                }
            }

            return warnings.Count == 0;
        }
    }
}
