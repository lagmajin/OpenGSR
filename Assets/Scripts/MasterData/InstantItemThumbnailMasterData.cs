using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using OpenGSCore;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "Item/InstantItemThumbnailMasterData")]
    public class InstantItemThumbnailMasterData : SerializedScriptableObject
    {
        [OdinSerialize] public Dictionary<EInstantItemType, Sprite> thumbnail;

        [Required]public Sprite normalGrenade;
        [Required]public Sprite powerGrenade;
        [Required]public Sprite fireGrenade;
        [Required]public Sprite mineGrenade;
        [Required]public Sprite magneticGrenade;

        [Required] public Sprite clusterGrenade;

        [CanBeNull]
        public Sprite Thumbnail()
        {




            return null;
        }

        [CanBeNull]
        public Sprite GrenadeSprite(EGrenadeType type)
        {
            if (type == EGrenadeType.Normal)
            {
                return normalGrenade;
            }

            if (type == EGrenadeType.Power)
            {
                return powerGrenade;
            }

            if (type == EGrenadeType.Mine)
            {
                return mineGrenade;
            }

            if(type==EGrenadeType.Magnetic)
            {
                return magneticGrenade;
            }

            if (type == EGrenadeType.Cluster)
            {
                return clusterGrenade;
            }

            return null;
        }


    }
}
