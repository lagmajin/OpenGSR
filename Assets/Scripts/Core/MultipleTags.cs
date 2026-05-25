using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    public enum eMajorTag
    {
        StageObject,
        Player,
        Grenade,
        Waterfall,
    }

    [DisallowMultipleComponent]
    public class MultipleTags : MonoBehaviour, IMultipleTags
    {
        [SerializeField] public List<string> tags;

        private void Awake()
        {
            tags ??= new List<string>();
        }

        private void Reset()
        {
            tags ??= new List<string>();
        }

        public bool Contains(string str)
        {
            return tags != null && !string.IsNullOrWhiteSpace(str) && tags.Contains(str);
        }

        public bool Contains(eMajorTag tag)
        {
            return Contains(tag.ToString());
        }

        public bool HasPlayerTag() => Contains("Player");
        public bool HasBotTag() => Contains("Bot");
        public bool HasBurstAreaTag() => Contains("BurstArea");
        public bool HasWaterFallTag() => Contains("WaterFall");
        public bool HasLavaTag() => Contains("Lava");
        public bool HasEnemyTag() => Contains("Enemy");
        public bool HasStageObjectTag() => Contains("StageObject");
        public bool HasPlayerAndEnemyTags() => HasPlayerTag() && HasEnemyTag();
        public bool HasGrenadeTag() => Contains("Grenade");
        public bool HasLightTag() => Contains("Light");
        public bool HasWallTag() => Contains("Wall");
        public bool HasFieldItemTag() => Contains("FieldItem");
        public bool HasFieldWeaponTag() => Contains("FieldWeapon");
        public bool HasEnemyAttackTag() => Contains("EnemyAttack");

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            tags ??= new List<string>();
            if (!Contains(tag))
            {
                tags.Add(tag);
                tags = tags.Distinct().ToList();
            }

            Debug.Log($"[MultipleTags] Added tag: {tag}");
        }

        public void AddTag(eMajorTag tag) => AddTag(tag.ToString());

        public void RemoveFront()
        {
            if (tags == null || tags.Count == 0)
            {
                return;
            }

            tags.RemoveAt(0);
        }

        public void RemoveEnd()
        {
            if (tags == null || tags.Count == 0)
            {
                return;
            }

            tags.RemoveAt(tags.Count - 1);
        }

        public void RemoveTag(string str)
        {
            if (tags == null || string.IsNullOrWhiteSpace(str))
            {
                return;
            }

            tags.RemoveAll(tag => string.Equals(tag, str, StringComparison.OrdinalIgnoreCase));
        }

        [Button("全てのタグをクリア")]
        public void RemoveAll()
        {
            tags?.Clear();
        }

        public List<string> AllTagsToString()
        {
            return tags != null ? new List<string>(tags) : new List<string>();
        }

        public JObject ToJson()
        {
            var result = new JObject();
            if (tags == null)
            {
                return result;
            }

            foreach (var item in tags.Select((value, index) => new { value, index }))
            {
                result[$"tag{item.index}"] = item.value;
            }

            return result;
        }

        public void PrintUnityDebugLog()
        {
            Debug.Log($"[MultipleTags] {name}: {(tags == null ? "" : string.Join(",", tags))}");
        }

        public override bool Equals(object obj)
        {
            return obj is MultipleTags other && ReferenceEquals(this, other);
        }

        public bool HasMyPlayerTag() => Contains("MyPlayer");

        [Button("プラットフォームタグを付加")]
        public void AddPlatformTag() => AddTag("Platform");

        [Button("プレイヤータグ")]
        public void AddPlayerTag() => AddTag("Player");

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(name);
            hash.Add(hideFlags);
            hash.Add(transform);
            hash.Add(gameObject);
            hash.Add(tag);
            return hash.ToHashCode();
        }

        public static bool operator ==(MultipleTags t1, MultipleTags t2)
        {
            if (ReferenceEquals(t1, t2))
            {
                return true;
            }

            if (t1 is null || t2 is null)
            {
                return false;
            }

            return t1.tags == t2.tags;
        }

        public static bool operator !=(MultipleTags t1, MultipleTags t2) => !(t1 == t2);
    }
}
