using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// BGM のリストを管理するマスターデータクラス。
    /// 名前（キー）と AudioClip を紐付けて登録する。
    /// </summary>
    [CreateAssetMenu(menuName = "MasterData/Sound/BGMMasterData")]
    public class BGMMasterData : ScriptableObject
    {
        [Serializable]
        public struct BGMMapping
        {
            public string name;
            public AudioClip clip;
        }

        [SerializeField] private List<BGMMapping> bgmList = new List<BGMMapping>();

        private readonly Dictionary<string, AudioClip> bgmMap = new Dictionary<string, AudioClip>();

        private void OnEnable()
        {
            RebuildMap();
        }

        public void RebuildMap()
        {
            bgmMap.Clear();
            foreach (var item in bgmList)
            {
                if (!string.IsNullOrEmpty(item.name))
                {
                    bgmMap[item.name] = item.clip;
                }
            }
        }

        public bool TryGetBGM(string name, out AudioClip clip)
        {
            if (bgmMap.Count == 0) RebuildMap();
            return bgmMap.TryGetValue(name, out clip) && clip != null;
        }

        public List<string> GetAllBgmNames()
        {
            return new List<string>(bgmMap.Keys);
        }
    }
}
