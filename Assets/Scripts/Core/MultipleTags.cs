

using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using Sirenix.OdinInspector;
using System;

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
        public List<string> tags;

        public bool Contains(string str)
        {
            return tags.Contains(str);
        }

        public bool Contains(eMajorTag tag)
        {
            return Contains(tag.ToString());
        }

        public bool HasPlayerTag()
        {
            return Contains("Player");
        }

        public bool HasBotTag()
        {
            return Contains("Bot");
        }

        public bool HasBurstAreaTag()
        {
            return Contains("BurstArea");
        }

        public bool HasWaterFallTag()
        {
            return Contains("WaterFall");
        }

        public bool HasLavaTag()
        {
            return Contains("Lava");
        }

        public bool HasEnemyTag()
        {
            return Contains("Enemy");
        }

        public bool HasStageObjectTag()
        {
            return Contains("StageObject");
        }

        public bool HasPlayerAndEnemyTags()
        {
            return HasPlayerTag() && HasEnemyTag();
        }

        public bool HasGrenadeTag()
        {
            return Contains("Grenade");
        }

        public bool HasLightTag()
        {
            return Contains("Light");
        }

        public bool HasWallTag()
        {
            return Contains("Wall");
        }

        public bool HasFieldItemTag()
        {
            return Contains("FieldItem");
        }

        public bool HasFieldWeaponTag()
        {

            return Contains("FieldWeapon");
        }

        public bool HasEnemyAttackTag()
        {

            return Contains("EnemyAttack");
        }


        public void AddTag(string tag)
        {
            if (!Contains(tag))
            {
                tags.Add(tag);

                tags.Distinct();
            }

            Debug.Log("Add");
        }

        public void AddTag(eMajorTag tag)
        {

            AddTag(tag.ToString());
        }

        public void RemoveFront()
        {

        }

        public void RemoveEnd()
        {

        }

        public void RemoveTag(string str)
        {
            //tag.Remove(str);
        }
        [Button("全てのタグをクリア")]
        public void RemoveAll()
        {
            tags.Clear();
        }

        public List<string> AllTagsToString()
        {
            List<string> result = tags;



            return result;
        }

        public JObject ToJson()
        {
            var result = new JObject();

            foreach (var item in tags.Select((value, index) => new { value, index }))
            {

                result["tag" + item.index.ToString()] = item.value.ToString();
            }

            return result;
        }

        public void PrintUnityDebugLog()
        {

        }

        public override bool Equals(object obj)
        {
            return obj is MultipleTags tags &&
                   base.Equals(obj) &&
                   tag == tags.tag;
        }

        public bool HasMyPlayerTag()
        {
            return Contains("MyPlayer");
        }

        [Button("プラットフォームタグを付加")]
        public void AddPlatformTag()
        {
            AddTag("Platform");
        }

        [Button("プレイヤータグ")]
        public void AddPlayerTag()
        {
            AddTag("Player");
        }

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

            return t1.tags == t2.tags;
        }

        public static bool operator !=(MultipleTags t1, MultipleTags t2)
        {

            return !(t1 == t2);
        }

    }
}
