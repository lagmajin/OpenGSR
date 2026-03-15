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
    }
}
